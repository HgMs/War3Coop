# Warcraft III campaign read demo

This read-only demo validates that the local classic Warcraft III installation
can provide an original campaign map and that the map's nested MPQ files can be
read.

The sample is the last Reign of Chaos Night Elf mission:
Maps\Campaign\NightElf07.w3m (Twilight of the Gods).

## What it does

1. Builds a small x86 helper with the Windows .NET Framework compiler.
2. Uses the installation's SFmpq.dll to search the classic archive priority
   order, starting with War3Patch.mpq.
3. Extracts the map and selected inner files into the ignored
   build/war3-campaign-demo/ directory.
4. Parses core war3map.w3i fields, resolves TRIGSTR values from war3map.wts,
   and checks the JASS script for the 45-minute Archimonde objective.
5. Writes a deterministic JSON report with file sizes and SHA-256 hashes.

The source archives are never opened for writing.

## Run

~~~powershell
pwsh -File tools/war3-campaign-demo/Invoke-Demo.ps1
~~~

To use another classic installation:

~~~powershell
pwsh -File tools/war3-campaign-demo/Invoke-Demo.ps1 -GamePath D:\Warcraft-III
~~~

Requirements:

- Windows with the .NET Framework 4.x x86 compiler;
- Python 3;
- a 32-bit SFmpq.dll under the game root or its change/ directory.

This is deliberately a compatibility probe, not the final archive subsystem.
It does not yet read CASC installations, write MPQs, or fully deserialize every
map component.
