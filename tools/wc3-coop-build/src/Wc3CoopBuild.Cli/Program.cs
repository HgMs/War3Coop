using System.Text;

namespace Wc3CoopBuild;

internal static class Program
{
    private const string Usage = "Usage: wc3-coop-build orc08 <original.w3m> <output.w3x>";

    public static int Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);

        try
        {
            if (args.Length != 3 || !args[0].Equals("orc08", StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine(Usage);
                return 2;
            }

            var result = Orc08Builder.Build(args[1], args[2]);
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
