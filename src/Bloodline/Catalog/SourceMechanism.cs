using System.Text.Json.Nodes;
using Bloodline.Output;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal interface ISourceMechanism
{
    IEnumerable<ItemSource> FindSources(CatalogContext context);
}

internal sealed record ItemSource(
    string ItemClass,
    Source Source,
    ContainerDetails? Container = null
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
    }

    public bool IsExcludedClass(string path) =>
        ExcludedClasses.Contains(path)
        || (
            Classes.TryGetValue(path, out var type) && type.Ancestors.Any(ExcludedClasses.Contains)
        );

    public ItemLink ItemLink(string path) =>
        Items.TryGetValue(path, out var item)
            ? new ItemLink(IsExcludedClass(path) ? null : item.Slug, ItemIdentity.DisplayName(item))
            : new ItemLink(null, ItemIdentity.Readable(ItemIdentity.ClassName(path)));

    public static IReadOnlyList<Source> MergeSources(
        IEnumerable<ItemSource> candidates,
        IReadOnlySet<string> excludedLevels
    ) => MergeKnownPlaces(ContainerSourceMerge.Merge(candidates, excludedLevels));

    private static IReadOnlyList<Source> MergeKnownPlaces(IEnumerable<Source> candidates)
    {
        var groups = new Dictionary<string, (Source Source, Dictionary<string, Place> Places)>(
            StringComparer.Ordinal
        );
        foreach (var source in candidates)
        {
            var allowed = source.Places;
            var key = CanonicalJson.Serialize(source with { Places = [] });
            if (!groups.TryGetValue(key, out var group))
            {
                group = (source, new Dictionary<string, Place>(StringComparer.OrdinalIgnoreCase));
                groups.Add(key, group);
            }
            foreach (var place in allowed)
            {
                group.Places.TryAdd(place.Id, place);
            }
        }
        return groups
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair =>
                pair.Value.Source with
                {
                    Places = pair
                        .Value.Places.Values.OrderBy(place => place.Name, StringComparer.Ordinal)
                        .ThenBy(place => place.Id, StringComparer.Ordinal)
                        .ToArray(),
                }
            )
            .ToArray();
    }
}
