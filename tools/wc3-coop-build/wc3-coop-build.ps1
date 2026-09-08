[CmdletBinding()]
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Arguments
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$dotnet = Join-Path $repoRoot 'build\dotnet\dotnet.exe'
$assembly = Join-Path $repoRoot 'build\wc3-coop-build\publish\wc3-coop-build.dll'
$sources = Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Recurse -File |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }
$needsBuild = -not (Test-Path -LiteralPath $assembly -PathType Leaf)
if (-not $needsBuild) {
    $latestSource = $sources | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
    $needsBuild = $latestSource.LastWriteTimeUtc -gt (Get-Item -LiteralPath $assembly).LastWriteTimeUtc
}

if ($needsBuild) {
    & (Join-Path $PSScriptRoot 'build.ps1')
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

& $dotnet $assembly @Arguments
exit $LASTEXITCODE
