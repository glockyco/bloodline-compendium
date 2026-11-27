using System.Text.Json;
using System.Text.RegularExpressions;
using Bloodline.Records;

namespace Bloodline.Catalog;

/// <summary>
/// One rule of <c>coverage/game-map.json</c>. A package belongs to the system of the first rule whose conditions
/// all hold: a class of the package is or derives from <see cref="Ancestor"/>, the package's path starts with
/// <see cref="Prefix"/>, the package holds a record of <see cref="Kind"/>, or, for <see cref="Loose"/>, the package
/// lies directly under <c>/Game</c>. <see cref="SetAside"/> gives the reason nobody needs to read the package.
/// </summary>
internal sealed record MapRule(
    string System,
    string? Ancestor = null,
    string? Prefix = null,
    string? Kind = null,
    bool Loose = false,
    string? SetAside = null
);

internal sealed record GameMapRegistry(IReadOnlyList<MapRule> Rules)
{
    public static GameMapRegistry Load(string path) =>
        JsonSerializer.Deserialize<GameMapRegistry>(
            File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        ) ?? throw new InvalidDataException("The game map registry is empty.");
}

internal enum MapState
{
    /// <summary>Site data or a fact citation names the package.</summary>
    Published,

    /// <summary>The registry gives a reason nobody needs to read the package.</summary>
    SetAside,

    /// <summary>Nobody has read the package.</summary>
    Open,
}

/// <summary>
/// One package of the build's records: a class, table, conversation, level or enum with its functions.
/// <see cref="Levels"/> counts the levels that hold an actor of the package's class, which makes it something a
/// player can meet. <see cref="CitedFunctions"/> says how many of its functions a published fact rests on.
/// </summary>
internal sealed record MapPackage(
    string Path,
    IReadOnlyList<string> Kinds,
    int Functions,
    int CitedFunctions,
    bool Window,
    int Levels,
    string System,
    MapState State,
    string? Reason
);

/// <summary>
/// A system as a player would name it, with how much of it the site rests on. The open windows and placed objects
/// are what a player can open or meet without any page describing it.
/// </summary>
internal sealed record MapSystem(
    string Name,
    int Packages,
    int Functions,
    int CitedFunctions,
    int Published,
    int SetAside,
    int Open,
    int OpenFunctions,
    IReadOnlyList<string> OpenWindows,
    IReadOnlyList<string> OpenPlaced,
    IReadOnlyList<string> SetAsidePlaced
);

internal sealed record GameMapReport(
    long Build,
    IReadOnlyList<MapSystem> Systems,
    IReadOnlyList<MapPackage> Packages
);

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

    /// <summary>The packages that the given JSON files name.</summary>
    public static IReadOnlySet<string> NamedPackages(IEnumerable<string> files)
    {
        var named = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        foreach (Match match in GamePath().Matches(File.ReadAllText(file)))
            named.Add(Package(match.Value));
        return named;
    }

    /// <summary>
    /// The map, and the packages that no rule of the registry covers, which nobody has decided anything about.
    /// </summary>
    public static (GameMapReport Map, IReadOnlyList<string> Unclassified) Build(
        CatalogRecords records,
        GameMapRegistry registry,
        IReadOnlySet<string> named,
        IEnumerable<FactClaim> claims,
        IEnumerable<string>? readings = null
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
            .Concat(readings ?? [])
            .Distinct(StringComparer.Ordinal)
            .GroupBy(function => function.Split(':')[0], StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Count(),
                StringComparer.OrdinalIgnoreCase
            );
        var levels = records
            .Levels.SelectMany(level =>
                level.Actors.Select(actor => Package(actor.Class)).Distinct()
            )
            .GroupBy(package => package, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Count(),
                StringComparer.OrdinalIgnoreCase
            );
        var lineage = records
            .Classes.GroupBy(type => Package(type.Path), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group =>
                    group
                        .SelectMany(type => type.Ancestors.Prepend(type.Path))
                        .ToHashSet(StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase
            );
        bool Matches(MapRule rule, string package, SortedSet<string> held) =>
            (rule.Kind is null || held.Contains(rule.Kind))
            && (
                rule.Prefix is null
                || (package + "/").StartsWith(rule.Prefix, StringComparison.OrdinalIgnoreCase)
            )
            && (
                !rule.Loose
                || (
                    package.StartsWith("/Game/", StringComparison.Ordinal)
                    && package.Count(letter => letter == '/') == 2
                )
            )
            && (
                rule.Ancestor is null
                || (lineage.TryGetValue(package, out var line) && line.Contains(rule.Ancestor))
            );

        var packages = new List<MapPackage>();
        var unclassified = new List<string>();
        foreach (var (package, held) in kinds.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            var rule = registry.Rules.FirstOrDefault(rule => Matches(rule, package, held));
            if (rule is null)
            {
                unclassified.Add(package);
                continue;
            }
            counts.TryGetValue(package, out var code);
            // What the site names is published, whatever the registry says about its folder.
            var state =
                named.Contains(package) || cited.ContainsKey(package) ? MapState.Published
                : rule.SetAside is not null ? MapState.SetAside
                : MapState.Open;
            packages.Add(
                new MapPackage(
                    package,
                    held.ToArray(),
                    code.Count,
                    cited.GetValueOrDefault(package),
                    code.Window,
                    levels.GetValueOrDefault(package),
                    rule.System,
                    state,
                    state == MapState.SetAside ? rule.SetAside : null
                )
            );
        }
        var systems = packages
            .GroupBy(package => package.System, StringComparer.Ordinal)
            .Select(group =>
            {
                var open = group.Where(package => package.State == MapState.Open).ToArray();
                return new MapSystem(
                    group.Key,
                    group.Count(),
                    group.Sum(package => package.Functions),
                    group.Sum(package => package.CitedFunctions),
                    group.Count(package => package.State == MapState.Published),
                    group.Count(package => package.State == MapState.SetAside),
                    open.Length,
                    open.Sum(package => package.Functions),
                    open.Where(package => package.Window).Select(package => package.Path).ToArray(),
                    open.Where(package => package.Levels > 0)
                        .Select(package => package.Path)
                        .ToArray(),
                    // A class that stands in a level is something a player meets, so a folder set aside as a
                    // whole shows what it would hide.
                    group
                        .Where(package => package.State == MapState.SetAside && package.Levels > 0)
                        .Select(package => package.Path)
                        .ToArray()
                );
            })
            .OrderByDescending(system => system.OpenFunctions)
            .ThenBy(system => system.Name, StringComparer.Ordinal)
            .ToArray();
        return (new GameMapReport(records.Build.BuildId, systems, packages), unclassified);
    }
}
