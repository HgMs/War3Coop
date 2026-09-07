using War3Net.IO.Mpq;

namespace Wc3Inspect;

internal sealed record GameArchiveEntry(string ArchivePath, string Entry, byte[] Data);

internal static class ArchiveService
{
    public const long DefaultMaxBytes = 512L * 1024L * 1024L;

    private static readonly string[] ClassicArchiveOrder =
    [
        "War3Patch.mpq",
        "War3xlocal.mpq",
        "War3x.mpq",
        "War3.mpq",
    ];

    public static object Inspect(string archivePath)
    {
        var path = RequireFile(archivePath);
        using var archive = MpqArchive.Open(path, loadListFile: true);
        var knownEntries = archive
            .Where(entry => !string.IsNullOrEmpty(entry.FileName))
            .Select(entry => new
            {
                name = entry.FileName,
                size = entry.FileSize,
                compressedSize = entry.CompressedSize,
                encrypted = entry.IsEncrypted,
                compressed = entry.IsCompressed,
            })
            .OrderBy(entry => entry.name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var entryCount = archive.Count();

        return new
        {
            schemaVersion = 1,
            kind = "archive",
            source = FileSource(path),
            archive = new
            {
                entryCount,
                knownEntryCount = knownEntries.Length,
                unknownEntryCount = entryCount - knownEntries.Length,
                entries = knownEntries,
            },
            warnings = entryCount == knownEntries.Length
                ? Array.Empty<string>()
                : ["The MPQ listfile is incomplete; unnamed entries are not enumerable by path."],
        };
    }

    public static object Extract(string archivePath, string entryName, string outputPath, long maxBytes, bool overwrite)
    {
        var path = RequireFile(archivePath);
        var destination = Path.GetFullPath(outputPath);
        if (File.Exists(destination) && !overwrite)
        {
            throw new IOException($"Output already exists: {destination}. Pass --overwrite to replace it.");
        }

        using var archive = MpqArchive.Open(path, loadListFile: false);
        if (!archive.TryOpenFile(entryName, out var entryStream))
        {
            throw new FileNotFoundException($"MPQ entry was not found: {entryName}", path);
        }

        using (entryStream)
        {
            var data = ReadBounded(entryStream, maxBytes);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            var mode = overwrite ? FileMode.Create : FileMode.CreateNew;
            using var output = new FileStream(destination, mode, FileAccess.Write, FileShare.None);
            output.Write(data);

            return new
            {
                schemaVersion = 1,
                kind = "archive-extraction",
                source = new { archive = path, entry = entryName, size = data.LongLength, sha256 = Hashing.Sha256(data) },
                output = new { path = destination },
                warnings = Array.Empty<string>(),
            };
        }
    }

    public static GameArchiveEntry FindInClassicGame(string gameRoot, string entryName, long maxBytes)
    {
        var root = Path.GetFullPath(gameRoot);
        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException(root);
        }

        foreach (var archiveName in ClassicArchiveOrder)
        {
            var archivePath = Path.Combine(root, archiveName);
            if (!File.Exists(archivePath))
            {
                continue;
            }

            using var archive = MpqArchive.Open(archivePath, loadListFile: false);
            if (!archive.TryOpenFile(entryName, out var entryStream))
            {
                continue;
            }

            using (entryStream)
            {
                return new GameArchiveEntry(archivePath, entryName, ReadBounded(entryStream, maxBytes));
            }
        }

        throw new FileNotFoundException($"Entry '{entryName}' was not found in the classic game archives under {root}.");
    }

    public static byte[] ReadBounded(Stream stream, long maxBytes)
    {
        using var output = new MemoryStream();
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            total += read;
            if (total > maxBytes)
            {
                throw new InvalidDataException($"Expanded entry exceeds the {maxBytes}-byte safety limit.");
            }

            output.Write(buffer, 0, read);
        }

        return output.ToArray();
    }

    private static object FileSource(string path)
    {
        var file = new FileInfo(path);
        return new { path, size = file.Length, sha256 = Hashing.Sha256(path) };
    }

    private static string RequireFile(string inputPath)
    {
        var path = Path.GetFullPath(inputPath);
        return File.Exists(path) ? path : throw new FileNotFoundException("Archive was not found.", path);
    }
}

