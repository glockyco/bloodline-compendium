using System.CommandLine;
using Bloodline.Configuration;

namespace Bloodline.Extraction;

internal static class ExtractCommand
{
    public static Command CreateCommand()
    {
        var configuration = new Option<string>("--config")
        {
            Description = "Machine-local Steam library and output settings.",
            DefaultValueFactory = _ => LocalConfiguration.DefaultFile,
        };
        var command = new Command(
            "extract",
            "Reads the installed game into reproducible local records."
        )
        {
            configuration,
        };
        command.SetAction(result =>
        {
            try
            {
                ExtractionRunner.Run(LocalConfiguration.Load(result.GetValue(configuration)!));
                return 0;
            }
            catch (Exception error) when (error is InvalidDataException or IOException)
            {
                Console.Error.WriteLine(error.Message);
                return 1;
            }
        });
        return command;
    }
}
