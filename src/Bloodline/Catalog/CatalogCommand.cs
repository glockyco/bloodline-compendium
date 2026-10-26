using System.CommandLine;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Bloodline.Configuration;
using Bloodline.Extraction;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal static class CatalogCommand
{
    public static Command CreateCommand()
    {
        var config = new Option<FileInfo>("--config")
        {
            Description = "Machine-local configuration file.",
            DefaultValueFactory = _ => new FileInfo(LocalConfiguration.DefaultFile),
        };
        var build = new Option<long?>("--build")
        {
            Description = "Steam build ID. Defaults to the installed build.",
        };
        var command = new Command("catalog", "Build item site data from extracted records.")
        {
            config,
            build,
        };
        command.SetAction(parse =>
        {
            try
            {
                var configuration = LocalConfiguration.Load(parse.GetValue(config)!.FullName);
                var buildId =
                    parse.GetValue(build)
                    ?? SteamManifest
                        .Parse(File.ReadAllText(configuration.AppManifest))
                        .Build.BuildId;
                var layout = new RecordLayout(configuration.OutputDirectory, buildId);
                var records = CatalogRecords.Load(layout);
                if (records.Build.BuildId != buildId)
                {
                    throw new InvalidDataException(
                        $"Records in build {buildId} identify build {records.Build.BuildId}."
                    );
                }
                var releases =
                    JsonSerializer.Deserialize<Dictionary<string, SiteRelease>>(
                        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "releases.json")),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    ) ?? throw new InvalidDataException("The reviewed release registry is empty.");
                if (
                    !releases.TryGetValue(
                        buildId.ToString(CultureInfo.InvariantCulture),
                        out var release
                    )
                )
                {
                    throw new InvalidDataException(
                        $"Build {buildId} has no reviewed entry in releases.json."
                    );
                }
                var commit = Git("rev-parse", "HEAD").Trim();
                var dirty = Git("status", "--porcelain").Length != 0;
                var timer = Stopwatch.StartNew();
                var catalog = CatalogBuilder.Build(records, iconsDirectory: layout.IconsDirectory);
                var provenance = new SiteBuild(
                    records.Build.AppId,
                    records.Build.BuildId,
                    records
                        .Build.Depots.Select(depot => new SiteDepot(
                            depot.DepotId,
                            depot.ManifestId
                        ))
                        .ToArray(),
                    commit,
                    dirty,
                    release
                );
                CatalogWriter.Write(layout, provenance, catalog);
                Console.WriteLine(
                    $"Cataloged {catalog.Items.Count} items for build {buildId} in {timer.Elapsed.TotalSeconds.ToString("F2", CultureInfo.InvariantCulture)} seconds."
                );
                foreach (var category in catalog.Index.Categories)
                {
                    Console.WriteLine($"{category.Name}: {category.Count}");
                }
                Console.WriteLine(
                    $"Pickup sources: {catalog.Items.Sum(item => item.Sources.Count(source => source.Kind == SourceKind.Pickup))}"
                );
                Console.WriteLine($"Named places: {catalog.NamedPlaces}");
                foreach (var gap in catalog.Coverage.Gaps)
                {
                    Console.WriteLine($"{gap.Kind}: {gap.Entries.Count}");
                }
                return 0;
            }
            catch (Exception error)
                when (error
                        is IOException
                            or JsonException
                            or InvalidDataException
                            or ConfigurationException
                            or InvalidOperationException
                )
            {
                Console.Error.WriteLine(error.Message);
                return 2;
            }
        });
        return command;
    }

    private static string Git(params string[] arguments)
    {
        var start = new ProcessStartInfo("git")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }
        using var process =
            Process.Start(start)
            ?? throw new InvalidOperationException(
                "Could not start git for publication provenance."
            );
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return process.ExitCode == 0
            ? output
            : throw new InvalidOperationException($"Git provenance failed: {error.Trim()}");
    }
}
