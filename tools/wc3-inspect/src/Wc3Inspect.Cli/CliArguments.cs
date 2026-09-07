namespace Wc3Inspect;

internal sealed class CliUsageException(string message) : Exception(message);

internal static class CliArguments
{
    public static string? TakeOption(List<string> arguments, string option)
    {
        var index = arguments.FindIndex(value => string.Equals(value, option, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return null;
        }

        if (index + 1 >= arguments.Count)
        {
            throw new CliUsageException($"Missing value for {option}.");
        }

        var value = arguments[index + 1];
        arguments.RemoveRange(index, 2);
        return value;
    }

    public static bool TakeFlag(List<string> arguments, string flag)
    {
        var index = arguments.FindIndex(value => string.Equals(value, flag, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return false;
        }

        arguments.RemoveAt(index);
        return true;
    }

    public static long TakeLongOption(List<string> arguments, string option, long defaultValue)
    {
        var raw = TakeOption(arguments, option);
        if (raw is null)
        {
            return defaultValue;
        }

        if (!long.TryParse(raw, out var value) || value <= 0)
        {
            throw new CliUsageException($"{option} must be a positive integer.");
        }

        return value;
    }
}

