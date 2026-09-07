using System.Text.Json;

namespace Wc3Inspect;

internal static class JsonReport
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
    };

    public static void Write(object report, string? outputPath)
    {
        var json = JsonSerializer.Serialize(report, Options) + Environment.NewLine;
        Console.Out.Write(json);

        if (outputPath is null)
        {
            return;
        }

        var fullPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, json, new System.Text.UTF8Encoding(false));
    }

    public static void WriteError(Exception exception)
    {
        var error = new
        {
            schemaVersion = 1,
            kind = "error",
            error = new { type = exception.GetType().Name, message = exception.Message },
            warnings = Array.Empty<string>(),
        };

        Console.Error.WriteLine(JsonSerializer.Serialize(error, Options));
    }
}

