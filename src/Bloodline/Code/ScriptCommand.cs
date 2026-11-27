using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Nodes;
using Bloodline.Configuration;
using Bloodline.Extraction;
using CUE4Parse.UE4.Objects.UObject;

namespace Bloodline.Code;

/// <summary>
/// Prints the statements of a package's functions, for reading what a function does when the code index, which
/// keeps only what a function reads, writes and calls, does not answer it. The output is game data, so it goes to
/// standard output and never into the repository.
/// </summary>
internal static class ScriptCommand
{
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    public static Command CreateCommand()
    {
        var package = new Argument<string>("package")
        {
            Description =
                "The package, such as /Game/ChallengeLocations/RepeatableDungeonPopulator.",
        };
        var function = new Argument<string?>("function")
        {
            Description =
                "The function to print. Without it, the package's function names are listed.",
            Arity = ArgumentArity.ZeroOrOne,
        };
        var configuration = new Option<string>("--config")
        {
            Description = "Machine-local Steam library and output settings.",
            DefaultValueFactory = _ => LocalConfiguration.DefaultFile,
        };
        var command = new Command(
            "script",
            "Print the statements of a function of the installed game."
        )
        {
            package,
            function,
            configuration,
        };
        command.SetAction(result =>
        {
            var snapshot = InstallationSnapshot.Capture(
                LocalConfiguration.Load(result.GetValue(configuration)!)
            );
            using var provider = ExtractionRunner.OpenGameFiles(snapshot);
            var loaded = provider.LoadPackage(result.GetValue(package)!);
            var wanted = result.GetValue(function);
            var found = false;
            foreach (var export in loaded.ExportsLazy)
            {
                if (export.Value is not UFunction script)
                    continue;
                if (wanted is null)
                {
                    Console.WriteLine(script.Name);
                    found = true;
                }
                else if (script.Name == wanted)
                {
                    var statements = new JsonArray(
                        (script.ScriptBytecode ?? [])
                            .Select(ScriptExpressions.Encode)
                            .ToArray<JsonNode?>()
                    );
                    Console.WriteLine(statements.ToJsonString(Indented));
                    found = true;
                }
            }
            if (found)
                return 0;
            Console.Error.WriteLine("No such function in the package.");
            return 1;
        });
        return command;
    }
}
