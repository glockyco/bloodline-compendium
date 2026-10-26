using System.CommandLine;
using System.Text.Json;
using Bloodline.Catalog;
using Bloodline.Output;
using Bloodline.Records;

namespace Bloodline.Code;

internal sealed record CitedFact(string Item, string Fact);

internal sealed record CitationChange(
    string Function,
    string PreviousFingerprint,
    string? CurrentFingerprint,
    CodeParseStatus? ParseStatus,
    IReadOnlyList<CitedFact> Facts
);

internal sealed record BuildComparison(
    long PreviousBuild,
    long CurrentBuild,
    IReadOnlyList<CitationChange> ChangedCitations
);

internal static class CompareCommand
{
    public static BuildComparison Compare(string previousDirectory, string currentDirectory)
    {
        var previous = CanonicalJson.Read<ClaimReport>(
            Path.Combine(previousDirectory, "item-claims.json")
        );
        var current = CanonicalJson.Read<BuildRecord>(
            Path.Combine(currentDirectory, "records", "build.json")
        );
        var code = CanonicalJson.Read<CodeIndex>(
            Path.Combine(currentDirectory, "records", "code", "index.json")
        );
        return Compare(previous, current.BuildId, code);
    }

    public static BuildComparison Compare(
        ClaimReport previous,
        long currentBuild,
        CodeIndex current
    )
    {
        var functions = current.Functions.ToDictionary(
            function => function.Citation,
            StringComparer.Ordinal
        );
        var changes = previous
            .Facts.SelectMany(fact =>
                fact.Citations.Select(citation => (Fact: fact, Citation: citation))
            )
            .GroupBy(entry => entry.Citation.Function, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                var before = group.First().Citation.Fingerprint;
                var after = functions.GetValueOrDefault(group.Key);
                return after?.Fingerprint == before && after.ParseStatus == CodeParseStatus.Complete
                    ? null
                    : new CitationChange(
                        group.Key,
                        before,
                        after?.Fingerprint,
                        after?.ParseStatus,
                        group
                            .Select(entry => new CitedFact(entry.Fact.Item, entry.Fact.Fact))
                            .Distinct()
                            .OrderBy(fact => fact.Item, StringComparer.Ordinal)
                            .ThenBy(fact => fact.Fact, StringComparer.Ordinal)
                            .ToArray()
                    );
            })
            .OfType<CitationChange>()
            .ToArray();
        return new BuildComparison(previous.Build, currentBuild, changes);
    }

    public static Command CreateCommand()
    {
        var previous = new Argument<DirectoryInfo>("previous")
        {
            Description = "Previous build's output directory.",
        };
        var current = new Argument<DirectoryInfo>("current")
        {
            Description = "Current build's output directory.",
        };
        var output = new Option<FileInfo?>("--output")
        {
            Description = "Write the JSON report to this file instead of stdout.",
        };
        var command = new Command(
            "compare",
            "List changed code citations and the facts that require review."
        )
        {
            previous,
            current,
            output,
        };
        command.SetAction(parse =>
        {
            try
            {
                var comparison = Compare(
                    parse.GetValue(previous)!.FullName,
                    parse.GetValue(current)!.FullName
                );
                if (parse.GetValue(output) is { } file)
                    CanonicalJson.Write(file.FullName, comparison);
                else
                    Console.Write(CanonicalJson.Serialize(comparison));
                return 0;
            }
            catch (Exception error)
                when (error
                        is IOException
                            or JsonException
                            or InvalidDataException
                            or InvalidOperationException
                )
            {
                Console.Error.WriteLine(error.Message);
                return 2;
            }
        });
        return command;
    }
}
