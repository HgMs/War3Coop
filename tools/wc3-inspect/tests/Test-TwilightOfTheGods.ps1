[CmdletBinding()]
param(
    [string]$GamePath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$toolRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$repoRoot = (Resolve-Path (Join-Path $toolRoot '..\..')).Path
if (-not $GamePath) {
    $GamePath = Join-Path $repoRoot 'game\Warcraft3-1.24e'
}

$reportPath = Join-Path $repoRoot 'build\wc3-inspect\tests\twilight-of-the-gods.json'
& (Join-Path $toolRoot 'wc3-inspect.ps1') game inspect $GamePath `
    --entry 'Maps\Campaign\NightElf07.w3m' `
    --output $reportPath | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw 'wc3-inspect returned a failure code.'
}

$report = Get-Content -LiteralPath $reportPath -Raw -Encoding utf8 | ConvertFrom-Json
if ($report.kind -ne 'game-map') { throw "Unexpected report kind: $($report.kind)" }
if ($report.source.game_version.label -ne '1.24e') { throw "Unexpected game version: $($report.source.game_version.label)" }
if ($report.map.format_version -ne 18) { throw "Unexpected map format: $($report.map.format_version)" }
if ($report.map.name -ne '暗夜精灵07') { throw "Unexpected map name: $($report.map.name)" }
if ($report.map.description -notmatch '诸神之黄昏|诸神黄昏') {
    throw "Unexpected map description: $($report.map.description)"
}
if ($report.map.loading_screen.text -notmatch '海加尔山' -or
    $report.map.loading_screen.text -match '^TRIGSTR_') {
    throw "Expected localized loading-screen text, received: $($report.map.loading_screen.text)"
}
if ($report.source.sha256 -ne '97143C5E8F417DB033B566ED54A2359D6A2BA609F343A3DE79ABADF50A3D1C92') {
    throw "Unexpected NightElf07.w3m hash: $($report.source.sha256)"
}

Write-Host "PASS: $($report.map.description), Warcraft III $($report.source.game_version.label)"
Write-Host "Report: $reportPath"
