[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$toolRoot = $PSScriptRoot
$repoRoot = (Resolve-Path (Join-Path $toolRoot '..\..')).Path
$installRoot = Join-Path $repoRoot 'build\dotnet'
$dotnet = Join-Path $installRoot 'dotnet.exe'
$sdkVersion = '8.0.424'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'

if (Test-Path -LiteralPath $dotnet -PathType Leaf) {
    $installed = & $dotnet --version
    if ($installed -eq $sdkVersion) {
        Write-Host ".NET SDK $sdkVersion is already available at $dotnet"
        exit 0
    }
}

New-Item -ItemType Directory -Force -Path $installRoot | Out-Null
$temporaryFile = New-TemporaryFile
$installerPath = [System.IO.Path]::ChangeExtension($temporaryFile.FullName, '.ps1')
Move-Item -LiteralPath $temporaryFile.FullName -Destination $installerPath
try {
    Invoke-WebRequest -UseBasicParsing -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile $installerPath
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $installerPath `
        -Version $sdkVersion `
        -InstallDir $installRoot `
        -Architecture x64 `
        -NoPath
    if ($LASTEXITCODE -ne 0) {
        throw ".NET SDK installation failed with exit code $LASTEXITCODE."
    }
}
finally {
    Remove-Item -LiteralPath $installerPath -Force -ErrorAction SilentlyContinue
}

$actual = & $dotnet --version
if ($actual -ne $sdkVersion) {
    throw "Expected .NET SDK $sdkVersion, found $actual."
}

Write-Host "Installed .NET SDK $actual at $installRoot"
