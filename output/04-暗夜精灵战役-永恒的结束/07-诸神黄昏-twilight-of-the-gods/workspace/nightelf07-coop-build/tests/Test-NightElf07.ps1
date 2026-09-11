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
$builder = Join-Path $toolRoot 'nightelf07-coop-build.ps1'
$testRoot = Join-Path $repoRoot ('build\test-nightelf07-' + [guid]::NewGuid().ToString('N'))
$mapPath = Join-Path $testRoot 'NightElf07-Coop-Development.w3x'
$reportPath = Join-Path $testRoot 'inspection.json'
$scriptPath = Join-Path $testRoot 'war3map.j'
$stringsPath = Join-Path $testRoot 'war3map.wts'
$commonJPath = Join-Path $testRoot 'common.j'
$blizzardJPath = Join-Path $testRoot 'Blizzard.j'

if (-not $GamePath) {
    $GamePath = Join-Path $repoRoot 'game\Warcraft3-1.24e'
}
if (-not $PjassPath) {
    $PjassPath = Join-Path $repoRoot 'game\Warcraft III - 1.31\x86\JassHelper\pjass.exe'
}

function Invoke-Inspect {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments)
    & $inspect @Arguments | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "wc3-inspect failed: $($Arguments -join ' ')"
    }
}

