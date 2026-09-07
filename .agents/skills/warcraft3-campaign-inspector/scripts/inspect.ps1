[CmdletBinding()]
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Arguments
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path
$tool = Join-Path $repoRoot 'tools\wc3-inspect\wc3-inspect.ps1'

if (-not (Test-Path -LiteralPath $tool -PathType Leaf)) {
    throw "wc3-inspect launcher was not found: $tool"
}

& $tool @Arguments
exit $LASTEXITCODE

