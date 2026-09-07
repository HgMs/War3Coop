using System.Text.RegularExpressions;
using War3Net.Build;
using War3Net.Build.Extensions;

namespace Wc3Inspect;

internal static partial class MapInspector
{
    public static object InspectFile(string mapPath)
    {
        var path = Path.GetFullPath(mapPath);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Map was not found.", path);
        }

        var file = new FileInfo(path);
        var warnings = new List<string>();
        var map = LoadMap(files => Map.Open(path, files), warnings);
        return CreateReport(map, new
        {
            path,
            size = file.Length,
            sha256 = Hashing.Sha256(path),
        }, "map", warnings);
    }

    public static object InspectBytes(byte[] data, object source, string kind = "game-map")
    {
        var warnings = new List<string>();
        var map = LoadMap(files =>
        {
            var stream = new MemoryStream(data, writable: false);
            return Map.Open(stream, files);
        }, warnings);
        return CreateReport(map, source, kind, warnings);
    }

    private static Map LoadMap(Func<MapFiles, Map> open, List<string> warnings)
    {
        var map = open(MapFiles.Info | MapFiles.TriggerStrings | MapFiles.Script);
        TryLoad("war3map.imp", MapFiles.ImportedFiles, part => map.ImportedFiles = part.ImportedFiles);
        TryLoad("war3mapUnits.doo", MapFiles.Units, part => map.Units = part.Units);
        TryLoad("war3map.doo", MapFiles.Doodads, part => map.Doodads = part.Doodads);
        return map;

        void TryLoad(string component, MapFiles files, Action<Map> assign)
        {
            try
            {
                assign(open(files));
            }
            catch (Exception exception)
            {
                warnings.Add($"Could not parse optional {component}: {exception.Message}");
            }
        }
    }

    private static object CreateReport(Map map, object source, string kind, IReadOnlyList<string> warnings)
    {
        var info = map.Info ?? throw new InvalidDataException("war3map.w3i is missing or unreadable.");
        var rawStrings = new
        {
            name = info.MapName,
            author = info.MapAuthor,
            description = info.MapDescription,
            recommendedPlayers = info.RecommendedPlayers,
        };
        map.LocalizeInfo();

        var script = map.Script ?? string.Empty;
        var functionCount = FunctionRegex().Matches(script).Count;
        var globalCount = GlobalRegex().Matches(script)
            .Select(match => match.Value)
            .Distinct(StringComparer.Ordinal)
            .Count();

        return new
        {
            schemaVersion = 1,
            kind,
            source,
            map = new
            {
                formatVersion = (int)info.FormatVersion,
                mapVersion = info.MapVersion,
                editorVersion = info.EditorVersion.ToString(),
                declaredGameVersion = info.GameVersion?.ToString(),
                name = info.MapName,
                author = info.MapAuthor,
                description = info.MapDescription,
                recommendedPlayers = info.RecommendedPlayers,
                rawStrings,
                width = info.PlayableMapAreaWidth,
                height = info.PlayableMapAreaHeight,
                flags = info.MapFlags.ToString(),
                flagsValue = (int)info.MapFlags,
                tileset = info.Tileset.ToString(),
                scriptLanguage = info.ScriptLanguage.ToString(),
                players = info.Players.Select(player => new
                {
                    id = player.Id,
                    name = player.Name,
                    controller = player.Controller.ToString(),
                    race = player.Race.ToString(),
                    flags = player.Flags.ToString(),
                }).ToArray(),
                forces = info.Forces.Select(force => new { name = force.Name }).ToArray(),
            },
            content = new
            {
                triggerStringCount = map.TriggerStrings?.Strings.Count ?? 0,
                scriptBytes = System.Text.Encoding.UTF8.GetByteCount(script),
                scriptFunctionCount = functionCount,
                scriptUdgGlobalCount = globalCount,
                unitCount = map.Units?.Units.Count,
                doodadCount = map.Doodads?.Doodads.Count,
                specialDoodadCount = map.Doodads?.SpecialDoodads.Count,
                importedFileCount = map.ImportedFiles?.Files.Count,
            },
            warnings,
        };
    }

    [GeneratedRegex(@"(?m)^\s*function\s+[A-Za-z_][A-Za-z0-9_]*\s+takes\b")]
    private static partial Regex FunctionRegex();

    [GeneratedRegex(@"\budg_[A-Za-z0-9_]+\b")]
    private static partial Regex GlobalRegex();
}
