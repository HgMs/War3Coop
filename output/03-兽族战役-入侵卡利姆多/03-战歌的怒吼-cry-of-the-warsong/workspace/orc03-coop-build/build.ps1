[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$toolRoot = $PSScriptRoot
$repoRoot = (Resolve-Path (Join-Path $toolRoot '..\..\..\..\..')).Path
$dotnet = Join-Path $repoRoot 'build\dotnet\dotnet.exe'
$project = Join-Path $toolRoot 'src\Orc03CoopBuild.Cli\Orc03CoopBuild.Cli.csproj'
$publish = Join-Path $repoRoot 'build\orc03-coop-build\publish'

if (-not (Test-Path -LiteralPath $dotnet -PathType Leaf)) {
    throw "Bundled dotnet was not found: $dotnet"
}

& $dotnet publish $project --configuration Release --output $publish -p:RestoreLockedMode=true
exit $LASTEXITCODE
