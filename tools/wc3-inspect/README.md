# wc3-inspect

Read-only Warcraft III game archive, map, and campaign inspector. The CLI uses War3Net and emits UTF-8 JSON with source provenance and SHA-256 hashes.

## Setup

```powershell
pwsh -File tools/wc3-inspect/bootstrap.ps1
pwsh -File tools/wc3-inspect/build.ps1
```

The SDK, NuGet cache, binaries, reports, and extracted game data stay under ignored `build/` paths.

## Examples

```powershell
pwsh -File tools/wc3-inspect/wc3-inspect.ps1 detect game/Warcraft3-1.20
pwsh -File tools/wc3-inspect/wc3-inspect.ps1 game version game/Warcraft3-1.20
pwsh -File tools/wc3-inspect/wc3-inspect.ps1 game inspect game/Warcraft3-1.20 --entry 'Maps\Campaign\NightElf07.w3m'
pwsh -File tools/wc3-inspect/tests/Test-TwilightOfTheGods.ps1
```

The parser never executes embedded scripts. MPQ extraction is explicit, refuses overwrite by default, and enforces a 512 MiB expanded-size limit.

