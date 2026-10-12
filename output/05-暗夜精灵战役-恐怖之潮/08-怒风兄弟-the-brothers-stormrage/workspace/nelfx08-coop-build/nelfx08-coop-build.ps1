[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$SourceMap,

    [Parameter(Mandatory = $true)]
    [string]$OutputMap
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..\..')).Path
$dotnet = Join-Path $repoRoot 'build\dotnet\dotnet.exe'
$assembly = Join-Path $repoRoot 'build\nelfx08-coop-build\publish\NelfX08CoopBuild.dll'
$sources = Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Recurse -File |
    Where-Object { $_.FullName -notmatch '[\/](bin|obj)[\/]' }
$needsBuild = -not (Test-Path -LiteralPath $assembly -PathType Leaf)
if (-not $needsBuild) {
    $latestSource = $sources | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
    $needsBuild = $latestSource.LastWriteTimeUtc -gt (Get-Item -LiteralPath $assembly).LastWriteTimeUtc
}

if ($needsBuild) {
    & (Join-Path $PSScriptRoot 'build.ps1')
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

& $dotnet $assembly $SourceMap $OutputMap
exit $LASTEXITCODE
