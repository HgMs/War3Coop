[CmdletBinding()]
param(
    [string]$GamePath,
    [switch]$Update,
    [switch]$Check
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($Update -and $Check) {
    throw 'Choose either -Update or -Check.'
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if (-not $GamePath) {
    $GamePath = Join-Path $repoRoot 'game\Warcraft3-1.24e'
}
$GamePath = (Resolve-Path -LiteralPath $GamePath).Path

$manifestPath = Join-Path $repoRoot 'docs\campaigns.json'
$evidencePath = Join-Path $repoRoot 'docs\original-loading-screens.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding utf8 | ConvertFrom-Json

$entrySets = [ordered]@{
    '00-兽族序章-兽族大逃亡' = @(
        'game:Maps\Campaign\Prologue01.w3m',
        'game:Maps\Campaign\Prologue02.w3m',
        'demo:Demo03.w3m',
        'demo:Demo04.w3m',
        'demo:Demo05.w3m'
    )
    '01-人族战役-洛丹伦大陆的灾难' = 1..9 | ForEach-Object { 'game:Maps\Campaign\Human{0:D2}.w3m' -f $_ }
    '02-不死族战役-诅咒之路' = 1..8 | ForEach-Object { 'game:Maps\Campaign\Undead{0:D2}.w3m' -f $_ }
    '03-兽族战役-入侵卡利姆多' = 1..8 | ForEach-Object { 'game:Maps\Campaign\Orc{0:D2}.w3m' -f $_ }
    '04-暗夜精灵战役-永恒的结束' = 1..7 | ForEach-Object { 'game:Maps\Campaign\NightElf{0:D2}.w3m' -f $_ }
    '05-暗夜精灵战役-恐怖之潮' = 1..8 | ForEach-Object { 'game:Maps\FrozenThrone\Campaign\NightElfX{0:D2}.w3x' -f $_ }
    '06-血精灵战役-血精灵的诅咒' = @(
        'game:Maps\FrozenThrone\Campaign\HumanX01.w3x',
        'game:Maps\FrozenThrone\Campaign\HumanX02.w3x',
        'game:Maps\FrozenThrone\Campaign\HumanX03.w3x',
        'game:Maps\FrozenThrone\Campaign\HumanX03Secret.w3x',
        'game:Maps\FrozenThrone\Campaign\HumanX04.w3x',
        'game:Maps\FrozenThrone\Campaign\HumanX05.w3x',
        'game:Maps\FrozenThrone\Campaign\HumanX06.w3x'
    )
    '07-不死族战役-诅咒者的遗物' = @(
        'game:Maps\FrozenThrone\Campaign\UndeadX01.w3x',
        'game:Maps\FrozenThrone\Campaign\UndeadX02.w3x',
        'game:Maps\FrozenThrone\Campaign\UndeadX03.w3x',
        'game:Maps\FrozenThrone\Campaign\UndeadX04.w3x',
        'game:Maps\FrozenThrone\Campaign\UndeadX05.w3x',
        'game:Maps\FrozenThrone\Campaign\UndeadX06.w3x',
        'game:Maps\FrozenThrone\Campaign\UndeadX07a.w3x',
        'game:Maps\FrozenThrone\Campaign\UndeadX07b.w3x',
        'game:Maps\FrozenThrone\Campaign\UndeadX07c.w3x',
        'game:Maps\FrozenThrone\Campaign\UndeadX08.w3x'
    )
    '08-兽族额外战役-多洛特的成立' = @(
        'game:Maps\FrozenThrone\Campaign\OrcX01.w3x',
        'game:Maps\FrozenThrone\Campaign\OrcX02.w3x',
        'game:Maps\FrozenThrone\Campaign\OrcX03a.w3x'
    )
}

$inspectWrapper = Join-Path $repoRoot 'tools\wc3-inspect\wc3-inspect.ps1'
& $inspectWrapper game version $GamePath | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw 'Could not build or run wc3-inspect.'
}

$dotnet = Join-Path $repoRoot 'build\dotnet\dotnet.exe'
$inspectAssembly = Join-Path $repoRoot 'build\wc3-inspect\publish\wc3-inspect.dll'

function Invoke-Inspector {
    param([Parameter(Mandatory)][string[]]$Arguments)

    $json = & $dotnet $inspectAssembly @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "wc3-inspect failed: $($Arguments -join ' ')"
    }
    return ($json -join "`n") | ConvertFrom-Json
}

