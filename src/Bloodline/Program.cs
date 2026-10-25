using System.CommandLine;
using Bloodline.Configuration;

var root = new RootCommand("Builds The Bloodline Compendium's data from the installed game.");

try
{
    return await root.Parse(args)
        .InvokeAsync(new InvocationConfiguration { EnableDefaultExceptionHandler = false })
        .ConfigureAwait(false);
}
catch (ConfigurationException error)
{
    await Console.Error.WriteLineAsync(error.Message).ConfigureAwait(false);
    return 2;
}
