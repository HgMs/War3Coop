[CmdletBinding()]
param(
    [string]$GamePath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$toolRoot = $PSScriptRoot
$repoRoot = (Resolve-Path (Join-Path $toolRoot "..\..")).Path
if (-not $GamePath) {
    $GamePath = Join-Path $repoRoot "game\Warcraft3-1.20"
}
$gameRoot = (Resolve-Path $GamePath).Path

$sfmpqCandidates = @(
    (Join-Path $gameRoot "change\SFmpq.dll"),
    (Join-Path $gameRoot "SFmpq.dll")
)
$sfmpq = $sfmpqCandidates |
    Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
    Select-Object -First 1
if (-not $sfmpq) {
    throw "SFmpq.dll was not found under $gameRoot."
}

$csc = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe"
if (-not (Test-Path -LiteralPath $csc -PathType Leaf)) {
    throw ".NET Framework x86 C# compiler was not found: $csc"
}
if (-not (Get-Command python -ErrorAction SilentlyContinue)) {
    throw "Python is required to parse war3map.w3i and war3map.wts."
}

$workRoot = Join-Path $repoRoot "build\war3-campaign-demo"
$mapRoot = Join-Path $workRoot "map"
[System.IO.Directory]::CreateDirectory($mapRoot) | Out-Null

$extractorSource = Join-Path $toolRoot "SfmpqExtract.cs"
$extractor = Join-Path $workRoot "SfmpqExtract.exe"
if (
    -not (Test-Path -LiteralPath $extractor) -or
    (Get-Item -LiteralPath $extractorSource).LastWriteTimeUtc -gt
        (Get-Item -LiteralPath $extractor).LastWriteTimeUtc
) {
    $compilerArgs = @(
        "/nologo",
        "/target:exe",
        "/platform:x86",
        "/out:$extractor",
        $extractorSource
    )
    $compilerOutput = & $csc $compilerArgs 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Could not compile the SFMPQ helper: $compilerOutput"
    }
}

$sourceEntry = "Maps\Campaign\NightElf07.w3m"
$mapPath = Join-Path $workRoot "NightElf07.w3m"
$archiveNames = @(
    "War3Patch.mpq",
    "War3xlocal.mpq",
    "War3x.mpq",
    "WAR3.MPQ"
)

$sourceArchive = $null
foreach ($archiveName in $archiveNames) {
    $candidate = Join-Path $gameRoot $archiveName
    if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
        continue
    }

    $extractOutput = & $extractor $sfmpq $candidate $sourceEntry $mapPath 2>&1
    $extractExitCode = $LASTEXITCODE
    if ($extractExitCode -eq 0) {
        $sourceArchive = $candidate
        break
    }
    if ($extractExitCode -ne 5) {
        throw "Could not read $candidate : $extractOutput"
    }
}
if (-not $sourceArchive) {
    throw "$sourceEntry was not found in the classic Warcraft III archives."
}

$entries = [ordered]@{
    "war3map.w3i" = "war3map.w3i"
    "war3map.wts" = "war3map.wts"
    "war3map.j" = "war3map.j"
    "war3mapUnits.doo" = "war3mapUnits.doo"
    "war3map.doo" = "war3map.doo"
    "(listfile)" = "listfile.txt"
}
foreach ($entry in $entries.GetEnumerator()) {
    $outputPath = Join-Path $mapRoot $entry.Value
    $extractOutput = & $extractor $sfmpq $mapPath $entry.Key $outputPath 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Could not extract $($entry.Key) from NightElf07.w3m: $extractOutput"
    }
}

$war3Exe = Join-Path $gameRoot "War3.exe"
$gameVersion = if (Test-Path -LiteralPath $war3Exe -PathType Leaf) {
    (Get-Item -LiteralPath $war3Exe).VersionInfo.ProductVersion
} else {
    "unknown"
}

$reportPath = Join-Path $workRoot "report.json"
$pythonArgs = @(
    (Join-Path $toolRoot "inspect_map.py"),
    "--map", $mapPath,
    "--w3i", (Join-Path $mapRoot "war3map.w3i"),
    "--wts", (Join-Path $mapRoot "war3map.wts"),
    "--jass", (Join-Path $mapRoot "war3map.j"),
    "--units", (Join-Path $mapRoot "war3mapUnits.doo"),
    "--doodads", (Join-Path $mapRoot "war3map.doo"),
    "--source-archive", $sourceArchive,
    "--source-entry", $sourceEntry,
    "--game-version", $gameVersion,
    "--output", $reportPath
)
$env:PYTHONUTF8 = "1"
& python $pythonArgs
if ($LASTEXITCODE -ne 0) {
    throw "The map was read, but one or more identity checks failed."
}

Write-Host ""
Write-Host "Demo report: $reportPath"