function Write-Utf8File {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Content
    )

    [System.IO.File]::WriteAllText($Path, $Content, [System.Text.UTF8Encoding]::new($false))
}

function Normalize-Text {
    param([Parameter(Mandatory)][string]$Content)

    return (($Content -replace "`r`n", "`n").TrimEnd() + "`n")
}

$gameVersionReport = Invoke-Inspector -Arguments @('game', 'version', $GamePath)
$gameVersion = $gameVersionReport.game_version
$workRoot = Join-Path $repoRoot 'build\original-loading-screens'
$demoCampaignPath = Join-Path $GamePath 'Campaigns\DemoCampaign.w3n'
if (-not (Test-Path -LiteralPath $demoCampaignPath -PathType Leaf)) {
    $demoCampaignPath = Join-Path $workRoot 'DemoCampaign.w3n'
    Invoke-Inspector -Arguments @(
        'archive', 'extract', (Join-Path $GamePath 'War3x.mpq'), 'DemoCampaign.w3n',
        $demoCampaignPath, '--overwrite'
    ) | Out-Null
}
$demoCampaignReport = Invoke-Inspector -Arguments @('campaign', 'inspect', $demoCampaignPath)

$records = [System.Collections.Generic.List[object]]::new()
foreach ($campaign in $manifest) {
    if (-not $entrySets.Contains($campaign.directory)) {
        throw "No official-map mapping for campaign: $($campaign.directory)"
    }

    $entries = $entrySets[$campaign.directory]
    if ($entries.Count -ne $campaign.missions.Count) {
        throw "Mapping count mismatch for $($campaign.directory): $($entries.Count) entries for $($campaign.missions.Count) missions."
    }

    for ($index = 0; $index -lt $campaign.missions.Count; $index++) {
        $mission = $campaign.missions[$index]
        $mapping = $entries[$index]
        $mode, $entry = $mapping -split ':', 2

        if ($mode -eq 'game') {
            $report = Invoke-Inspector -Arguments @('game', 'inspect', $GamePath, '--entry', $entry)
            $sourceContainer = Split-Path -Leaf $report.source.archive
            $sourceHash = $report.source.sha256
        }
        elseif ($mode -eq 'demo') {
            $mapPath = Join-Path $workRoot $entry
            Invoke-Inspector -Arguments @(
                'archive', 'extract', $demoCampaignPath, $entry, $mapPath, '--overwrite'
            ) | Out-Null
            $report = Invoke-Inspector -Arguments @('map', 'inspect', $mapPath)
            $sourceContainer = 'Campaigns\DemoCampaign.w3n'
            $sourceHash = $report.source.sha256
        }
        else {
            throw "Unknown mapping mode: $mode"
        }

        if ([string]::IsNullOrWhiteSpace($report.map.loading_screen.text)) {
            throw "Missing localized loading-screen text for $($campaign.directory)/$($mission.directory) ($entry)."
        }

        $records.Add([ordered]@{
            campaignDirectory = $campaign.directory
            missionDirectory = $mission.directory
            number = $mission.number
            titleZh = $mission.titleZh
            titleEn = $mission.titleEn
            sourceContainer = $sourceContainer
            sourceEntry = $entry
            sourceSha256 = $sourceHash
            formatVersion = $report.map.format_version
            loadingScreen = [ordered]@{
                title = $report.map.loading_screen.title
                subtitle = $report.map.loading_screen.subtitle
                text = $report.map.loading_screen.text
            }
            warnings = @($report.warnings)
        })
    }
}

$evidence = [ordered]@{
    schemaVersion = 1
    basis = 'Warcraft III local loading-screen fields parsed from official maps; text is preserved verbatim.'
    gameVersion = [ordered]@{
        label = $gameVersion.label
        build = $gameVersion.build
        gameDllSha256 = $gameVersion.sha256
        productVersion = $gameVersion.product_version
    }
    demoCampaign = [ordered]@{
        sourceContainer = 'Campaigns\DemoCampaign.w3n'
        sha256 = $demoCampaignReport.source.sha256
    }
    missions = $records
}
$evidenceJson = ($evidence | ConvertTo-Json -Depth 12) + "`n"

