namespace Wc3Inspect;

internal static class GameInspector
{
    public static object InspectVersion(string gameRoot)
    {
        var root = Path.GetFullPath(gameRoot);
        return new
        {
            schemaVersion = 1,
            kind = "game-version",
            source = new { gameRoot = root },
            gameVersion = GameVersionDetector.Detect(root),
            warnings = Array.Empty<string>(),
        };
    }

    public static object InspectMap(string gameRoot, string entryName, long maxBytes)
    {
        var root = Path.GetFullPath(gameRoot);
        var version = GameVersionDetector.Detect(root);
        var entry = ArchiveService.FindInClassicGame(root, entryName, maxBytes);
        var source = new
        {
            gameRoot = root,
            gameVersion = version,
            archive = entry.ArchivePath,
            entry = entry.Entry,
            size = entry.Data.LongLength,
            sha256 = Hashing.Sha256(entry.Data),
        };

        return MapInspector.InspectBytes(entry.Data, source);
    }
}

