[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$dotnet = Join-Path $repoRoot 'build\dotnet\dotnet.exe'
$project = Join-Path $PSScriptRoot 'src\Wc3CoopBuild.Cli\Wc3CoopBuild.Cli.csproj'
$publish = Join-Path $repoRoot 'build\wc3-coop-build\publish'
$env:NUGET_PACKAGES = Join-Path $repoRoot 'build\nuget-packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'

if (-not (Test-Path -LiteralPath $dotnet -PathType Leaf)) {
    throw 'The repository-local .NET runtime is missing. Bootstrap wc3-inspect first.'
}

& $dotnet restore $project --locked-mode --ignore-failed-sources --nologo
if ($LASTEXITCODE -ne 0) {
    throw "wc3-coop-build restore failed with exit code $LASTEXITCODE."
}

& $dotnet publish $project -c Release -o $publish --no-restore --nologo
exit $LASTEXITCODE
