[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$SourceMap,

    [string]$GamePath,

    [string]$PjassPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$toolRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$repoRoot = (Resolve-Path (Join-Path $toolRoot '..\..\..\..\..')).Path
$inspect = Join-Path $repoRoot 'tools\wc3-inspect\wc3-inspect.ps1'
$builder = Join-Path $toolRoot 'orc03-coop-build.ps1'
$testRoot = Join-Path $repoRoot ('build\test-orc03-' + [guid]::NewGuid().ToString('N'))
$mapPath = Join-Path $testRoot 'Orc03-Coop-A.w3x'
$secondMapPath = Join-Path $testRoot 'Orc03-Coop-B.w3x'
$reportPath = Join-Path $testRoot 'inspection.json'
$scriptPath = Join-Path $testRoot 'war3map.j'
$stringsPath = Join-Path $testRoot 'war3map.wts'
$generatedUnitDataPath = Join-Path $testRoot 'generated-war3map.w3u'
$unitBalancePath = Join-Path $testRoot 'UnitBalance.slk'
$itemDataPath = Join-Path $testRoot 'ItemData.slk'
$commonJPath = Join-Path $testRoot 'common.j'
$blizzardJPath = Join-Path $testRoot 'Blizzard.j'
$war3NetRoot = Join-Path $repoRoot 'build\orc03-coop-build\publish'

if (-not $GamePath) {
    $GamePath = Join-Path $repoRoot 'game\Warcraft3-1.24e'
}
if (-not $PjassPath) {
    $PjassPath = Join-Path $repoRoot 'game\Warcraft III - 1.31\x86\JassHelper\pjass.exe'
}
if (-not (Test-Path -LiteralPath $PjassPath -PathType Leaf)) {
    throw "pjass was not found: $PjassPath"
}

function Assert-Contains([string]$Text, [string]$Fragment) {
    if (-not $Text.Contains($Fragment)) { throw "Missing script fragment: $Fragment" }
}

function Assert-NotContains([string]$Text, [string]$Fragment) {
    if ($Text.Contains($Fragment)) { throw "Forbidden script fragment remains: $Fragment" }
}

function Assert-Count([string]$Text, [string]$Fragment, [int]$Expected) {
    $actual = ([regex]::Matches($Text, [regex]::Escape($Fragment))).Count
    if ($actual -ne $Expected) {
        throw "Expected $Expected occurrence(s) of '$Fragment', found $actual."
    }
}

function Get-FunctionBody([string]$Script, [string]$FunctionName) {
    $match = [regex]::Match(
        $Script,
        "function $([regex]::Escape($FunctionName)) takes nothing returns (?:nothing|boolean)(?s:.*?)endfunction")
    if (-not $match.Success) { throw "Function is missing: $FunctionName" }
    return $match.Value
}

try {
    New-Item -ItemType Directory -Path $testRoot | Out-Null
    & $builder orc03 $SourceMap $mapPath
    if ($LASTEXITCODE -ne 0) { throw 'First map build failed.' }
    & $builder orc03 $SourceMap $secondMapPath
    if ($LASTEXITCODE -ne 0) { throw 'Second map build failed.' }

    $firstHash = (Get-FileHash -LiteralPath $mapPath -Algorithm SHA256).Hash
    $secondHash = (Get-FileHash -LiteralPath $secondMapPath -Algorithm SHA256).Hash
    if ($firstHash -ne $secondHash) {
        throw "Build is not deterministic: $firstHash vs $secondHash."
    }

    & $inspect map inspect $mapPath --output $reportPath | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Generated map inspection failed.' }
    foreach ($entry in @(
        @('war3map.j', $scriptPath),
        @('war3map.wts', $stringsPath),
        @('war3map.w3u', $generatedUnitDataPath)
    )) {
        & $inspect archive extract $mapPath $entry[0] $entry[1] | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "Generated $($entry[0]) extraction failed." }
    }

    $unchangedEntries = @(
        'war3map.doo', 'war3map.imp', 'war3map.mmp', 'war3map.shd', 'war3map.w3c',
        'war3map.w3e', 'war3map.w3r', 'war3map.w3s', 'war3map.wct', 'war3map.wpm',
        'war3map.wtg', 'war3mapMap.blp', 'war3mapUnits.doo'
    )
    $unchangedRoot = Join-Path $testRoot 'unchanged'
    New-Item -ItemType Directory -Path $unchangedRoot | Out-Null
    for ($index = 0; $index -lt $unchangedEntries.Count; $index++) {
        $sourceEntry = Join-Path $unchangedRoot ("source-$index.bin")
        $outputEntry = Join-Path $unchangedRoot ("output-$index.bin")
        & $inspect archive extract $SourceMap $unchangedEntries[$index] $sourceEntry | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "Source extraction failed for $($unchangedEntries[$index])." }
        & $inspect archive extract $mapPath $unchangedEntries[$index] $outputEntry | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "Output extraction failed for $($unchangedEntries[$index])." }
        if ((Get-FileHash -LiteralPath $sourceEntry -Algorithm SHA256).Hash -ne
            (Get-FileHash -LiteralPath $outputEntry -Algorithm SHA256).Hash) {
            throw "Official archive entry changed unexpectedly: $($unchangedEntries[$index])."
        }
    }

    foreach ($entry in @(
        @('Units\UnitBalance.slk', $unitBalancePath),
        @('Units\ItemData.slk', $itemDataPath),
        @('Scripts\common.j', $commonJPath),
        @('Scripts\Blizzard.j', $blizzardJPath)
    )) {
        & $inspect archive extract (Join-Path $GamePath 'War3Patch.mpq') $entry[0] $entry[1] | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "1.24e $($entry[0]) extraction failed." }
    }

    $sourceHash = (Get-FileHash -LiteralPath $SourceMap -Algorithm SHA256).Hash
    if ($sourceHash -ne '84ADDFD4D347B2A0A89947F47680CA4D968C8FB1DB3E649515C22CE3A659F239') {
        throw "Unexpected official source hash: $sourceHash"
    }

    $report = Get-Content -LiteralPath $reportPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $warnings = @($report.warnings)
    if ($warnings.Count -ne 1 -or -not $warnings[0].StartsWith('Could not parse optional war3map.imp:')) {
        throw "Unexpected map inspection warnings: $($warnings -join '; ')"
    }
    if ($report.map.format_version -ne 18 -or $report.map.map_version -ne 308 -or $report.map.editor_version -ne 'v4448') {
        throw 'Official format/version/editor metadata changed.'
    }
    if ($report.map.script_language -ne 'Jass') { throw 'Generated map is not JASS.' }
    if ($report.map.name -ne '战歌的怒吼 - 双人合作开发版') { throw 'Generated map name is incorrect.' }
    if ($report.map.recommended_players -ne '2') { throw 'Recommended player count is not 2.' }
    if ($report.map.width -ne 92 -or $report.map.height -ne 124) { throw 'Map dimensions changed.' }
    if ($report.content.trigger_string_count -ne 100) { throw 'Trigger-string count changed unexpectedly.' }
    if ($report.content.unit_count -ne 230 -or $report.content.doodad_count -ne 3875 -or
        $report.content.special_doodad_count -ne 1) {
        throw 'Placed unit or doodad counts changed.'
    }
    $players = @($report.map.players)
    $userPlayers = @($players | Where-Object controller -eq 'User')
    if ($userPlayers.Count -ne 2) { throw "Expected exactly two user slots, found $($userPlayers.Count)." }
    $p0 = $players | Where-Object id -eq 0
    $p2 = $players | Where-Object id -eq 2
    $p3 = $players | Where-Object id -eq 3
    if ($null -eq $p0 -or $p0.controller -ne 'User' -or $p0.race -ne 'Orc') {
        throw 'Player(0) must remain the mandatory Thrall Orc user slot.'
    }
    if ($null -eq $p2 -or $p2.controller -ne 'Computer' -or $p2.race -ne 'Orc') {
        throw 'Player(2) must remain the Warsong raider computer slot.'
    }
    if ($null -eq $p3 -or $p3.controller -ne 'User' -or $p3.race -ne 'Orc') {
        throw 'Player(3) must be the optional Grom Orc user slot.'
    }

    Add-Type -Path (Join-Path $war3NetRoot 'War3Net.Common.dll')
    Add-Type -Path (Join-Path $war3NetRoot 'War3Net.Build.Core.dll')
    Add-Type -Path (Join-Path $war3NetRoot 'War3Net.IO.Slk.dll')

    $expectedItemAbilities = @{
        rin1 = 'AIi3'
        rst1 = 'AIs3'
        penr = 'AImb'
        hlst = 'AIh2,Arll'
        whwd = 'AIhw'
        shea = 'AIha'
    }
    $expectedItemUses = @{ hlst = 1; whwd = 3; shea = 1 }
    $itemStream = [IO.File]::OpenRead($itemDataPath)
    try { $itemData = [War3Net.IO.Slk.SylkParser]::new().Parse($itemStream, $true) }
    finally { $itemStream.Dispose() }
    $itemHeaders = for ($column = 0; $column -le $itemData.Columns; $column++) { [string]$itemData[$column, 0] }
    $itemIdColumn = [array]::IndexOf($itemHeaders, 'itemID')
    $itemAbilityColumn = [array]::IndexOf($itemHeaders, 'abilList')
    $itemUsesColumn = [array]::IndexOf($itemHeaders, 'uses')
    if ($itemIdColumn -lt 0 -or $itemAbilityColumn -lt 0 -or $itemUsesColumn -lt 0) {
        throw 'ItemData.slk is missing required columns.'
    }
    $actualItemAbilities = @{}
    $actualItemUses = @{}
    for ($row = 1; $row -le $itemData.Rows; $row++) {
        $itemId = [string]$itemData[$itemIdColumn, $row]
        if (-not $expectedItemAbilities.ContainsKey($itemId)) { continue }
        $actualItemAbilities[$itemId] = [string]$itemData[$itemAbilityColumn, $row]
        if ($expectedItemUses.ContainsKey($itemId)) {
            $actualItemUses[$itemId] = [int]$itemData[$itemUsesColumn, $row]
        }
    }
    foreach ($itemId in $expectedItemAbilities.Keys) {
        if (-not $actualItemAbilities.ContainsKey($itemId) -or
            $actualItemAbilities[$itemId] -ne $expectedItemAbilities[$itemId]) {
            throw "Unexpected 1.24e ability mapping for item $itemId."
        }
    }
    foreach ($itemId in $expectedItemUses.Keys) {
        if (-not $actualItemUses.ContainsKey($itemId) -or $actualItemUses[$itemId] -ne $expectedItemUses[$itemId]) {
            throw "Unexpected 1.24e uses value for item $itemId."
        }
    }

    $heroIds = @('Othr', 'Ogrh', 'Hamg', 'Hpal', 'Hmkg')
    $balanceStream = [IO.File]::OpenRead($unitBalancePath)
    try { $balance = [War3Net.IO.Slk.SylkParser]::new().Parse($balanceStream, $true) }
    finally { $balanceStream.Dispose() }
    $headers = for ($column = 0; $column -le $balance.Columns; $column++) { [string]$balance[$column, 0] }
    $unitIdColumn = [array]::IndexOf($headers, 'unitBalanceID')
    $foodColumn = [array]::IndexOf($headers, 'fused')
    if ($unitIdColumn -lt 0 -or $foodColumn -lt 0) { throw 'UnitBalance.slk is missing required columns.' }
    $effectiveFood = @{}
    for ($row = 1; $row -le $balance.Rows; $row++) {
        $unitId = [string]$balance[$unitIdColumn, $row]
        if ($unitId -notin $heroIds) { continue }
        $rawFood = $balance[$foodColumn, $row]
        if ($null -eq $rawFood -or [string]$rawFood -eq '' -or [string]$rawFood -eq '-') {
            $effectiveFood[$unitId] = 0
        }
        elseif ($rawFood -is [int]) { $effectiveFood[$unitId] = [int]$rawFood }
        else { throw "Unsupported fused value '$rawFood' for $unitId." }
    }
    $unitStream = [IO.File]::OpenRead($generatedUnitDataPath)
    $unitReader = [IO.BinaryReader]::new($unitStream)
    try { $unitData = [War3Net.Build.Extensions.BinaryReaderExtensions]::ReadUnitObjectData($unitReader) }
    finally { $unitReader.Dispose(); $unitStream.Dispose() }
    foreach ($unit in @($unitData.BaseUnits) + @($unitData.NewUnits)) {
        $oldId = [War3Net.Common.Extensions.Int32Extensions]::ToRawcode($unit.OldId)
        $effectiveValue = if ($unit.NewId -eq 0) { $unit.OldId } else { $unit.NewId }
        $effectiveId = [War3Net.Common.Extensions.Int32Extensions]::ToRawcode($effectiveValue)
        if ($effectiveId -notin $heroIds) { continue }
        if ($unit.NewId -ne 0 -and $effectiveFood.ContainsKey($oldId)) { $effectiveFood[$effectiveId] = $effectiveFood[$oldId] }
        foreach ($modification in $unit.Modifications) {
            if ([War3Net.Common.Extensions.Int32Extensions]::ToRawcode($modification.Id) -eq 'ufoo') {
                $effectiveFood[$effectiveId] = $modification.ValueAsInt
            }
        }
    }
    foreach ($heroId in $heroIds) {
        if (-not $effectiveFood.ContainsKey($heroId) -or $effectiveFood[$heroId] -ne 0) {
            throw "Campaign hero $heroId does not have effective food use 0."
        }
    }
    foreach ($heroId in @('Hamg', 'Hpal', 'Hmkg')) {
        $override = @($unitData.BaseUnits | Where-Object {
            [War3Net.Common.Extensions.Int32Extensions]::ToRawcode($_.OldId) -eq $heroId
        })
        if ($override.Count -ne 1) { throw "Expected one base-unit override for $heroId." }
        $foodMods = @($override[0].Modifications | Where-Object {
            [War3Net.Common.Extensions.Int32Extensions]::ToRawcode($_.Id) -eq 'ufoo'
        })
        if ($foodMods.Count -ne 1 -or $foodMods[0].ValueAsInt -ne 0) { throw "Expected $heroId ufoo=0." }
    }

    $script = Get-Content -LiteralPath $scriptPath -Raw -Encoding UTF8
    $strings = Get-Content -LiteralPath $stringsPath -Raw -Encoding UTF8
    $stringBytes = [IO.File]::ReadAllBytes($stringsPath)
    if ($stringBytes.Length -ge 3 -and $stringBytes[0] -eq 0xEF -and $stringBytes[1] -eq 0xBB -and $stringBytes[2] -eq 0xBF) {
        throw 'war3map.wts must be UTF-8 without BOM.'
    }
    $strictUtf8 = [Text.UTF8Encoding]::new($false, $true)
    $null = $strictUtf8.GetString($stringBytes)
    $pjassOutput = & $PjassPath $commonJPath $blizzardJPath $scriptPath 2>&1
    if ($LASTEXITCODE -ne 0) { throw "1.24e JASS validation failed:`n$($pjassOutput -join "`n")" }
    if ($script.Contains("`r`r`n")) { throw 'Generated script contains CR-CR-LF line endings.' }

    foreach ($fragment in @(
        'STRING 900', 'STRING 901', 'STRING 902', 'STRING 903', 'STRING 904', 'STRING 905', 'STRING 906',
        '选择难度', '玩家 2 槽未设置为真人或电脑'
    )) { if (-not $strings.Contains($fragment)) { throw "Missing WTS fragment: $fragment" } }

    foreach ($fragment in @(
        'call SetPlayerController( Player(3), MAP_CONTROL_USER )',
        'local mapcontrol controller = GetPlayerController(warsongPlayer)',
        'local playerslotstate slotState = GetPlayerSlotState(warsongPlayer)',
        'set udg_CoopP3Human = ( controller == MAP_CONTROL_USER and slotState == PLAYER_SLOT_STATE_PLAYING )',
        'set udg_CoopP3AI = ( controller == MAP_CONTROL_COMPUTER and slotState == PLAYER_SLOT_STATE_PLAYING )',
        'function CoopApplyHumanP3TechParity takes nothing returns nothing',
        'call CoopApplyHumanP3TechParity(  )',
        'call SetMapFlag( MAP_LOCK_ALLIANCE_CHANGES, false )',
        'call SetMapFlag( MAP_LOCK_RESOURCE_TRADING, false )',
        'function CoopApplyHumanP3Diplomacy takes nothing returns nothing',
        'call SetPlayerAllianceStateBJ( Player(3), Player(PLAYER_NEUTRAL_AGGRESSIVE), bj_ALLIANCE_UNALLIED )',
        'call SetPlayerAllianceStateBJ( Player(PLAYER_NEUTRAL_AGGRESSIVE), Player(3), bj_ALLIANCE_UNALLIED )',
        'function CoopRestoreEndingAlliance takes nothing returns nothing',
        'function CoopVictory takes nothing returns nothing',
        'function CoopDefeat takes string message returns nothing',
        'call DialogDisplay( Player(0), udg_CoopDifficultyDialog, true )',
        'call TimerStart( udg_CoopDifficultyTimer, 30.00, false, null )',
        'call StartCampaignAI( Player(3), "o03_grom.ai" )',
        'call CommandAI( Player(3), command, data )',
        'call SelectUnitForPlayerSingle( udg_Thrall, Player(0) )',
        'call SelectUnitForPlayerSingle( udg_Grom, Player(3) )',
        'if ( not udg_CoopP3Human ) then'
    )) { Assert-Contains $script $fragment }

    foreach ($fragment in @(
        'EVENT_PLAYER_LEAVE', 'InitGameCacheBJ(', 'RestoreUnitLocFacingAngleBJ(', 'StoreUnitBJ(',
        'SaveGameCacheBJ(', 'SetMissionAvailableBJ(', 'SetNextLevelBJ(', 'SetCampaignMenuRaceBJ(',
        'bj_FORCE_PLAYER[0]', 'SetUnitOwner(', 'Blz', 'Lua'
    )) { Assert-NotContains $script $fragment }

    Assert-Count $script 'GetPlayerController(warsongPlayer)' 1
    Assert-Count $script 'GetPlayerSlotState(warsongPlayer)' 1
    Assert-Count $script 'call CoopClassifyP3(  )' 1
    Assert-Count $script 'call InitCustomTriggers(  )' 1
    Assert-Count $script 'call RunInitializationTriggers(  )' 1
    Assert-Count $script 'call StartCampaignAI( Player(3), "o03_grom.ai" )' 1
    Assert-Count $script 'call CoopCommandGromAI(' 15
    Assert-Count $script 'call CommandAI( Player(3), command, data )' 1
    Assert-Count $script 'call CoopVictory(  )' 1
    Assert-Count $script 'call CoopDefeat( "TRIGSTR_010" )' 1
    Assert-Count $script 'call CoopSelectStartingHeroes(  )' 2
    Assert-Count $script 'TriggerRegisterPlayerEventEndCinematic' 4
    foreach ($playerId in @(0,1,2,3,8,9,10,11)) {
        Assert-Count $script "call SetPlayerRaceSelectable( Player($playerId), false )" 1
        Assert-Count $script "call SetPlayerRaceSelectable( Player($playerId), true )" 0
    }
    foreach ($playerId in @(0,2)) {
        Assert-Count $script "call SetPlayerTechMaxAllowed( Player($playerId), 'Oshd', 0 )" 1
    }
    foreach ($rawcode in @('ospw', 'otbk', 'otbr', 'ovln', 'Rolf', 'Rorb', 'Robk', 'Ropm', 'Robf')) {
        Assert-Count $script "call SetPlayerTechMaxAllowed( Player(0), '$rawcode', 0 )" 1
        Assert-Count $script "call SetPlayerTechMaxAllowed( Player(3), '$rawcode', 0 )" 1
    }
    Assert-Count $script 'function InitUpgrades_Player3 takes nothing returns nothing' 0
    Assert-Count $script 'call SetPlayerAllianceStateBJ( Player(3), Player(PLAYER_NEUTRAL_AGGRESSIVE), bj_ALLIANCE_UNALLIED )' 1
    Assert-Count $script 'call SetPlayerAllianceStateBJ( Player(PLAYER_NEUTRAL_AGGRESSIVE), Player(3), bj_ALLIANCE_UNALLIED )' 1

    $humanTechBody = Get-FunctionBody $script 'CoopApplyHumanP3TechParity'
    Assert-Contains $humanTechBody 'if ( udg_CoopP3Human ) then'
    $expectedHumanTechCalls = @()
    foreach ($sourceFunction in @('InitUpgrades_Player0', 'InitTechTree_Player0')) {
        $expectedHumanTechCalls += [regex]::Matches((Get-FunctionBody $script $sourceFunction), '(?m)^\s*call .+$') |
            ForEach-Object { $_.Value.Trim().Replace('Player(0)', 'Player(3)') }
    }
    $actualHumanTechCalls = [regex]::Matches($humanTechBody, '(?m)^\s*call .+$') |
        ForEach-Object { $_.Value.Trim() }
    if (($expectedHumanTechCalls -join "`n") -ne ($actualHumanTechCalls -join "`n")) {
        throw 'CoopApplyHumanP3TechParity does not exactly mirror Player 0 tech and upgrade calls.'
    }

    foreach ($functionName in @(
        'Trig_Player_Attacks_Grom_Conditions', 'Trig_Player_Attacks_Teal_Conditions',
        'Trig_GromAggrosPlayerCheck_Actions', 'Trig_GromAttacksBrownHeartbeat_Actions'
    )) {
        Assert-Contains (Get-FunctionBody $script $functionName) 'if ( not udg_CoopP3AI ) then'
    }
    foreach ($functionName in @('Trig_GromGold_Actions', 'Trig_GromLumber_Actions')) {
        Assert-Contains (Get-FunctionBody $script $functionName) 'if ( not udg_CoopP3AI ) then'
    }
    $activateAi = Get-FunctionBody $script 'Trig_ActivateGromAI_Actions'
    Assert-Contains $activateAi 'if ( not udg_CoopP3AI or udg_GromAIActive ) then'
    Assert-Contains $activateAi 'call ReviveHeroLoc( udg_Grom, GetRectCenter(gg_rct_InitialGromLocation), true )'
    Assert-Contains $activateAi 'set udg_GromAIActive = true'

    $thrallLoad = Get-FunctionBody $script 'Trig_Load_Thrall_Actions'
    $gromLoad = Get-FunctionBody $script 'Trig_Load_Grom_Actions'
    foreach ($fragment in @("'Othr', Player(0)", 'SetHeroLevel( udg_Thrall, 3', "'AOcl'", "'AOsf'", "'AOfs'")) {
        Assert-Contains $thrallLoad $fragment
    }
    $expectedThrallItems = @('rin1', 'rst1', 'penr', 'hlst', 'whwd', 'shea')
    $actualThrallItems = @(
        [regex]::Matches($thrallLoad, "call UnitAddItemByIdSwapped\( '([^']+)', udg_Thrall \)") |
            ForEach-Object { $_.Groups[1].Value }
    )
    if (($expectedThrallItems -join '/') -ne ($actualThrallItems -join '/')) {
        throw "Thrall's initial item order is not the confirmed rin1/rst1/penr/hlst/whwd/shea loadout."
    }
    Assert-NotContains $thrallLoad 'SetItemCharges('
    foreach ($fragment in @("'Ogrh', Player(3)", 'SetHeroLevel( udg_Grom, 4', "'AOmi'", "'AOwk'")) {
        Assert-Contains $gromLoad $fragment
    }
    Assert-Count $gromLoad "SelectHeroSkill( udg_Grom, 'AOcr' )" 2

    Write-Host 'PASS: Orc03 co-op build is deterministic and passed all static checks.'
    Write-Host "Map SHA-256: $firstHash"
}
finally {
    if (Test-Path -LiteralPath $testRoot) {
        $resolvedTestRoot = (Resolve-Path -LiteralPath $testRoot).Path
        $resolvedBuildRoot = (Resolve-Path -LiteralPath (Join-Path $repoRoot 'build')).Path
        if (-not $resolvedTestRoot.StartsWith($resolvedBuildRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to remove unexpected test directory: $resolvedTestRoot"
        }
        Remove-Item -LiteralPath $resolvedTestRoot -Recurse -Force
    }
}
