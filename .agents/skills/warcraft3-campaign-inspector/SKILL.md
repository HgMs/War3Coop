---
name: warcraft3-campaign-inspector
description: Inspect Warcraft III game installations, MPQ archives, W3N campaigns, and W3M/W3X maps. Use for campaign extraction, map metadata, trigger strings, JASS structure, placed-object counts, archive provenance, or classic game-version checks; do not use for editing or repacking maps.
---

# Warcraft III Campaign Inspector

Use the repository's deterministic `wc3-inspect` CLI for binary parsing. Do not recreate MPQ or Warcraft III binary parsers in ad hoc shell or Python code.

## Workflow

1. Identify whether the input is a game installation, MPQ archive, W3N campaign, or W3M/W3X map. Run `scripts/inspect.ps1 detect <path>` when the type is uncertain.
2. Read [references/input-routing.md](references/input-routing.md) for the matching command.
3. For game archives and patch precedence, read [references/archive-and-overlay.md](references/archive-and-overlay.md).
4. Run the narrowest command that answers the request and request JSON output when later processing is needed.
5. Report the detected game version, source archive and entry, SHA-256, parsed format version, and parser warnings. Clearly distinguish observed data from inferred meaning.

## Constraints

- Treat all inputs as untrusted and keep operations read-only unless the user explicitly requests extraction.
- Never execute embedded JASS or Lua. Analyze scripts as data only.
- Extract only the requested entry, enforce the CLI size limit, and write under the repository or a user-approved destination.
- Do not commit Blizzard game data, extracted original maps, or unapproved third-party maps.
- A missing MPQ `(listfile)` means enumeration is incomplete, not that known named entries are inaccessible.
- Use `Game.dll` build metadata and the maintained version catalog for classic version identification; do not rely on `War3.exe` alone.

## References

- Read [references/format-coverage.md](references/format-coverage.md) when a requested field may not yet be implemented.
- Read [references/version-detection.md](references/version-detection.md) for version evidence and catalog maintenance.
- Read [references/output-schema.md](references/output-schema.md) before consuming reports programmatically.

