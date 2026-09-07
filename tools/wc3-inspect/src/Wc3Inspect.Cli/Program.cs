using System.Text;
using Wc3Inspect;

Console.OutputEncoding = new UTF8Encoding(false);

try
{
    var arguments = args.ToList();
    var reportOutput = CliArguments.TakeOption(arguments, "--output");
    var report = CommandDispatcher.Run(arguments);
    JsonReport.Write(report, reportOutput);
    return 0;
}
catch (CliUsageException exception)
{
    Console.Error.WriteLine(exception.Message);
    Console.Error.WriteLine(CommandDispatcher.Usage);
    return 2;
}
catch (Exception exception)
{
    JsonReport.WriteError(exception);
    return 1;
}

