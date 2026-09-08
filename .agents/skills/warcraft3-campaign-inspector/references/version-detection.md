# Version detection

Use `Game.dll` numeric file-version parts as the primary classic-build key. Include the DLL SHA-256 as provenance and use the product/file version strings only as supporting evidence.

Known verified classic builds:

| Label | Build | Game.dll SHA-256 |
| --- | --- | --- |
| 1.24e | `1.24.4.6387` | `5F7D8B25005BC12F8453839474EB37ADE8869ABA0570EAD2B76B6A7979C904CC` |

The machine-readable catalog is `tools/wc3-inspect/version-catalog.json`. Add a mapping only after comparing it with a trusted patch source or independently verified installation. An unknown build must remain `unknown`; do not guess from directory names.

