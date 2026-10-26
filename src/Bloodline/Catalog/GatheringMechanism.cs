using System.Text.Json.Nodes;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed class GatheringMechanism : ISourceMechanism
{
    private static readonly IReadOnlyDictionary<int, string> HarvestPools = new Dictionary<
        int,
        string
    >
    {
        [1] = "Iron Deposit Loot",
        [2] = "Copper Deposit Loot",
        [4] = "Yew Tree Loot",
        [5] = "Pine Tree Loot",
        [6] = "Stone Loot",
        [7] = "Berry Bush Loot",
        [8] = "Mushroom Patch Loot",
        [11] = "Bone Pile Loot",
    };
    private static readonly IReadOnlyDictionary<int, string> GatherPools = new Dictionary<
        int,
        string
    >
    {
        [0] = "Cinderberry Bush Table",
        [1] = "Shimmerberry Bush Table",
        [2] = "Candyberry Bush Table",
        [3] = "Abyssberry Bush Table",
        [4] = "TreeLootTable",
        [5] = "Ore Deposit Table",
        [9] = "MushroomLootTable",
        [10] = "Waterfront Mushroom Grove Table",
        [11] = "Abyssberry Bush Table",
    };
    private static readonly string[] UniformPools =
    [
        "Berries Bush Items",
        "Tree Items",
        "Ore Deposit Items",
        "Cinderberry Bush items",
        "Shimmerberry Bush items",
        "Abyssberry Bush items",
        "Candyberry Bush items",
        "Mushroom Grove items",
        "Crownberry Bush items",
        "Flower Grove items",
        "WaterfrontMushroom Items",
    ];

    public IEnumerable<ItemSource> FindSources(CatalogContext context)
    {
        foreach (
            var type in context.Classes.Values.Where(type =>
                !context.IsExcludedClass(type.Path) && IsGatherer(context, type.Path)
            )
        )
        {
            var placements = context
                .Placements.Where(placement => placement.Actor.Class == type.Path)
                .ToArray();
            foreach (var source in Sources(context, type.Path, type.Properties, [], false))
            {
                // Defaults can describe selectable pools not selected by any known placement.
                if (
                    !placements.Any(placement =>
                        SelectedPool(context, type.Path, placement.Properties) == source.Source.Name
                    )
                )
                {
                    yield return source;
                }
            }
            foreach (var placement in placements)
            {
                foreach (
                    var source in Sources(
                        context,
                        type.Path,
                        placement.Properties,
                        [context.Places.Get(placement.Level)],
                        true
                    )
                )
                {
                    yield return source;
                }
            }
        }
    }

    private static bool IsGatherer(CatalogContext context, string path) =>
        WorldSourceData.IsClass(context, path, "HarvestableInteractionObject_C")
        || WorldSourceData.IsClass(context, path, "GatherPoint_C")
        || WorldSourceData.IsClass(context, path, "Overworld_Gatherpoint_C");

    private static string? SelectedPool(CatalogContext context, string path, JsonObject properties)
    {
        var harvest = WorldSourceData.IsClass(context, path, "HarvestableInteractionObject_C");
        if (harvest && RecordValues.Boolean(properties["Custom Preset"]) == true)
        {
            return "Selected Loot Table";
        }
        var index = WorldSourceData.EnumIndex(properties[harvest ? "Preset" : "GatherPointType"]);
        var pools = harvest ? HarvestPools : GatherPools;
        return index is { } value && pools.TryGetValue(value, out var pool)
            ? pool
            : "Selected Loot Table";
    }

    private static IEnumerable<ItemSource> Sources(
        CatalogContext context,
        string path,
        JsonObject properties,
        IReadOnlyList<Place> places,
        bool placed
    )
    {
        var harvest = WorldSourceData.IsClass(context, path, "HarvestableInteractionObject_C");
        var overworld = WorldSourceData.IsClass(context, path, "Overworld_Gatherpoint_C");
        var selected = SelectedPool(context, path, properties);
        var pools = harvest
            ? HarvestPools
                .Values.Append("Selected Loot Table")
                .Append("Magma Stone Loot")
                .Append("Hetrum Deposit Loot")
            : GatherPools.Values;
        foreach (var pool in pools.Distinct(StringComparer.Ordinal))
        {
            if (placed && selected != pool)
            {
                continue;
            }
            var notes = WorldSourceData
                .Notes(
                    properties,
                    "Maximum amount of items that can be harvested",
                    "Required level",
                    "Custom Preset Name"
                )
                .ToList();
            notes.Add(
                placed
                    ? "Selected gathering pool. Harvest attempts and bonuses depend on the resource and gathering state."
                    : "Selectable resource pool. Activation is not resolved to a placed gather point."
            );
            foreach (var (item, weight) in WorldSourceData.Pool(properties[pool]))
            {
                yield return new ItemSource(
                    item,
                    new Source(
                        SourceKind.Gathering,
                        pool,
                        places,
                        null,
                        null,
                        weight,
                        null,
                        [],
                        notes
                    )
                );
            }
        }
        if (!overworld)
        {
            yield break;
        }
        foreach (var pool in UniformPools)
        {
            var entries = RecordValues.Elements(properties[pool]).ToArray();
            foreach (
                var group in entries
                    .Select(RecordValues.AnyReference)
                    .Where(item => item is not null)
                    .GroupBy(item => item!, StringComparer.OrdinalIgnoreCase)
            )
            {
                yield return new ItemSource(
                    group.Key,
                    new Source(
                        SourceKind.Gathering,
                        pool,
                        [],
                        null,
                        null,
                        null,
                        (double)group.Count() / entries.Length,
                        [],
                        [
                            "Uniform selection from the resource array. Activation of this array-based path is unresolved.",
                            .. WorldSourceData.Notes(
                                properties,
                                "Maximum amount of items that can be harvested"
                            ),
                        ]
                    )
                );
            }
        }
    }
}
