[CmdletBinding()]
param(
    [switch]$Bootstrap
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

if (-not $dotnet -and $Bootstrap) {
    & (Join-Path $toolRoot 'bootstrap.ps1')
    $dotnet = $localDotnet
}
if (-not $dotnet) {
    throw 'A .NET 8 SDK is required. Run tools\wc3-inspect\bootstrap.ps1 first.'
}

$project = Join-Path $toolRoot 'src\Wc3Inspect.Cli\Wc3Inspect.Cli.csproj'
$publishRoot = Join-Path $repoRoot 'build\wc3-inspect\publish'
$env:NUGET_PACKAGES = Join-Path $repoRoot 'build\nuget-packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'

& $dotnet restore $project --locked-mode --nologo
if ($LASTEXITCODE -ne 0) {
    throw "wc3-inspect restore failed with exit code $LASTEXITCODE."
}

& $dotnet publish $project --configuration Release --output $publishRoot --no-restore --nologo
if ($LASTEXITCODE -ne 0) {
    throw "wc3-inspect build failed with exit code $LASTEXITCODE."
}

Write-Host "Published wc3-inspect to $publishRoot"
