# Report schema

All reports use `schema_version: 1`, a `kind` discriminator, a `source` object, and a `warnings` array. Field names use `snake_case`.

The maintained JSON Schema is `tools/wc3-inspect/schemas/report.schema.json`.

Important provenance fields:

- `source.path`: standalone input path, when applicable.
- `source.game_root`: inspected installation.
- `source.archive` and `source.entry`: exact container and inner path.
- `source.size` and `source.sha256`: bytes actually parsed.
- `source.game_version`: `Game.dll` evidence and known label.

Consumers should tolerate additional fields within a schema version and must check `kind` before reading kind-specific objects such as `map`, `campaign`, `archive`, or `game_version`.

