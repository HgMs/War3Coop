# Format coverage

## Implemented

- Input classification for game directories, MPQ, W3N, W3M, and W3X.
- Classic `Game.dll` version/build/hash reporting.
- MPQ listfile-based inventory and bounded named-entry extraction.
- Direct inspection of maps stored inside classic game MPQs.
- W3I metadata, player and force summaries, WTS localization, map script statistics, imports, units, and doodads.
- W3F campaign metadata, map buttons/order, trigger-string localization, imports, and campaign object-data presence.

## Planned

- Structured WTG/WCT trigger queries and JASS AST queries.
- Detailed object-data modification reports and base/campaign/map overlay resolution.
- Terrain, region, camera, sound, and asset dependency reports.
- CASC installation discovery for 1.31 data.
- A separate, explicitly invoked mutation workflow for editing and repacking.

When a field is not implemented, inspect a known inner file only when an existing structured War3Net parser supports it. Do not infer binary layouts from a single sample.

