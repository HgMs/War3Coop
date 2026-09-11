using System.Text;

namespace NightElf07CoopBuild;

internal static class Program
{
    private const string Usage = "Usage: nightelf07-coop-build nightelf07 <original.w3m> <output.w3x>";

    public static int Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);

        try
        {
            if (args.Length != 3 || !args[0].Equals("nightelf07", StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine(Usage);
                return 2;
            }

            var result = NightElf07Builder.Build(args[1], args[2]);
            Console.WriteLine($"Built {result.OutputPath}");
            Console.WriteLine($"Source SHA-256: {result.SourceSha256}");
            Console.WriteLine($"Output SHA-256: {result.OutputSha256}");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }
}
