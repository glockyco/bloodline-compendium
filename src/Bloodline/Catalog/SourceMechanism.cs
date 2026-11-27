using System.Text.Json.Nodes;
using Bloodline.Output;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal interface ISourceMechanism
{
    IEnumerable<EntitySource> FindSources(CatalogContext context);
}

/// <summary>
/// A source of an entity as a mechanism finds it: <see cref="Key"/> is the key of the entity of
/// <see cref="Kind"/> that the source gives, such as an item's class path. <see cref="Unresolved"/> holds
/// requirements the catalog could not resolve into anything a player can act on, which go to the maintainer report
/// instead of the page.
/// </summary>
internal sealed record EntitySource(string Key, Source Source, ContainerDetails? Container = null)
{
    public EntityKind Kind { get; init; } = EntityKind.Item;

    public IReadOnlyList<string> Unresolved { get; init; } = [];
}

/// <summary>Dialogue requirements a page shows, and those that only the maintainer report lists.</summary>
internal sealed record DialogueRequirements(
    IReadOnlyList<RelationCondition> Shown,
    IReadOnlyList<string> Unresolved
);

internal sealed record Placement(string Level, ActorRecord Actor, JsonObject Properties);

internal sealed class CatalogContext
{
    public CatalogRecords Records { get; }
    public IReadOnlyDictionary<string, ClassRecord> Classes { get; }
    public IReadOnlyDictionary<string, ItemPage> Items { get; }
    public IReadOnlyList<Placement> Placements { get; }
    public Places Places { get; }
    public IReadOnlySet<string> ExcludedLevels { get; }
    public IReadOnlySet<string> ExcludedClasses { get; }
    public IReadOnlySet<string> SharedPeopleClasses { get; }

    /// <summary>Dialogues that differently named people share, such as one quartermaster conversation.</summary>
    public IReadOnlySet<string> SharedDialogues { get; }

    private DialogueTags? tags;

    public DialogueTags Tags => tags ??= new DialogueTags(this);

    private SkillTrees? skillTrees;

    /// <summary>The skill trees a player reaches, which skill and spellsong sources and producers share.</summary>
    public SkillTrees SkillTrees => skillTrees ??= new SkillTrees(this);

    private SpawnSites? spawnSites;

    /// <summary>Where spawners, level scripts and read spawn sites bring a class into the world.</summary>
    public SpawnSites SpawnSites => spawnSites ??= new SpawnSites(this);

    public CatalogContext(
        CatalogRecords records,
        IReadOnlyDictionary<string, ItemPage> items,
        IReadOnlyList<Exclusion> exclusions
    )
    {
        Records = records;
        Items = items;
        Classes = records.Classes.ToDictionary(item => item.Path, StringComparer.OrdinalIgnoreCase);
        ExcludedLevels = exclusions
            .Where(entry => entry.Kind == ExclusionKind.Level)
            .Select(entry => entry.Path)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        ExcludedClasses = exclusions
            .Where(entry => entry.Kind == ExclusionKind.Class)
            .Select(entry => entry.Path)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Places = new Places(records, ExcludedLevels);
        Placements = records
            .Levels.Where(level => !ExcludedLevels.Contains(level.Path))
            .SelectMany(level =>
                level
                    .Actors.Where(actor => !IsExcludedClass(actor.Class))
                    .Select(actor => new Placement(
                        level.Path,
                        actor,
                        Classes.TryGetValue(actor.Class, out var type)
                            ? RecordMerge.OverlayProperties(type.Properties, actor.Properties)
                            : actor.Properties
                    ))
            )
            .ToArray();
        SharedPeopleClasses = Placements
            .GroupBy(placement => placement.Actor.Class, StringComparer.OrdinalIgnoreCase)
            .Where(group =>
                group
                    .Select(placement =>
                        WorldSourceData.Name(placement.Properties, placement.Actor.Class)
                    )
                    .Distinct(StringComparer.Ordinal)
                    .Skip(1)
                    .Any()
            )
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        SharedDialogues = Placements
            .Select(placement =>
                (
                    Dialogue: RecordValues.AnyReference(placement.Properties["Dialogue"]),
                    Name: WorldSourceData.Name(placement.Properties, placement.Actor.Class)
                )
            )
            .Where(entry => entry.Dialogue is not null)
            .GroupBy(entry => entry.Dialogue!, StringComparer.OrdinalIgnoreCase)
            .Where(group =>
                group.Select(entry => entry.Name).Distinct(StringComparer.Ordinal).Skip(1).Any()
            )
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public bool IsExcludedClass(string path) =>
        ExcludedClasses.Contains(path)
        || (
            Classes.TryGetValue(path, out var type) && type.Ancestors.Any(ExcludedClasses.Contains)
        );

    private Liveness? liveness;

    /// <summary>
    /// Whether anything in a level in play could bring the class into the world, as <see cref="Liveness"/> follows
    /// it. A class that no level holds and nothing there leads to cannot be found in play.
    /// </summary>
    public bool CanAppear(string path) => (liveness ??= new Liveness(this)).IsLive(path);

    public EntityReference ItemLink(string path) =>
        Items.TryGetValue(path, out var item) && !IsExcludedClass(path)
            ? CatalogGraph.Reference(item)
            : new EntityReference(
                EntityKind.Item,
                null,
                Items.TryGetValue(path, out var excluded)
                    ? excluded.Name
                    : ItemIdentity.Readable(ItemIdentity.ClassName(path)),
                null,
                null
            );

    public static IReadOnlyList<Source> MergeSources(
        IEnumerable<EntitySource> candidates,
        IReadOnlySet<string> excludedLevels
    ) => MergeKnownPlaces(ContainerSourceMerge.Merge(candidates, excludedLevels));

    private static IReadOnlyList<Source> MergeKnownPlaces(IEnumerable<Source> candidates)
    {
        var groups = new Dictionary<
            string,
            (Source Source, Dictionary<string, EntityReference> Places)
        >(StringComparer.Ordinal);
        foreach (var source in candidates)
        {
            var allowed = source.Places;
            var key = CanonicalJson.Serialize(source with { Places = [] });
            if (!groups.TryGetValue(key, out var group))
            {
                group = (
                    source,
                    new Dictionary<string, EntityReference>(StringComparer.OrdinalIgnoreCase)
                );
                groups.Add(key, group);
            }
            foreach (var place in allowed)
            {
                group.Places.TryAdd(place.Key!, place);
            }
        }
        return groups
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair =>
                pair.Value.Source with
                {
                    Places = pair
                        .Value.Places.Values.OrderBy(place => place.Name, StringComparer.Ordinal)
                        .ThenBy(place => place.Key, StringComparer.Ordinal)
                        .ToArray(),
                }
            )
            .ToArray();
    }
}

/// <summary>
/// One gathering spot or spawner that gives <see cref="Resource"/> in <see cref="Place"/>. <see cref="Count"/> is how
/// many harvestables it holds on average and <see cref="Most"/> at most, both null when no run has measured them.
/// <see cref="Chance"/> is the chance it is there when the place loads, and <see cref="Share"/> the chance each of its
/// harvestables is this resource, below 1 when the game draws the resource from a table by weight.
/// </summary>
internal sealed record GatherSpot(
    EntityReference Resource,
    EntityReference Place,
    double? Count,
    int? Most,
    double Chance,
    double Share
);
