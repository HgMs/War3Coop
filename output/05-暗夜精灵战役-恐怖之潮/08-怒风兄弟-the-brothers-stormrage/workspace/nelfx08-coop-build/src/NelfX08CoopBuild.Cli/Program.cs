using System.Text;

namespace NelfX08CoopBuild;

internal static class Program
{
    private const string Usage = "Usage: NelfX08CoopBuild <NightElfX08-1.24e.w3x> <output.w3x>";

    public static int Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);

        try
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine(Usage);
                return 2;
            }

            var result = NelfX08Builder.Build(args[0], args[1]);
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
