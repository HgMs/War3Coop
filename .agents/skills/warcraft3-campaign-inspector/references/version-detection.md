# Version detection

Use `Game.dll` numeric file-version parts as the primary classic-build key. Include the DLL SHA-256 as provenance and use the product/file version strings only as supporting evidence.

Known local baseline:

| Label | Build | Game.dll SHA-256 |
| --- | --- | --- |
| 1.20e | `1.20.4.6074` | `6B5B406861A56449C604C15CF0971DF7D6D26F679EFF7AA69A34946C7D91764B` |

The machine-readable catalog is `tools/wc3-inspect/version-catalog.json`. Add a mapping only after comparing it with a trusted patch source or independently verified installation. An unknown build must remain `unknown`; do not guess from directory names.

