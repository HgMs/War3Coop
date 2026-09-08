# wc3-inspect

Read-only Warcraft III game archive, map, and campaign inspector. The CLI uses War3Net and emits UTF-8 JSON with source provenance and SHA-256 hashes. Map reports include localized loading-screen title, subtitle, and text while retaining the original `TRIGSTR_*` keys.

## Setup

```powershell
pwsh -File tools/wc3-inspect/bootstrap.ps1
pwsh -File tools/wc3-inspect/build.ps1
```

The SDK, NuGet cache, binaries, reports, and extracted game data stay under ignored `build/` paths.

## Examples

```powershell
pwsh -File tools/wc3-inspect/wc3-inspect.ps1 detect game/Warcraft3-1.24e
pwsh -File tools/wc3-inspect/wc3-inspect.ps1 game version game/Warcraft3-1.24e
pwsh -File tools/wc3-inspect/wc3-inspect.ps1 game inspect game/Warcraft3-1.24e --entry 'Maps\Campaign\NightElf07.w3m'
pwsh -File tools/wc3-inspect/wc3-inspect.ps1 gamecache inspect game/Warcraft3-1.24e/save/Profile1/Campaigns.w3v
pwsh -File tools/wc3-inspect/tests/Test-TwilightOfTheGods.ps1
```

The parser never executes embedded scripts. Game-cache inspection is read-only and reports stored hero inventory, levels, attributes, and skills. MPQ extraction is explicit, refuses overwrite by default, and enforces a 512 MiB expanded-size limit.
