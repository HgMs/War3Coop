using War3Net.IO.Mpq;

namespace Wc3Inspect;

internal static class InputInspector
{
    public static object Detect(string inputPath)
    {
        var path = Path.GetFullPath(inputPath);
        if (Directory.Exists(path))
        {
            var gameDll = Path.Combine(path, "Game.dll");
            return new
            {
                schemaVersion = 1,
                kind = "input-detection",
                source = new { path },
                detectedType = File.Exists(gameDll) ? "classic-game-installation" : "directory",
                gameVersion = File.Exists(gameDll) ? GameVersionDetector.Detect(path) : null,
                warnings = Array.Empty<string>(),
            };
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Input path does not exist.", path);
        }

        var extension = Path.GetExtension(path).ToLowerInvariant();
        var detectedType = extension switch
        {
            ".w3n" => "campaign",
            ".w3m" or ".w3x" => "map",
            ".mpq" => "mpq-archive",
            _ => CanOpenAsMpq(path) ? "mpq-archive" : "file",
        };

        var file = new FileInfo(path);
        return new
        {
            schemaVersion = 1,
            kind = "input-detection",
            source = new { path, size = file.Length, sha256 = Hashing.Sha256(path) },
            detectedType,
            warnings = Array.Empty<string>(),
        };
    }

    private static bool CanOpenAsMpq(string path)
    {
        try
        {
            using var archive = MpqArchive.Open(path);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

