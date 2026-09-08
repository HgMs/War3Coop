[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$SourceMap,

    [string]$GamePath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$toolRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$repoRoot = (Resolve-Path (Join-Path $toolRoot '..\..')).Path
$inspect = Join-Path $repoRoot 'tools\wc3-inspect\wc3-inspect.ps1'
$builder = Join-Path $toolRoot 'wc3-coop-build.ps1'
$testRoot = Join-Path $repoRoot ('build\test-orc08-' + [guid]::NewGuid().ToString('N'))
$mapPath = Join-Path $testRoot 'Orc08-Coop-Development.w3x'
$reportPath = Join-Path $testRoot 'inspection.json'
$scriptPath = Join-Path $testRoot 'war3map.j'
$stringsPath = Join-Path $testRoot 'war3map.wts'
$sourceUnitDataPath = Join-Path $testRoot 'source-war3map.w3u'
$generatedUnitDataPath = Join-Path $testRoot 'generated-war3map.w3u'
$unitBalancePath = Join-Path $testRoot 'UnitBalance.slk'
$war3NetRoot = Join-Path $repoRoot 'build\wc3-coop-build\publish'

if (-not $GamePath) {
    $GamePath = Join-Path $repoRoot 'game\Warcraft3-1.24e'
}

try {
    New-Item -ItemType Directory -Path $testRoot | Out-Null
    & $builder orc08 $SourceMap $mapPath
    if ($LASTEXITCODE -ne 0) { throw 'Map build failed.' }

    & $inspect map inspect $mapPath --output $reportPath
    if ($LASTEXITCODE -ne 0) { throw 'Generated map inspection failed.' }
    & $inspect archive extract $mapPath 'war3map.j' $scriptPath
    if ($LASTEXITCODE -ne 0) { throw 'Generated script extraction failed.' }
    & $inspect archive extract $mapPath 'war3map.wts' $stringsPath
    if ($LASTEXITCODE -ne 0) { throw 'Generated trigger-string extraction failed.' }
    & $inspect archive extract $SourceMap 'war3map.w3u' $sourceUnitDataPath
    if ($LASTEXITCODE -ne 0) { throw 'Source unit object data extraction failed.' }
    & $inspect archive extract $mapPath 'war3map.w3u' $generatedUnitDataPath
    if ($LASTEXITCODE -ne 0) { throw 'Generated unit object data extraction failed.' }
    & $inspect archive extract (Join-Path $GamePath 'War3Patch.mpq') 'Units\UnitBalance.slk' $unitBalancePath
    if ($LASTEXITCODE -ne 0) { throw '1.24e UnitBalance.slk extraction failed.' }

    $report = Get-Content -LiteralPath $reportPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $players = @($report.map.players)
    $userPlayers = @($players | Where-Object { $_.controller -eq 'User' })
    if ($userPlayers.Count -ne 2) { throw "Expected 2 user slots, found $($userPlayers.Count)." }

    $playerOne = $players | Where-Object { $_.id -eq 0 }
    $playerTwo = $players | Where-Object { $_.id -eq 9 }
    $unusedHuman = $players | Where-Object { $_.id -eq 7 }
    if ($null -eq $playerOne -or $playerOne.controller -ne 'User' -or $playerOne.race -ne 'Orc') {
        throw 'Player 1 must remain the original Orc user faction.'
    }
    if ($null -eq $playerTwo -or $playerTwo.controller -ne 'User' -or $playerTwo.race -ne 'Human') {
        throw 'Player 2 must be the original Player(9) Human expedition faction.'
    }
    if ($null -eq $unusedHuman -or $unusedHuman.controller -ne 'Neutral' -or $unusedHuman.race -ne 'Human') {
        throw 'The unused Player(7) slot must remain the original neutral Human slot.'
    }

    if ($report.map.format_version -ne 18) { throw "Expected map format 18, found $($report.map.format_version)." }
    if ($report.map.recommended_players -ne '2') { throw 'Recommended player count was not updated.' }
    if ($report.content.unit_count -ne 291) { throw 'Placed unit count changed unexpectedly.' }
    if ($report.content.doodad_count -ne 3189) { throw 'Doodad count changed unexpectedly.' }

    $expectedUnitDataHash = '0E1079EF01C6824868CA9106DC42111189342DA93003C376D8BA539C556CEF15'
    $sourceUnitDataHash = (Get-FileHash -LiteralPath $sourceUnitDataPath -Algorithm SHA256).Hash
    $generatedUnitDataHash = (Get-FileHash -LiteralPath $generatedUnitDataPath -Algorithm SHA256).Hash
    if ($sourceUnitDataHash -ne $expectedUnitDataHash) {
        throw "Unexpected official Orc08 war3map.w3u hash: $sourceUnitDataHash."
    }
    if ($generatedUnitDataHash -ne $sourceUnitDataHash) {
        throw "Generated war3map.w3u differs from the official source: $generatedUnitDataHash."
    }

    Add-Type -Path (Join-Path $war3NetRoot 'War3Net.Common.dll')
    Add-Type -Path (Join-Path $war3NetRoot 'War3Net.Build.Core.dll')
    Add-Type -Path (Join-Path $war3NetRoot 'War3Net.IO.Slk.dll')

    $campaignHeroIds = @('Othr', 'Ocbh', 'Hjai', 'Opgh', 'Ogrh')
    $unitBalanceStream = [System.IO.File]::OpenRead($unitBalancePath)
    try {
        $unitBalance = [War3Net.IO.Slk.SylkParser]::new().Parse($unitBalanceStream, $true)
    }
    finally {
        $unitBalanceStream.Dispose()
    }
    $unitBalanceHeaders = for ($column = 0; $column -le $unitBalance.Columns; $column++) {
        [string]$unitBalance[$column, 0]
    }
    $unitIdColumn = [array]::IndexOf($unitBalanceHeaders, 'unitBalanceID')
    $foodUsedColumn = [array]::IndexOf($unitBalanceHeaders, 'fused')
    if ($unitIdColumn -lt 0 -or $foodUsedColumn -lt 0) {
        throw 'UnitBalance.slk is missing unitBalanceID or fused.'
    }

    $effectiveHeroFood = @{}
    for ($row = 1; $row -le $unitBalance.Rows; $row++) {
        $unitId = [string]$unitBalance[$unitIdColumn, $row]
        if ($unitId -notin $campaignHeroIds) { continue }
        $rawFood = $unitBalance[$foodUsedColumn, $row]
        if ($null -eq $rawFood -or [string]$rawFood -eq '' -or [string]$rawFood -eq '-') {
            $effectiveHeroFood[$unitId] = 0
        }
        elseif ($rawFood -is [int]) {
            $effectiveHeroFood[$unitId] = [int]$rawFood
        }
        else {
            throw "Unsupported fused value '$rawFood' for campaign hero $unitId."
        }
    }

    $unitDataStream = [System.IO.File]::OpenRead($generatedUnitDataPath)
    $unitDataReader = [System.IO.BinaryReader]::new($unitDataStream)
    try {
        $unitObjectData = [War3Net.Build.Extensions.BinaryReaderExtensions]::ReadUnitObjectData($unitDataReader)
    }
    finally {
        $unitDataReader.Dispose()
        $unitDataStream.Dispose()
    }
    foreach ($unitModification in @($unitObjectData.BaseUnits) + @($unitObjectData.NewUnits)) {
        $effectiveIdValue = if ($unitModification.NewId -eq 0) { $unitModification.OldId } else { $unitModification.NewId }
        $effectiveId = [War3Net.Common.Extensions.Int32Extensions]::ToRawcode($effectiveIdValue)
        if ($effectiveId -notin $campaignHeroIds) { continue }
        foreach ($modification in $unitModification.Modifications) {
            $fieldId = [War3Net.Common.Extensions.Int32Extensions]::ToRawcode($modification.Id)
            if ($fieldId -eq 'ufoo') {
                $effectiveHeroFood[$effectiveId] = $modification.ValueAsInt
            }
        }
    }
    foreach ($heroId in $campaignHeroIds) {
        if (-not $effectiveHeroFood.ContainsKey($heroId)) {
            throw "Campaign hero $heroId is missing from the 1.24e unit data overlay."
        }
        if ($effectiveHeroFood[$heroId] -ne 0) {
            throw "Campaign hero $heroId consumes $($effectiveHeroFood[$heroId]) food; expected 0."
        }
    }

    $script = Get-Content -LiteralPath $scriptPath -Raw -Encoding UTF8
    $required = @(
        'SetPlayerController( Player(9), MAP_CONTROL_USER )',
        'SetPlayerRaceSelectable( Player(0), false )',
        'SetPlayerRaceSelectable( Player(9), false )',
        'constant integer    COOP_HUMAN_EXPEDITION_PLAYER_ID = 9',
        'function CoopClassifyPlayerTwo takes nothing returns nothing',
        'set udg_CoopPlayerTwoHuman = ( GetPlayerController(Player(COOP_HUMAN_EXPEDITION_PLAYER_ID)) == MAP_CONTROL_USER and GetPlayerSlotState(Player(COOP_HUMAN_EXPEDITION_PLAYER_ID)) == PLAYER_SLOT_STATE_PLAYING )',
        'function CoopConfigurePlayerTwoSystems takes nothing returns nothing',
        'function CoopStartJainaAI takes nothing returns nothing',
        'call StartCampaignAI( Player(9), "o08_Jaina.ai" )',
        'function CoopCommandJainaAI takes integer command, integer data returns nothing',
        'call CommandAI( Player(9), command, data )',
        'function CoopEnableJainaAssaults takes nothing returns nothing',
        'function CoopEnableJainaHelp takes nothing returns nothing',
        'call CoopInitializeJainaEconomy(  )',
        'PLAYER_STATE_RESOURCE_GOLD, 1000',
        'PLAYER_STATE_RESOURCE_LUMBER, 400',
        'PLAYER_STATE_RESOURCE_GOLD, 10000',
        'PLAYER_STATE_RESOURCE_LUMBER, 10000',
        'call CoopGrantJainaAIBonus(  )',
        'call CoopVictory(  )',
        'call CoopDefeat(',
        'call CoopClassifyPlayerTwo(  )',
        'call CoopBeginDifficultySelection(  )',
        'call CoopConfigurePlayerTwoSystems(  )',
        'function CoopFinishDifficultySelection takes gamedifficulty difficulty returns nothing',
        'call SetGameDifficulty( difficulty )',
        'call CoopFinishDifficultySelection( MAP_DIFFICULTY_NORMAL )',
        'call CoopFinishDifficultySelection( MAP_DIFFICULTY_HARD )',
        'call TimerStart( udg_CoopDifficultyTimer, 30.00, false, null )',
        'call SetMapFlag( MAP_LOCK_ALLIANCE_CHANGES, false )',
        'call SetMapFlag( MAP_ALLIANCE_CHANGES_HIDDEN, false )',
        'call SetMapFlag( MAP_LOCK_RESOURCE_TRADING, false )',
        'call SetMapFlag( MAP_RESOURCE_TRADING_ALLIES_ONLY, true )',
        'function CoopRestoreEnemyDiplomacy takes nothing returns nothing',
        'call CoopSetEnemyPair( Player(PLAYER_NEUTRAL_AGGRESSIVE) )',
        'ALLIANCE_SHARED_VISION, true',
        'PLAYER_STATE_ALLIED_VICTORY, 1',
        'function CoopCreateWaterfallVision takes nothing returns nothing',
        'function CoopDestroyWaterfallVision takes nothing returns nothing',
        'function CoopCaptureCameras takes nothing returns nothing',
        'function CoopRestoreSavedCameras takes nothing returns nothing',
        'call RemoveLocation( udg_PlayerCameraPosition )',
        'call RemoveLocation( udg_CoopPlayerCameraPosition )',
        'set udg_PlayerCameraPosition = null',
        'set udg_CoopPlayerCameraPosition = null',
        'return ( GetOwningPlayer(whichUnit) == Player(0) or ( udg_CoopPlayerTwoHuman and GetOwningPlayer(whichUnit) == Player(9) ) )',
        'set udg_CoopSelectedUnits = GetUnitsSelectedAll(Player(9))',
        'call SelectGroupForPlayerBJ( udg_CoopSelectedUnits, Player(9) )',
        'call SelectUnitForPlayerSingle( udg_Thrall, Player(0) )',
        'call SelectUnitForPlayerSingle( udg_Jaina, Player(9) )',
        'call PanCameraToTimedLocForPlayer( Player(9), GetRectCenter(gg_rct_JainasTown), 0 )',
        'call PanCameraToTimedLocForPlayer( Player(9), udg_CoopPlayerCameraPosition, 0.00 )',
        "call UnitAddItemByIdSwapped( 'ckng', udg_Thrall )",
        "call UnitAddItemByIdSwapped( 'ratc', udg_Thrall )",
        "call UnitAddItemByIdSwapped( 'ciri', udg_Thrall )",
        "call UnitAddItemByIdSwapped( 'nspi', udg_Cairne )",
        "call UnitAddItemByIdSwapped( 'bgst', udg_Cairne )",
        "call UnitAddItemByIdSwapped( 'rwiz', udg_Cairne )",
        "call UnitAddItemByIdSwapped( 'ofir', udg_Cairne )",
        "call UnitAddItemByIdSwapped( 'gsou', udg_Thrall )",
        'GetPlayerStructureCount(udg_Player, true) <= 0',
        'StartTimerBJ( udg_JainaAssaultTimer, true, 300.00 )',
        'StartTimerBJ( udg_JainaAssaultTimer, true, 540.00 )',
        'local player p = Player(9)',
        "set gg_unit_Hjai_0021 = CreateUnit( p, 'Hjai'"
    )
    foreach ($fragment in $required) {
        if (-not $script.Contains($fragment)) { throw "Missing script fragment: $fragment" }
    }

    $forbidden = @(
        'SetPlayerController( Player(7), MAP_CONTROL_USER )',
        'SetUnitOwner( udg_Cairne',
        'function CoopSecondPlayer',
        'function CoopTransferUnits',
        'function CoopPlayerTwoLeaves',
        'function CoopStructureCount',
        'EVENT_PLAYER_LEAVE',
        "CreateUnit( Player(9), 'ostr'",
        "CreateUnit( Player(9), 'obar'",
        "CreateUnit( Player(9), 'opeo'",
        'call SelectUnitSingle( udg_Thrall )',
        'call SelectGroupBJ( udg_SelectedUnits )',
        'CustomDefeatBJ( udg_Player',
        'CustomVictoryBJ( udg_Player',
        'InitGameCacheBJ(',
        'RestoreUnitLocFacingAngleBJ(',
        'SaveGameCacheBJ(',
        'GetLastCreatedGameCacheBJ(',
        'PLAYER_STATE_RESOURCE_FOOD_USED',
        'SetCinematicAvailableBJ(',
        'SetCampaignAvailableBJ(',
        'SetMissionAvailableBJ(',
        'ALLIANCE_SHARED_CONTROL, true',
        'ALLIANCE_SHARED_ADVANCED_CONTROL, true',
        'SetDefaultDifficulty(',
        'GetPlayerController(Player(9)) !=',
        'GetPlayerSlotState(Player(9)) !=',
        'Blz'
    )
    foreach ($fragment in $forbidden) {
        if ($script.Contains($fragment)) { throw "Forbidden wrong-direction fragment remains: $fragment" }
    }

    $startAiCount = ([regex]::Matches($script, [regex]::Escape('call StartCampaignAI( Player(9), "o08_Jaina.ai" )'))).Count
    if ($startAiCount -ne 1) { throw "Expected exactly one gated original Jaina AI call, found $startAiCount." }
    $commandAiCount = ([regex]::Matches($script, [regex]::Escape('call CommandAI( Player(9), command, data )'))).Count
    if ($commandAiCount -ne 1) { throw "Expected exactly one gated original Jaina CommandAI call, found $commandAiCount." }

    $controllerChecks = ([regex]::Matches($script, [regex]::Escape('GetPlayerController(Player(COOP_HUMAN_EXPEDITION_PLAYER_ID))'))).Count
    if ($controllerChecks -ne 1) { throw "Expected one startup controller check, found $controllerChecks." }
    $slotChecks = ([regex]::Matches($script, [regex]::Escape('GetPlayerSlotState(Player(COOP_HUMAN_EXPEDITION_PLAYER_ID))'))).Count
    if ($slotChecks -ne 1) { throw "Expected one startup slot-state check, found $slotChecks." }
    $allControllerChecks = ([regex]::Matches($script, 'GetPlayerController\(')).Count
    if ($allControllerChecks -ne 1) { throw "Expected no controller checks after startup classification, found $allControllerChecks total." }
    $allSlotChecks = ([regex]::Matches($script, 'GetPlayerSlotState\(')).Count
    if ($allSlotChecks -ne 1) { throw "Expected no slot-state checks after startup classification, found $allSlotChecks total." }
    $structureChecks = ([regex]::Matches($script, [regex]::Escape('GetPlayerStructureCount(udg_Player, true) <= 0'))).Count
    if ($structureChecks -ne 1) { throw "Expected the original Player 1 structure-loss condition exactly once, found $structureChecks." }
    $soulGemAdds = ([regex]::Matches($script, [regex]::Escape("call UnitAddItemByIdSwapped( 'gsou', udg_Thrall )"))).Count
    if ($soulGemAdds -ne 3) { throw "Expected the two intro grants plus the original Thrall-revival replacement, found $soulGemAdds." }
    $cameraCaptureCalls = ([regex]::Matches($script, [regex]::Escape('call CoopCaptureCameras(  )'))).Count
    if ($cameraCaptureCalls -ne 2) { throw "Expected camera capture at the two player-interrupting cinematics, found $cameraCaptureCalls." }
    $cameraRestoreCalls = ([regex]::Matches($script, [regex]::Escape('call CoopRestoreSavedCameras(  )'))).Count
    if ($cameraRestoreCalls -ne 4) { throw "Expected camera restoration on both normal and skipped cinematic paths, found $cameraRestoreCalls." }
    $playerTwoEndCinematicEvents = ([regex]::Matches($script, 'TriggerRegisterPlayerEventEndCinematic\([^\r\n]*Player\(9\)')).Count
    if ($playerTwoEndCinematicEvents -ne 4) { throw "Expected four player 2 cinematic-skip registrations, found $playerTwoEndCinematicEvents." }

    foreach ($seconds in @(240, 90, 60)) {
        $timerCall = "call StartTimerBJ( udg_InfernalFallTimer, true, $seconds.00 )"
        $timerCount = ([regex]::Matches($script, [regex]::Escape($timerCall))).Count
        if ($timerCount -ne 1) { throw "Expected original infernal timer $seconds seconds exactly once, found $timerCount." }
    }
    $infernalRegistration = 'call TriggerRegisterTimerExpireEventBJ( gg_trg_Every_XSeconds_Summon_Infernals_Go, udg_InfernalFallTimer )'
    $infernalRegistrationCount = ([regex]::Matches($script, [regex]::Escape($infernalRegistration))).Count
    if ($infernalRegistrationCount -ne 1) { throw "Expected one periodic infernal timer registration, found $infernalRegistrationCount." }
    $infernalGo = [regex]::Match($script, 'function Trig_Every_XSeconds_Summon_Infernals_Go_Actions takes nothing returns nothing(?s:.*?)endfunction').Value
    $periodicSummons = ([regex]::Matches($infernalGo, [regex]::Escape('call ConditionalTriggerExecute( gg_trg_Every_XSeconds_Summon_Infernals )'))).Count
    if ($periodicSummons -ne 2) { throw "Expected two infernal summons per periodic timer expiry, found $periodicSummons." }
    $firstInfernalConditions = [regex]::Match($script, 'function Trig_First_Infernal_Wave_Func001001 takes nothing returns boolean(?s:.*?)function Trig_First_Infernal_Wave_Actions').Value
    $expectedFirstWaveCounts = @{ 'MAP_DIFFICULTY_EASY' = 2; 'MAP_DIFFICULTY_NORMAL' = 4; 'MAP_DIFFICULTY_HARD' = 8 }
    foreach ($difficulty in $expectedFirstWaveCounts.Keys) {
        $actualCount = ([regex]::Matches($firstInfernalConditions, [regex]::Escape($difficulty))).Count
        if ($actualCount -ne $expectedFirstWaveCounts[$difficulty]) {
            throw "Expected $($expectedFirstWaveCounts[$difficulty]) first-wave infernal condition(s) for $difficulty, found $actualCount."
        }
    }

    $mainFunction = [regex]::Match($script, 'function main takes nothing returns nothing(?s:.*?)endfunction').Value
    if ($mainFunction.Contains('call InitCustomTriggers(  )') -or $mainFunction.Contains('call RunInitializationTriggers(  )')) {
        throw 'Original trigger initialization must remain deferred until difficulty selection finishes.'
    }
    $difficultyFinish = [regex]::Match($script, 'function CoopFinishDifficultySelection takes gamedifficulty difficulty returns nothing(?s:.*?)endfunction').Value
    if ($difficultyFinish.IndexOf('call SetGameDifficulty( difficulty )') -gt $difficultyFinish.IndexOf('call InitCustomTriggers(  )')) {
        throw 'Game difficulty must be set before original triggers are initialized.'
    }

    $stringsBytes = [System.IO.File]::ReadAllBytes($stringsPath)
    if ($stringsBytes.Length -lt 3 -or $stringsBytes[0] -ne 0xEF -or $stringsBytes[1] -ne 0xBB -or $stringsBytes[2] -ne 0xBF) {
        throw 'war3map.wts must preserve the original UTF-8 BOM for classic-client Chinese text.'
    }
    $strictUtf8 = [System.Text.UTF8Encoding]::new($true, $true)
    $strings = $strictUtf8.GetString($stringsBytes)
    foreach ($text in @('选择难度', '普通', '困难', '正在等待玩家 1 选择难度')) {
        if (-not $strings.Contains($text)) { throw "Missing localized difficulty text: $text" }
    }

    $guardedPlayerTwoLinePatterns = @(
        'TriggerRegisterPlayerEventEndCinematic\([^\r\n]*Player\(9\)',
        '(CameraSetupApplyForPlayer|CameraSetupApplyForceDurationForPlayer|PanCameraToTimedLocForPlayer|CameraSetSmoothingFactorForPlayer|CameraSetEQNoiseForPlayer|CameraClearNoiseForPlayer)\([^\r\n]*Player\(9\)',
        '(SelectGroupForPlayerBJ|SelectUnitForPlayerSingle)\([^\r\n]*Player\(9\)'
    )
    $scriptLines = $script -split '\r?\n'
    for ($lineIndex = 0; $lineIndex -lt $scriptLines.Count; $lineIndex++) {
        $line = $scriptLines[$lineIndex]
        if ($guardedPlayerTwoLinePatterns | Where-Object { $line -match $_ }) {
            $previousLine = if ($lineIndex -gt 0) { $scriptLines[$lineIndex - 1].Trim() } else { '' }
            if ($previousLine -notmatch '^if \( udg_CoopPlayerTwoHuman(?: \) then| and .+\) then)$') {
                throw "Player 2 local-only call is not directly guarded at script line $($lineIndex + 1): $($line.Trim())"
            }
        }
    }

    Write-Host 'Orc08 formal-route cooperative build regression passed.'
}
finally {
    if (Test-Path -LiteralPath $testRoot) {
        Remove-Item -LiteralPath $testRoot -Recurse -Force
    }
}