try {
    New-Item -ItemType Directory -Path $testRoot | Out-Null
    & $builder nightelf07 $SourceMap $mapPath
    if ($LASTEXITCODE -ne 0) { throw 'Map build failed.' }

    Invoke-Inspect map inspect $mapPath --output $reportPath
    Invoke-Inspect archive extract $mapPath 'war3map.j' $scriptPath
    Invoke-Inspect archive extract $mapPath 'war3map.wts' $stringsPath
    Invoke-Inspect archive extract (Join-Path $GamePath 'War3Patch.mpq') 'Scripts\common.j' $commonJPath
    Invoke-Inspect archive extract (Join-Path $GamePath 'War3Patch.mpq') 'Scripts\Blizzard.j' $blizzardJPath

    $report = Get-Content -LiteralPath $reportPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($report.source.sha256 -ne (Get-FileHash -LiteralPath $mapPath -Algorithm SHA256).Hash) {
        throw 'Inspection report hash does not match the generated map.'
    }
    if ($report.map.format_version -ne 18 -or $report.map.map_version -ne 898 -or $report.map.editor_version -ne 'v4448') {
        throw 'Classic map/editor format changed unexpectedly.'
    }
    if ($report.map.recommended_players -ne '2-3') { throw 'Recommended player count must be 2-3.' }
    if ($report.content.unit_count -ne 173 -or $report.content.doodad_count -ne 4078) {
        throw 'Placed unit or doodad count changed unexpectedly.'
    }

    $players = @($report.map.players)
    $userPlayers = @($players | Where-Object controller -eq 'User')
    if ($userPlayers.Count -ne 3) { throw "Expected exactly 3 User slots, found $($userPlayers.Count)." }
    foreach ($expected in @(@{ Id = 0; Race = 'Orc' }, @{ Id = 1; Race = 'NightElf' }, @{ Id = 9; Race = 'Human' })) {
        $player = $players | Where-Object id -eq $expected.Id
        if ($null -eq $player -or $player.controller -ne 'User' -or $player.race -ne $expected.Race) {
            throw "Unexpected cooperative slot Player($($expected.Id))."
        }
    }

    $preservedEntries = @(
        'war3map.doo', 'war3map.imp', 'war3map.mmp', 'war3map.shd', 'war3map.w3c',
        'war3map.w3e', 'war3map.w3r', 'war3map.w3s', 'war3map.w3u', 'war3map.wct',
        'war3map.wpm', 'war3map.wtg', 'war3mapMap.blp', 'war3mapUnits.doo'
    )
    foreach ($entry in $preservedEntries) {
        $safeName = $entry.Replace('\', '_')
        $sourceEntry = Join-Path $testRoot ('source-' + $safeName)
        $generatedEntry = Join-Path $testRoot ('generated-' + $safeName)
        Invoke-Inspect archive extract $SourceMap $entry $sourceEntry
        Invoke-Inspect archive extract $mapPath $entry $generatedEntry
        $sourceHash = (Get-FileHash -LiteralPath $sourceEntry -Algorithm SHA256).Hash
        $generatedHash = (Get-FileHash -LiteralPath $generatedEntry -Algorithm SHA256).Hash
        if ($sourceHash -ne $generatedHash) { throw "Non-target archive entry changed: $entry" }
    }

    $redAi = Join-Path $testRoot 'n07_red.ai'
    $lightBlueAi = Join-Path $testRoot 'n07_lightblue.ai'
    Invoke-Inspect archive extract (Join-Path $GamePath 'WAR3.MPQ') 'Scripts\n07_red.ai' $redAi
    Invoke-Inspect archive extract (Join-Path $GamePath 'WAR3.MPQ') 'Scripts\n07_lightblue.ai' $lightBlueAi
    if ((Get-FileHash $redAi -Algorithm SHA256).Hash -ne 'DC590B9442E4C2BE7DD855D6438E417871AFC49B5213683BB079D8300A3A9FA0') {
        throw 'The target 1.24e n07_red.ai does not match the validated official AI.'
    }
    if ((Get-FileHash $lightBlueAi -Algorithm SHA256).Hash -ne 'AF63184A705381244F426E6AEF228D0283CEEE0389CBB0442598A83BD4BEC385') {
        throw 'The target 1.24e n07_lightblue.ai does not match the validated official AI.'
    }

    $pjassOutput = & $PjassPath $commonJPath $blizzardJPath $scriptPath 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Generated script failed 1.24e JASS validation:`n$($pjassOutput -join "`n")"
    }
    $script = Get-Content -LiteralPath $scriptPath -Raw -Encoding UTF8
    if ($script.Contains("`r`r`n")) { throw 'Generated script contains CR-CR-LF line endings.' }

    function Assert-HeroInventory {
        param(
            [Parameter(Mandatory = $true)][string]$HeroVariable,
            [Parameter(Mandatory = $true)][string[]]$ExpectedItems
        )

        $pattern = "call UnitAddItemByIdSwapped\( '([^']+)', $([regex]::Escape($HeroVariable)) \)"
        $actualItems = @([regex]::Matches($script, $pattern) | ForEach-Object { $_.Groups[1].Value })
        if (($actualItems -join '/') -ne ($ExpectedItems -join '/')) {
            throw "Unexpected inventory for $HeroVariable. Expected $($ExpectedItems -join '/'), found $($actualItems -join '/')."
        }
    }

    function Assert-HeroSkills {
        param(
            [Parameter(Mandatory = $true)][string]$FunctionName,
            [Parameter(Mandatory = $true)][string]$HeroVariable,
            [Parameter(Mandatory = $true)][int]$ExpectedLevel,
            [Parameter(Mandatory = $true)][string[]]$ExpectedSkills
        )

        $body = [regex]::Match($script, "function $FunctionName takes nothing returns nothing(?s:.*?)endfunction").Value
        if (-not $body) { throw "Hero initialization function not found: $FunctionName" }
        $levelFragment = "call SetHeroLevel( $HeroVariable, $ExpectedLevel, false )"
        if (([regex]::Matches($body, [regex]::Escape($levelFragment))).Count -ne 1) {
            throw "Unexpected hero level initialization for $HeroVariable."
        }
        $skillPattern = "call SelectHeroSkill\( $([regex]::Escape($HeroVariable)), '([^']+)' \)"
        $actualSkills = @([regex]::Matches($body, $skillPattern) | ForEach-Object { $_.Groups[1].Value })
        if (($actualSkills -join '/') -ne ($ExpectedSkills -join '/')) {
            throw "Unexpected skills for $HeroVariable. Expected $($ExpectedSkills -join '/'), found $($actualSkills -join '/')."
        }
    }

    $required = @(
        'function CoopClassifySlots takes nothing returns nothing',
        'local mapcontrol thrallController = GetPlayerController(Player(0))',
        'local playerslotstate jainaSlot = GetPlayerSlotState(Player(9))',
        'function CoopStartThrallAI takes nothing returns nothing',
        'function CoopStartJainaAI takes nothing returns nothing',
        'call StartCampaignAI( Player(0), "n07_red.ai" )',
        'call StartCampaignAI( Player(9), "n07_lightblue.ai" )',
        'function CoopCommandThrallAI takes integer command, integer data returns nothing',
        'function CoopCommandJainaAI takes integer command, integer data returns nothing',
        'if ( not udg_CoopThrallAI ) then',
        'if ( not udg_CoopJainaAI ) then',
        'function CoopCapturePlayerState takes nothing returns nothing',
        'function CoopRestoreCameras takes nothing returns nothing',
        'function CoopFocusStartingHeroes takes nothing returns nothing',
        'function CoopVictory takes nothing returns nothing',
        'function CoopDefeat takes string message returns nothing',
        'function CoopShowStartupWarnings takes nothing returns nothing',
        'function CoopFinishDifficultySelection takes gamedifficulty difficulty, string notice returns nothing',
        'call TimerStart( udg_CoopDifficultyTimer, 30.00, false, null )',
        'call SetMapFlag( MAP_LOCK_ALLIANCE_CHANGES, false )',
        'call SetMapFlag( MAP_LOCK_RESOURCE_TRADING, false )',
        'call SetMapFlag( MAP_RESOURCE_TRADING_ALLIES_ONLY, true )',
        "call SetPlayerTechMaxAllowed( Player(0), 'Oshd', 0 )",
        "call SetPlayerTechMaxAllowed( Player(1), 'Ewar', 0 )",
        "call SetPlayerTechMaxAllowed( Player(9), 'Hblm', 0 )",
        "call CreateNUnitsAtLoc( 1, 'Etyr', udg_AAAP_NightElves, GetRectCenter(gg_rct_IntroTyrande00), 180.00 )",
        "call CreateNUnitsAtLoc( 1, 'Efur', udg_AAAP_NightElves, GetRectCenter(gg_rct_IntroTyrande00), 225.00 )",
        "call UnitAddItemByIdSwapped( 'cnhn', udg_Furion )",
        "call SetPlayerStateBJ( Player(0), PLAYER_STATE_RESOURCE_GOLD, 1000 )",
        "call SetPlayerStateBJ( Player(9), PLAYER_STATE_RESOURCE_LUMBER, 800 )"
    )
    foreach ($fragment in $required) {
        if (-not $script.Contains($fragment)) { throw "Missing script fragment: $fragment" }
    }

    $tftRestrictions = @{
        0 = @('Oshd', 'otbk', 'otbr', 'ovln', 'Rolf', 'Rorb', 'Robk', 'Ropm')
        1 = @('Ewar', 'Ewrd', 'efdr', 'emtg', 'eden', 'Rers', 'Rehs', 'Reeb', 'Reec', 'Rews', 'Repm')
        9 = @('Hblm', 'hspt', 'hdhw', 'hrtt', 'hvlt', 'Rhss', 'Rhrt', 'Rhpm', 'Rhfc', 'Rhcd')
    }
    foreach ($playerId in $tftRestrictions.Keys) {
        foreach ($rawcode in $tftRestrictions[$playerId]) {
            $fragment = "call SetPlayerTechMaxAllowed( Player($playerId), '$rawcode', 0 )"
            if (([regex]::Matches($script, [regex]::Escape($fragment))).Count -ne 1) {
                throw "Expected one Frozen Throne restriction for Player($playerId) '$rawcode'."
            }
        }
    }
    $tftExceptions = @{
        0 = @('ospw', 'ospm', 'Rowt', 'Robf')
        9 = @('hatw', 'Rhfs')
    }
    foreach ($playerId in $tftExceptions.Keys) {
        foreach ($rawcode in $tftExceptions[$playerId]) {
            $fragment = "call SetPlayerTechMaxAllowed( Player($playerId), '$rawcode', 0 )"
            if ($script.Contains($fragment)) {
                throw "Approved Frozen Throne exception must remain available for Player($playerId): '$rawcode'."
            }
        }
    }

    foreach ($fragment in @(
        'EVENT_PLAYER_LEAVE', 'InitGameCacheBJ(', 'RestoreUnitLocFacingAngleBJ(', 'SaveGameCacheBJ(',
        'SetNextLevelBJ(', 'SetCinematicAvailableBJ(', 'ALLIANCE_SHARED_CONTROL, true',
        'ALLIANCE_SHARED_ADVANCED_CONTROL, true', 'SetPlayerController( Player(0), MAP_CONTROL_COMPUTER )',
        'SetPlayerController( Player(9), MAP_CONTROL_COMPUTER )', 'CinematicModeBJ( true, GetPlayersAll() )',
        'QuestMessageBJ( GetPlayersAll(),'
    )) {
        if ($script.Contains($fragment)) { throw "Forbidden script fragment remains: $fragment" }
    }

    if (([regex]::Matches($script, 'GetPlayerController\(')).Count -ne 3) { throw 'Slot controllers must be queried exactly once per cooperative slot.' }
    if (([regex]::Matches($script, 'GetPlayerSlotState\(')).Count -ne 3) { throw 'Slot states must be queried exactly once per cooperative slot.' }
    foreach ($playerId in @(0, 1, 9)) {
        $skipCount = ([regex]::Matches($script, "TriggerRegisterPlayerEventEndCinematic\([^\r\n]*Player\($playerId\)")).Count
        if ($skipCount -ne 5) { throw "Expected five cinematic skip registrations for Player($playerId), found $skipCount." }
    }

    Assert-HeroInventory -HeroVariable 'udg_Tyrande' -ExpectedItems @('gcel', 'rat6', 'rag1', 'rnsp', 'rin1', 'mnst')
    Assert-HeroInventory -HeroVariable 'udg_Furion' -ExpectedItems @('cnhn', 'nspi', 'rwiz', 'ciri', 'rat9', 'rin1')
    Assert-HeroInventory -HeroVariable 'udg_Thrall' -ExpectedItems @('nspi', 'ratc', 'rat9', 'cnob', 'rin1', 'rin1')
    Assert-HeroInventory -HeroVariable 'udg_Jaina' -ExpectedItems @('ckng', 'hcun', 'ciri', 'cnob', 'gcel', 'bspd')
    Assert-HeroSkills -FunctionName 'Trig_Load_Tyrande_Actions' -HeroVariable 'udg_Tyrande' -ExpectedLevel 7 -ExpectedSkills @('AEst', 'AHfa', 'AEar', 'AEar', 'AEar', 'AEsf')
    Assert-HeroSkills -FunctionName 'Trig_Load_Furion_Actions' -HeroVariable 'udg_Furion' -ExpectedLevel 7 -ExpectedSkills @('AEer', 'AEer', 'AEfn', 'AEfn', 'AEah', 'AEtq')

    foreach ($resourceMode in @(
        @{ PlayerId = 0; AiFlag = 'udg_CoopThrallAI' },
        @{ PlayerId = 9; AiFlag = 'udg_CoopJainaAI' }
    )) {
        $playerId = $resourceMode.PlayerId
        $aiFlag = $resourceMode.AiFlag
        $resourcePattern = "if \( $aiFlag \) then\s+call SetPlayerStateBJ\( Player\($playerId\), PLAYER_STATE_RESOURCE_GOLD, 6500 \)\s+call SetPlayerStateBJ\( Player\($playerId\), PLAYER_STATE_RESOURCE_LUMBER, 6500 \)\s+else\s+call SetPlayerStateBJ\( Player\($playerId\), PLAYER_STATE_RESOURCE_GOLD, 1000 \)\s+call SetPlayerStateBJ\( Player\($playerId\), PLAYER_STATE_RESOURCE_LUMBER, 800 \)\s+endif"
        if (([regex]::Matches($script, $resourcePattern)).Count -ne 1) {
            throw "Player($playerId) does not have the expected AI/human starting-resource split."
        }
    }

    foreach ($subsidy in @(
        @{ Function = 'Trig_Thrall_Gold_Actions'; Flag = 'udg_CoopThrallAI'; Resource = 'GOLD'; PlayerId = 0 },
        @{ Function = 'Trig_Thrall_Lumber_Actions'; Flag = 'udg_CoopThrallAI'; Resource = 'LUMBER'; PlayerId = 0 },
        @{ Function = 'Trig_Jaina_Gold_Actions'; Flag = 'udg_CoopJainaAI'; Resource = 'GOLD'; PlayerId = 9 },
        @{ Function = 'Trig_Jaina_Lumber_Actions'; Flag = 'udg_CoopJainaAI'; Resource = 'LUMBER'; PlayerId = 9 }
    )) {
        $body = [regex]::Match($script, "function $($subsidy.Function) takes nothing returns nothing(?s:.*?)endfunction").Value
        if (-not $body.Contains("if ( not $($subsidy.Flag) ) then") -or
            -not $body.Contains("call AdjustPlayerStateBJ( 100, Player($($subsidy.PlayerId)), PLAYER_STATE_RESOURCE_$($subsidy.Resource) )")) {
            throw "Subsidy function $($subsidy.Function) is not gated by its fixed AI mode."
        }
    }
    foreach ($fixedRacePlayerId in @(0, 1, 2, 3, 4, 6, 7, 8, 9, 10)) {
        $fixed = "call SetPlayerRaceSelectable( Player($fixedRacePlayerId), false )"
        if (([regex]::Matches($script, [regex]::Escape($fixed))).Count -ne 1) {
            throw "Player($fixedRacePlayerId) race is not fixed exactly once."
        }
    }

    $main = [regex]::Match($script, 'function main takes nothing returns nothing(?s:.*?)endfunction').Value
    if ($main.Contains('call InitCustomTriggers(  )') -or $main.Contains('call RunInitializationTriggers(  )')) {
        throw 'Original triggers must remain deferred until difficulty selection completes.'
    }
    if (-not $main.Contains('call CoopClassifySlots(  )') -or -not $main.Contains('call CoopScheduleDifficultySelection(  )')) {
        throw 'main does not classify slots and schedule difficulty selection.'
    }

    $stringsBytes = [IO.File]::ReadAllBytes($stringsPath)
    if ($stringsBytes.Length -ge 3 -and $stringsBytes[0] -eq 0xEF -and $stringsBytes[1] -eq 0xBB -and $stringsBytes[2] -eq 0xBF) {
        throw 'war3map.wts must be UTF-8 without BOM for 1.24e.'
    }
    $strings = [Text.UTF8Encoding]::new($false, $true).GetString($stringsBytes)
    foreach ($text in @('选择难度', '普通', '困难', '大厅配置错误')) {
        if (-not $strings.Contains($text)) { throw "Missing localized string: $text" }
    }

    Write-Host 'NightElf07 cooperative build regression passed.'
    Write-Host "Output SHA-256: $((Get-FileHash -LiteralPath $mapPath -Algorithm SHA256).Hash)"
}
finally {
    if (Test-Path -LiteralPath $testRoot) {
        Remove-Item -LiteralPath $testRoot -Recurse -Force
    }
}
