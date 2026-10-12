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
$builder = Join-Path $toolRoot 'nelfx08-coop-build.ps1'
$testRoot = Join-Path $repoRoot ('build\test-nelfx08-' + [guid]::NewGuid().ToString('N'))
$mapPath = Join-Path $testRoot 'NightElfX08-Coop-A.w3x'
$secondMapPath = Join-Path $testRoot 'NightElfX08-Coop-B.w3x'
$reportPath = Join-Path $testRoot 'inspection.json'
$scriptPath = Join-Path $testRoot 'war3map.j'
$stringsPath = Join-Path $testRoot 'war3map.wts'
$sourceUnitDataPath = Join-Path $testRoot 'source-war3map.w3u'
$generatedUnitDataPath = Join-Path $testRoot 'generated-war3map.w3u'
$unitBalancePath = Join-Path $testRoot 'UnitBalance.slk'
$itemDataPath = Join-Path $testRoot 'ItemData.slk'
$commonJPath = Join-Path $testRoot 'common.j'
$blizzardJPath = Join-Path $testRoot 'Blizzard.j'
$war3NetRoot = Join-Path $repoRoot 'build\nelfx08-coop-build\publish'

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

try {
    New-Item -ItemType Directory -Path $testRoot | Out-Null
    & $builder -SourceMap $SourceMap -OutputMap $mapPath
    if ($LASTEXITCODE -ne 0) { throw 'First map build failed.' }
    & $builder -SourceMap $SourceMap -OutputMap $secondMapPath
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
    & $inspect archive extract $SourceMap 'war3map.w3u' $sourceUnitDataPath | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Source unit object data extraction failed.' }

    $unchangedEntries = @(
        'war3map.doo', 'war3map.mmp', 'war3map.shd', 'war3map.w3a', 'war3map.w3b',
        'war3map.w3c', 'war3map.w3d', 'war3map.w3e', 'war3map.w3q', 'war3map.w3r',
        'war3map.w3s', 'war3map.w3t', 'war3map.wct', 'war3map.wpm', 'war3map.wtg',
        'war3mapMap.blp', 'war3mapSkin.txt', 'war3mapUnits.doo'
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
        $sourceEntryHash = (Get-FileHash -LiteralPath $sourceEntry -Algorithm SHA256).Hash
        $outputEntryHash = (Get-FileHash -LiteralPath $outputEntry -Algorithm SHA256).Hash
        if ($sourceEntryHash -ne $outputEntryHash) {
            throw "Unchanged archive entry differs: $($unchangedEntries[$index])."
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

    $report = Get-Content -LiteralPath $reportPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if (@($report.warnings).Count -ne 0) { throw 'Generated map inspection reported warnings.' }
    if ($report.map.format_version -ne 25 -or $report.map.map_version -ne 739 -or $report.map.editor_version -ne 'v6031') {
        throw 'Official format/version/editor metadata changed.'
    }
    if ($report.map.script_language -ne 'Jass') { throw 'Generated map is not JASS.' }
    if ($report.map.recommended_players -ne '2') { throw 'Recommended player count is not 2.' }
    if ($report.content.unit_count -ne 292 -or $report.content.doodad_count -ne 3843 -or $report.content.special_doodad_count -ne 0) {
        throw 'Placed unit or doodad counts changed.'
    }
    $players = @($report.map.players)
    $userPlayers = @($players | Where-Object controller -eq 'User')
    if ($userPlayers.Count -ne 2) { throw "Expected exactly two user slots, found $($userPlayers.Count)." }
    $p1 = $players | Where-Object id -eq 1
    $p2 = $players | Where-Object id -eq 2
    $p3 = $players | Where-Object id -eq 3
    if ($null -eq $p1 -or $p1.controller -ne 'User' -or $p1.race -ne 'NightElf') {
        throw 'Player(1) must remain the mandatory Naga user slot.'
    }
    if ($null -eq $p2 -or $p2.controller -ne 'Computer') {
        throw 'Player(2) must remain the Sentinel computer slot.'
    }
    if ($null -eq $p3 -or $p3.controller -ne 'User' -or $p3.race -ne 'NightElf') {
        throw 'Player(3) must be the optional Malfurion Night Elf user slot.'
    }

    $sourceHash = (Get-FileHash -LiteralPath $SourceMap -Algorithm SHA256).Hash
    if ($sourceHash -ne 'AC942B38F7E7F4C1EDA45DE0512667FAE91E5CD36D902FCBE1C1BBFB96B363E0') {
        throw "Unexpected official source hash: $sourceHash"
    }
    $sourceUnitDataHash = (Get-FileHash -LiteralPath $sourceUnitDataPath -Algorithm SHA256).Hash
    if ($sourceUnitDataHash -ne '9EBBA8195A2B076B32A984F296B8F0C751AA3131C881926C23965653C5C4E9C7') {
        throw "Unexpected official war3map.w3u hash: $sourceUnitDataHash"
    }

    Add-Type -Path (Join-Path $war3NetRoot 'War3Net.Common.dll')
    Add-Type -Path (Join-Path $war3NetRoot 'War3Net.Build.Core.dll')
    Add-Type -Path (Join-Path $war3NetRoot 'War3Net.IO.Slk.dll')

    $itemStream = [IO.File]::OpenRead($itemDataPath)
    try { $itemData = [War3Net.IO.Slk.SylkParser]::new().Parse($itemStream, $true) }
    finally { $itemStream.Dispose() }
    $itemHeaders = for ($column = 0; $column -le $itemData.Columns; $column++) { [string]$itemData[$column, 0] }
    $itemIdColumn = [array]::IndexOf($itemHeaders, 'itemID')
    if ($itemIdColumn -lt 0) { throw 'ItemData.slk is missing itemID.' }
    $hasShadowOrb = $false
    for ($row = 1; $row -le $itemData.Rows; $row++) {
        if ([string]$itemData[$itemIdColumn, $row] -eq 'sora') { $hasShadowOrb = $true; break }
    }
    if (-not $hasShadowOrb) { throw "The 1.24e item table does not contain 'sora'." }

    $heroIds = @('Eevi', 'Emfr', 'Etyr', 'Ewrd', 'Hmgd', 'Udea', 'Udre', 'Ulic')
    $balanceStream = [IO.File]::OpenRead($unitBalancePath)
    try { $balance = [War3Net.IO.Slk.SylkParser]::new().Parse($balanceStream, $true) }
    finally { $balanceStream.Dispose() }
    $balanceHeaders = for ($column = 0; $column -le $balance.Columns; $column++) { [string]$balance[$column, 0] }
    $unitIdColumn = [array]::IndexOf($balanceHeaders, 'unitBalanceID')
    $foodColumn = [array]::IndexOf($balanceHeaders, 'fused')
    if ($unitIdColumn -lt 0 -or $foodColumn -lt 0) { throw 'UnitBalance.slk is missing unitBalanceID or fused.' }
    $effectiveFood = @{}
    for ($row = 1; $row -le $balance.Rows; $row++) {
        $unitId = [string]$balance[$unitIdColumn, $row]
        if ($unitId -notin $heroIds) { continue }
        $rawFood = $balance[$foodColumn, $row]
        if ($null -eq $rawFood -or [string]$rawFood -eq '' -or [string]$rawFood -eq '-') { $effectiveFood[$unitId] = 0 }
        elseif ($rawFood -is [int]) { $effectiveFood[$unitId] = [int]$rawFood }
        else { throw "Unsupported fused value '$rawFood' for $unitId." }
    }
    $unitStream = [IO.File]::OpenRead($generatedUnitDataPath)
    $unitReader = [IO.BinaryReader]::new($unitStream)
    try { $unitData = [War3Net.Build.Extensions.BinaryReaderExtensions]::ReadUnitObjectData($unitReader) }
    finally { $unitReader.Dispose(); $unitStream.Dispose() }
    foreach ($unit in @($unitData.BaseUnits) + @($unitData.NewUnits)) {
        $oldId = [War3Net.Common.Extensions.Int32Extensions]::ToRawcode($unit.OldId)
        $effectiveIdValue = if ($unit.NewId -eq 0) { $unit.OldId } else { $unit.NewId }
        $effectiveId = [War3Net.Common.Extensions.Int32Extensions]::ToRawcode($effectiveIdValue)
        if ($effectiveId -notin $heroIds) { continue }
        if ($unit.NewId -ne 0 -and $effectiveFood.ContainsKey($oldId)) {
            $effectiveFood[$effectiveId] = $effectiveFood[$oldId]
        }
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
    foreach ($heroId in @('Udea', 'Udre', 'Ulic')) {
        $override = @($unitData.BaseUnits | Where-Object {
            [War3Net.Common.Extensions.Int32Extensions]::ToRawcode($_.OldId) -eq $heroId
        })
        if ($override.Count -ne 1) { throw "Expected one base-unit override for $heroId." }
        $foodMods = @($override[0].Modifications | Where-Object {
            [War3Net.Common.Extensions.Int32Extensions]::ToRawcode($_.Id) -eq 'ufoo'
        })
        if ($foodMods.Count -ne 1 -or $foodMods[0].ValueAsInt -ne 0) {
            throw "Expected $heroId ufoo=0."
        }
    }

    $script = Get-Content -LiteralPath $scriptPath -Raw -Encoding UTF8
    $strings = Get-Content -LiteralPath $stringsPath -Raw -Encoding UTF8
    $pjassOutput = & $PjassPath $commonJPath $blizzardJPath $scriptPath 2>&1
    if ($LASTEXITCODE -ne 0) { throw "1.24e JASS validation failed:`n$($pjassOutput -join "`n")" }
    if ($script.Contains("`r`r`n")) { throw 'Generated script contains CR-CR-LF line endings.' }
    foreach ($fragment in @(
        'STRING 900', 'STRING 901', 'STRING 902', 'STRING 903', 'STRING 904', 'STRING 905', 'STRING 906',
        '选择难度', '已启用原版单人共享控制回退'
    )) { if (-not $strings.Contains($fragment)) { throw "Missing WTS fragment: $fragment" } }

    foreach ($fragment in @(
        'call SetPlayerController( Player(3), MAP_CONTROL_USER )',
        'call SetPlayerRacePreference( Player(3), RACE_PREF_NIGHTELF )',
        'function CoopClassifyP3 takes nothing returns nothing',
        'set udg_CoopP3Human = ( controller == MAP_CONTROL_USER and slotState == PLAYER_SLOT_STATE_PLAYING )',
        'call SetPlayerController( Player(3), MAP_CONTROL_NEUTRAL )',
        'set udg_APG2_BluePlayer = udg_CoopHumanForce',
        'function CoopApplyP3Mode takes nothing returns nothing',
        'ALLIANCE_SHARED_CONTROL, false',
        'ALLIANCE_SHARED_ADVANCED_CONTROL, false',
        'call SetPlayerAllianceStateBJ( Player(3), Player(PLAYER_NEUTRAL_AGGRESSIVE), bj_ALLIANCE_UNALLIED )',
        'call SetPlayerAllianceStateBJ( Player(PLAYER_NEUTRAL_AGGRESSIVE), Player(3), bj_ALLIANCE_UNALLIED )',
        'call SetMapFlag( MAP_LOCK_ALLIANCE_CHANGES, false )',
        'call SetMapFlag( MAP_LOCK_RESOURCE_TRADING, false )',
        'function CoopFinishDifficultySelection takes gamedifficulty difficulty returns nothing',
        'call SetGameDifficulty( difficulty )',
        'call DialogDisplay( Player(1), udg_CoopDifficultyDialog, true )',
        'call TimerStart( udg_CoopDifficultyTimer, 30.00, false, null )',
        'call CoopScheduleDifficultySelection(  )',
        'call StartMeleeAI( Player(2), "N08X03.ai" )',
        "call CreateItemLoc( 'sora', GetRectCenter(gg_rct_MaievItem01) )",
        'function CoopVictory takes nothing returns nothing',
        'function CoopDefeat takes string message returns nothing',
        'call SelectUnitForPlayerSingle( udg_Illidan, Player(1) )',
        'call SelectUnitForPlayerSingle( udg_Furion, Player(3) )',
        'call SelectGroupForPlayerBJ( udg_CoopP3SelectedUnits, Player(3) )',
        'call PanCameraToTimedLocForPlayer( Player(3), GetRectCenter(gg_rct_Game_Start_Furion), 0 )',
        'IsLocationVisibleToPlayer(udg_PointDamInSight1, Player(3))'
    )) { Assert-Contains $script $fragment }

    foreach ($fragment in @(
        'EVENT_PLAYER_LEAVE',
        'InitGameCacheBJ(',
        'RestoreUnitLocFacingAngleBJ(',
        'SaveGameCacheBJ(',
        'StoreUnitBJ(',
        'SetNextLevelBJ(',
        'SetMissionAvailableBJ(',
        'SetCampaignAvailableBJ(',
        'CustomVictoryBJ( udg_AP2_BluePlayer',
        'CustomDefeatBJ( udg_AP2_BluePlayer',
        'StartMeleeAI( Player(3)',
        'Blz'
    )) { Assert-NotContains $script $fragment }

    Assert-Count $script 'GetPlayerController(Player(3))' 1
    Assert-Count $script 'GetPlayerSlotState(Player(3))' 1
    Assert-Count $script 'call SetPlayerAllianceStateBJ( Player(3), Player(PLAYER_NEUTRAL_AGGRESSIVE), bj_ALLIANCE_UNALLIED )' 1
    Assert-Count $script 'call SetPlayerAllianceStateBJ( Player(PLAYER_NEUTRAL_AGGRESSIVE), Player(3), bj_ALLIANCE_UNALLIED )' 1
    Assert-Count $script 'call StartMeleeAI( Player(2), "N08X03.ai" )' 1
    Assert-Count $script "call CreateItemLoc( 'sora', GetRectCenter(gg_rct_MaievItem01) )" 1
    Assert-Count $script 'TriggerRegisterPlayerEventEndCinematic' 6
    Assert-Count $script 'if ( udg_IntroSkipped ) then' 1
    Assert-Count $script 'if ( udg_DamCinSkipped ) then' 1
    Assert-Count $script 'if ( udg_VictorySkipped ) then' 1
    Assert-Count $script 'call SelectGroupForPlayerBJ( udg_SelectedUnits, Player(1) )' 2
    Assert-Count $script 'call SelectGroupForPlayerBJ( udg_CoopP3SelectedUnits, Player(3) )' 2

    $expectedSkills = @{
        Trig_Load_Tyrande_Actions = @('AEst','AEst','AEst','AHfa','AHfa','AHfa','AEar','AEar','AEar','AEsf')
        Trig_Load_Furion_Actions  = @('AEer','AEer','AEer','AEfn','AEfn','AEfn','AEah','AEah','AEah','AEtq')
        Trig_Load_Illidan_Actions = @('AEmb','AEmb','AEmb','AEim','AEim','AEim','AEev','AEev','AEev','AEvi')
    }
    foreach ($functionName in $expectedSkills.Keys) {
        $body = [regex]::Match($script, "function $functionName takes nothing returns nothing(?s:.*?)endfunction").Value
        if (-not $body) { throw "$functionName is missing." }
        $skills = @([regex]::Matches($body, "SelectHeroSkill\( [^,]+, '([^']+)' \)") | ForEach-Object { $_.Groups[1].Value })
        if (($skills -join '|') -ne ($expectedSkills[$functionName] -join '|')) {
            throw "Unexpected skill order in $functionName`: $($skills -join ', ')"
        }
    }

    $startAiBody = [regex]::Match($script, 'function Trig_Start_AI_Actions takes nothing returns nothing(?s:.*?)endfunction').Value
    foreach ($playerId in @(2,4,5,6,8,10,11)) {
        Assert-Contains $startAiBody "call StartMeleeAI( Player($playerId),"
    }
    $tealGoldBody = [regex]::Match($script, 'function Trig_AI_Resources_Teal_Gold_Actions takes nothing returns nothing(?s:.*?)endfunction').Value
    $tealLumberBody = [regex]::Match($script, 'function Trig_AI_Resources_Teal_Lumber_Actions takes nothing returns nothing(?s:.*?)endfunction').Value
    Assert-Contains $tealGoldBody 'call SetPlayerStateBJ( Player(2), PLAYER_STATE_RESOURCE_GOLD, 2109 )'
    Assert-Contains $tealLumberBody 'call SetPlayerStateBJ( Player(2), PLAYER_STATE_RESOURCE_LUMBER, 3243 )'

    Write-Host 'PASS: NightElfX08 co-op build is deterministic and passed all static checks.'
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
