using System.Text.RegularExpressions;
using Bloodline.Records;

namespace Bloodline.Catalog;

/// <summary>
/// One package of the build's records: a class, table, conversation, level or enum with its functions.
/// <see cref="Levels"/> counts the levels that hold an actor of the package's class, which makes it something a
/// player can meet. <see cref="Published"/> says that site data or a fact citation names the package, and
/// <see cref="CitedFunctions"/> how many of its functions a published fact rests on.
/// </summary>
internal sealed record MapPackage(
    string Path,
    IReadOnlyList<string> Kinds,
    int Functions,
    int CitedFunctions,
    bool Window,
    int Levels,
    bool Published
);

internal sealed record GameMapReport(long Build, IReadOnlyList<MapPackage> Packages);

/// <summary>
/// The map of everything in the game files, which shows what the site rests on and what nobody has read. It lists
/// every package the records name, so it is complete without anyone keeping it.
/// </summary>
internal static partial class GameMap
{
    [GeneratedRegex("""/Game/[^"\\#]+""")]
    private static partial Regex GamePath();

    /// <summary>The package of an object path, such as <c>/Game/A/B</c> of <c>/Game/A/B.B_C:Field</c>.</summary>
    public static string Package(string path) => path.Split('.', ':', '#')[0];

    /// <summary>The packages that the JSON files under the given directories and files name.</summary>
    public static IReadOnlySet<string> NamedPackages(IEnumerable<string> files)
    {
        var named = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        foreach (Match match in GamePath().Matches(File.ReadAllText(file)))
            named.Add(Package(match.Value));
        return named;
    }

    public static GameMapReport Build(
        CatalogRecords records,
        IReadOnlySet<string> named,
        IEnumerable<FactClaim> claims
    )
    {
        var kinds = new Dictionary<string, SortedSet<string>>(StringComparer.OrdinalIgnoreCase);
        void Add(string path, string kind)
        {
            var package = Package(path);
            if (!kinds.TryGetValue(package, out var held))
                kinds.Add(package, held = new SortedSet<string>(StringComparer.Ordinal));
            held.Add(kind);
        }
        foreach (var type in records.Classes)
            Add(type.Path, "class");
        foreach (var table in records.Tables)
            Add(table.Path, "table");
        foreach (var entry in records.Enums)
            Add(entry.Path, "enum");
        foreach (var level in records.Levels)
            Add(level.Path, "level");
        foreach (var dialogue in records.Dialogues)
            Add(dialogue.Path, "conversation");
        var functions = records.Code?.Functions ?? [];
        foreach (var function in functions)
            Add(function.Package, "code");
        var counts = functions
            .GroupBy(function => function.Package, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (Count: group.Count(), Window: group.Any(function => function.Widget)),
                StringComparer.OrdinalIgnoreCase
            );
        var cited = claims
            .SelectMany(claim => claim.Citations)
            .Select(citation => citation.Function)
            .Distinct(StringComparer.Ordinal)
            .GroupBy(function => function.Split(':')[0], StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Count(),
                StringComparer.OrdinalIgnoreCase
            );
        var levels = records
            .Levels.SelectMany(level =>
                level
                    .Actors.Select(actor => Package(actor.Class))
                    .Distinct()
                    .Select(package => package)
            )
            .GroupBy(package => package, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Count(),
                StringComparer.OrdinalIgnoreCase
            );
        return new GameMapReport(
            records.Build.BuildId,
            kinds
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair =>
                {
                    counts.TryGetValue(pair.Key, out var code);
                    return new MapPackage(
                        pair.Key,
                        pair.Value.ToArray(),
                        code.Count,
                        cited.GetValueOrDefault(pair.Key),
                        code.Window,
                        levels.GetValueOrDefault(pair.Key),
                        named.Contains(pair.Key) || cited.ContainsKey(pair.Key)
                    );
                })
                .ToArray()
        );
    }
}
