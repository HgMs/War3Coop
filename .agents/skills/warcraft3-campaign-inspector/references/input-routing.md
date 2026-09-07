# Input routing

Run all commands through `scripts/inspect.ps1` from the skill directory.

| Input or question | Command |
| --- | --- |
| Unknown path | `detect <path>` |
| Classic installation version | `game version <game-directory>` |
| Map inside a game MPQ | `game inspect <game-directory> --entry <archive-path>` |
| MPQ contents | `archive inspect <archive>` |
| One MPQ entry | `archive extract <archive> <entry> <output-file>` |
| Standalone map | `map inspect <map.w3m-or-w3x>` |
| Custom campaign | `campaign inspect <campaign.w3n>` |

Add `--output <report.json>` to report-producing commands when another process needs the result. Console output is always UTF-8 JSON.

Extraction refuses to overwrite by default. Use `--overwrite` only when the user has authorized replacing the exact destination. The default expanded-size limit is 512 MiB; raise `--max-bytes` only for a known, trusted entry.

