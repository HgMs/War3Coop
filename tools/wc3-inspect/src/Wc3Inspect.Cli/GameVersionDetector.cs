using System.Diagnostics;
using System.Text.Json;

namespace Wc3Inspect;

internal sealed record GameVersionEvidence(
    string Label,
    string Build,
    string GameDll,
    long Size,
    string Sha256,
    string? FileVersion,
    string? ProductVersion);

internal static class GameVersionDetector
{
    public static GameVersionEvidence Detect(string gameRoot)
    {
        var root = Path.GetFullPath(gameRoot);
        var gameDll = Path.Combine(root, "Game.dll");
        if (!File.Exists(gameDll))
        {
            throw new FileNotFoundException("Game.dll was not found in the game directory.", gameDll);
        }

        var info = FileVersionInfo.GetVersionInfo(gameDll);
        var build = string.Join('.', info.FileMajorPart, info.FileMinorPart, info.FileBuildPart, info.FilePrivatePart);
        var sha256 = Hashing.Sha256(gameDll);
        var catalog = LoadCatalog();
        var match = catalog.Versions.FirstOrDefault(candidate =>
            string.Equals(candidate.Build, build, StringComparison.OrdinalIgnoreCase) &&
            (string.IsNullOrEmpty(candidate.GameDllSha256) ||
             string.Equals(candidate.GameDllSha256, sha256, StringComparison.OrdinalIgnoreCase)));

        return new GameVersionEvidence(
            match?.Label ?? "unknown",
            build,
            gameDll,
            new FileInfo(gameDll).Length,
            sha256,
            info.FileVersion,
            info.ProductVersion);
    }

    private static VersionCatalog LoadCatalog()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "version-catalog.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The game version catalog is missing.", path);
        }

        return JsonSerializer.Deserialize<VersionCatalog>(File.ReadAllText(path), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        }) ?? throw new InvalidDataException("The game version catalog is invalid.");
    }

    private sealed record VersionCatalog(int SchemaVersion, List<VersionEntry> Versions);
    private sealed record VersionEntry(string Label, string Build, string GameDllSha256);
}

