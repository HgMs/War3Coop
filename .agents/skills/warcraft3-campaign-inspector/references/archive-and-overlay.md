# Archives and overlay order

Classic Warcraft III installations store campaign maps and shared data in MPQ archives. Search known entries in this order and preserve the archive that supplied the result:

1. `War3Patch.mpq`
2. `War3xlocal.mpq`
3. `War3x.mpq`
4. `War3.mpq`

This is a lookup order for the current read-only inspector. It does not yet materialize Blizzard incremental patch-file deltas across archives.

W3M, W3X, and W3N files are MPQ containers with Warcraft-specific inner files. A W3N normally contains `war3campaign.w3f`, campaign object data, imports, trigger strings, and embedded map paths. A map normally contains `war3map.w3i`, scripts, triggers, object data, terrain, widgets, and imports.

MPQ filenames are hash-addressed. `(listfile)` is optional, so `archive inspect` can report unknown entries. Known conventional filenames and explicitly supplied names may still be opened when enumeration is incomplete.

