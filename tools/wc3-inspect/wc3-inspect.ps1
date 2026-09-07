[CmdletBinding()]
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Arguments
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$toolRoot = $PSScriptRoot
$repoRoot = (Resolve-Path (Join-Path $toolRoot '..\..')).Path
$localDotnet = Join-Path $repoRoot 'build\dotnet\dotnet.exe'
$dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
$dotnet = if (Test-Path -LiteralPath $localDotnet -PathType Leaf) {
    $localDotnet
} elseif ($dotnetCommand) {
    $dotnetCommand.Source
} else {
    $null
}
$assembly = Join-Path $repoRoot 'build\wc3-inspect\publish\wc3-inspect.dll'

if (-not $dotnet) {
    throw 'A .NET 8 runtime is required. Run tools\wc3-inspect\bootstrap.ps1 first.'
}
$sourceInputs = @(
    Get-ChildItem -LiteralPath (Join-Path $toolRoot 'src') -Recurse -File |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }
    Get-Item -LiteralPath (Join-Path $toolRoot 'version-catalog.json')
)
$needsBuild = -not (Test-Path -LiteralPath $assembly -PathType Leaf)
if (-not $needsBuild) {
    $latestInput = $sourceInputs | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
    $needsBuild = $latestInput.LastWriteTimeUtc -gt (Get-Item -LiteralPath $assembly).LastWriteTimeUtc
}
if ($needsBuild) {
    & (Join-Path $toolRoot 'build.ps1')
}

& $dotnet $assembly @Arguments
exit $LASTEXITCODE
