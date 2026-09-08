namespace Wc3Inspect;

internal static class CommandDispatcher
{
    public const string Usage = """
Usage:
  wc3-inspect detect <path> [--output <report.json>]
  wc3-inspect game version <game-directory> [--output <report.json>]
  wc3-inspect game inspect <game-directory> --entry <archive-entry> [--max-bytes <n>] [--output <report.json>]
  wc3-inspect archive inspect <archive> [--output <report.json>]
  wc3-inspect archive extract <archive> <entry> <output-file> [--max-bytes <n>] [--overwrite]
  wc3-inspect map inspect <map.w3m-or-w3x> [--output <report.json>]
  wc3-inspect campaign inspect <campaign.w3n> [--output <report.json>]
  wc3-inspect gamecache inspect <cache.w3v> [--output <report.json>]
""";

    public static object Run(List<string> arguments)
    {
        if (arguments.Count < 2)
        {
            throw new CliUsageException("A command and input path are required.");
        }

        return arguments[0].ToLowerInvariant() switch
        {
            "detect" => RunDetect(arguments),
            "game" => RunGame(arguments),
            "archive" => RunArchive(arguments),
            "map" => RunMap(arguments),
            "campaign" => RunCampaign(arguments),
            "gamecache" => RunGameCache(arguments),
            _ => throw new CliUsageException($"Unknown command: {arguments[0]}")
        };
    }

    private static object RunDetect(List<string> arguments)
    {
        RequireCount(arguments, 2);
        return InputInspector.Detect(arguments[1]);
    }

    private static object RunGame(List<string> arguments)
    {
        if (arguments.Count < 3)
        {
            throw new CliUsageException("The game command requires an action and game directory.");
        }

        return arguments[1].ToLowerInvariant() switch
        {
            "version" => RunGameVersion(arguments),
            "inspect" => RunGameInspect(arguments),
            _ => throw new CliUsageException($"Unknown game action: {arguments[1]}")
        };
    }

    private static object RunGameVersion(List<string> arguments)
    {
        RequireCount(arguments, 3);
        return GameInspector.InspectVersion(arguments[2]);
    }

    private static object RunGameInspect(List<string> arguments)
    {
        var entry = CliArguments.TakeOption(arguments, "--entry")
            ?? throw new CliUsageException("game inspect requires --entry.");
        var maxBytes = CliArguments.TakeLongOption(arguments, "--max-bytes", ArchiveService.DefaultMaxBytes);
        RequireCount(arguments, 3);
        return GameInspector.InspectMap(arguments[2], entry, maxBytes);
    }

    private static object RunArchive(List<string> arguments)
    {
        if (arguments.Count < 3)
        {
            throw new CliUsageException("The archive command requires an action and archive path.");
        }

        return arguments[1].ToLowerInvariant() switch
        {
            "inspect" => RunArchiveInspect(arguments),
            "extract" => RunArchiveExtract(arguments),
            _ => throw new CliUsageException($"Unknown archive action: {arguments[1]}")
        };
    }

    private static object RunArchiveInspect(List<string> arguments)
    {
        RequireCount(arguments, 3);
        return ArchiveService.Inspect(arguments[2]);
    }

    private static object RunArchiveExtract(List<string> arguments)
    {
        var overwrite = CliArguments.TakeFlag(arguments, "--overwrite");
        var maxBytes = CliArguments.TakeLongOption(arguments, "--max-bytes", ArchiveService.DefaultMaxBytes);
        RequireCount(arguments, 5);
        return ArchiveService.Extract(arguments[2], arguments[3], arguments[4], maxBytes, overwrite);
    }

    private static object RunMap(List<string> arguments)
    {
        RequireAction(arguments, "inspect");
        return MapInspector.InspectFile(arguments[2]);
    }

    private static object RunCampaign(List<string> arguments)
    {
        RequireAction(arguments, "inspect");
        return CampaignInspector.Inspect(arguments[2]);
    }

    private static object RunGameCache(List<string> arguments)
    {
        RequireAction(arguments, "inspect");
        return GameCacheInspector.Inspect(arguments[2]);
    }

    private static void RequireAction(List<string> arguments, string action)
    {
        RequireCount(arguments, 3);
        if (!string.Equals(arguments[1], action, StringComparison.OrdinalIgnoreCase))
        {
            throw new CliUsageException($"Expected action '{action}'.");
        }
    }

    private static void RequireCount(List<string> arguments, int expected)
    {
        if (arguments.Count != expected)
        {
            throw new CliUsageException($"Expected {expected} arguments after processing options, received {arguments.Count}.");
        }
    }
}