function Format-OriginalDescription {
    param(
        [Parameter(Mandatory)]$Campaign,
        [Parameter(Mandatory)]$Mission,
        [Parameter(Mandatory)]$Record
    )

    $warnings = if ($Record.warnings.Count -eq 0) { '无' } else { $Record.warnings -join '；' }
    $displayText = $Record.loadingScreen.text.TrimEnd()
    $lines = @(
        "# 原版关卡说明：$($Mission.titleZh)",
        '',
        "- 所属游戏：$($Campaign.game)",
        "- 所属战役：$($Campaign.faction)战役《$($Campaign.titleZh)》",
        "- 章节编号：$($Mission.number)",
        "- 英文名：$($Mission.titleEn)",
        "- 类型：$($Mission.type)",
        '',
        '## 本地加载界面原文',
        '',
        $displayText,
        '',
        '## 核对依据',
        '',
        "- 核对版本：Warcraft III $($gameVersion.label)（build $($gameVersion.build)，简体中文本地化）",
        "- 加载画面标题：$($Record.loadingScreen.title) / $($Record.loadingScreen.subtitle)",
        "- 本地来源：``$($Record.sourceContainer) / $($Record.sourceEntry)``",
        "- 地图 SHA-256：``$($Record.sourceSha256)``",
        "- 地图格式版本：$($Record.formatVersion)",
        "- 解析警告：$warnings"
    )
    return ($lines -join "`n") + "`n"
}

$recordByMission = @{}
foreach ($record in $records) {
    $recordByMission["$($record.campaignDirectory)/$($record.missionDirectory)"] = $record
}

$expectedOriginals = [ordered]@{}
foreach ($campaign in $manifest) {
    foreach ($mission in $campaign.missions) {
        $key = "$($campaign.directory)/$($mission.directory)"
        $record = $recordByMission[$key]
        $mission.summary = $record.loadingScreen.text
        $path = Join-Path $repoRoot "output\$($campaign.directory)\$($mission.directory)\ORIGINAL.md"
        $expectedOriginals[$path] = Format-OriginalDescription -Campaign $campaign -Mission $mission -Record $record
    }
}
$manifestJson = ($manifest | ConvertTo-Json -Depth 12) + "`n"

if ($Update) {
    Write-Utf8File -Path $evidencePath -Content $evidenceJson
    Write-Utf8File -Path $manifestPath -Content $manifestJson
    foreach ($item in $expectedOriginals.GetEnumerator()) {
        Write-Utf8File -Path $item.Key -Content $item.Value
    }
    Write-Host "Updated $($records.Count) original descriptions from Warcraft III $($gameVersion.label)."
    Write-Host "Evidence: $evidencePath"
    exit 0
}

if ($Check) {
    $mismatches = [System.Collections.Generic.List[string]]::new()
    if (-not (Test-Path -LiteralPath $evidencePath) -or
        (Normalize-Text (Get-Content -LiteralPath $evidencePath -Raw -Encoding utf8)) -cne (Normalize-Text $evidenceJson)) {
        $mismatches.Add($evidencePath)
    }
    if ((Normalize-Text (Get-Content -LiteralPath $manifestPath -Raw -Encoding utf8)) -cne (Normalize-Text $manifestJson)) {
        $mismatches.Add($manifestPath)
    }
    foreach ($item in $expectedOriginals.GetEnumerator()) {
        if (-not (Test-Path -LiteralPath $item.Key) -or
            (Normalize-Text (Get-Content -LiteralPath $item.Key -Raw -Encoding utf8)) -cne (Normalize-Text $item.Value)) {
            $mismatches.Add($item.Key)
        }
    }
    if ($mismatches.Count -gt 0) {
        throw "Original descriptions are out of sync:`n$($mismatches -join "`n")"
    }
    Write-Host "PASS: $($records.Count) original descriptions match Warcraft III $($gameVersion.label) loading-screen text."
    exit 0
}

$records |
    Select-Object campaignDirectory, missionDirectory, sourceContainer, sourceEntry,
        @{ Name = 'title'; Expression = { $_.loadingScreen.title } },
        @{ Name = 'subtitle'; Expression = { $_.loadingScreen.subtitle } },
        @{ Name = 'text'; Expression = { $_.loadingScreen.text } } |
    ConvertTo-Json -Depth 5
