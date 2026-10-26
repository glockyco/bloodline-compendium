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
        [9] = "Bone Pile Loot",
        [10] = "Magma Stone Loot",
        [11] = "Hetrum Deposit Loot",
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

    private static readonly (string ClassName, string Field)[] BespokeGrants =
    [
        ("WarpSpike_C", "Warpshards"),
        ("WheatField_Volume_C", "Wheat Ref"),
    ];

    public IEnumerable<ItemSource> FindSources(CatalogContext context)
    {
        foreach (var source in BespokeSources(context))
        {
            yield return source;
        }
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
            if (RecordValues.Boolean(properties["Table Based Preset?"]) == true)
            {
                return null;
            }
            return RecordValues.String(properties["Custom Preset Name"]) switch
            {
                "Iron" => "Iron Deposit Loot",
                "Copper" => "Copper Deposit Loot",
                "Pine" => "Pine Tree Loot",
                _ => "Selected Loot Table",
            };
        }
        var index = WorldSourceData.EnumIndex(
            context,
            properties[harvest ? "Preset" : "GatherPointType"]
        );
        // GatherPoint routes Crownberry to the Abyssberry table. The overworld consumer has no such branch.
        if (!harvest && index == 11 && WorldSourceData.IsClass(context, path, "GatherPoint_C"))
        {
            return "Abyssberry Bush Table";
        }
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
            ? HarvestPools.Values.Append("Selected Loot Table")
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
                    ? "Gathering pool selected by the authored preset. Construction or preset events may replace it with a scripted pool. Harvest attempts and bonuses depend on gathering state."
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

    private static IEnumerable<ItemSource> BespokeSources(CatalogContext context)
    {
        foreach (var (className, field) in BespokeGrants)
        {
            foreach (
                var type in context.Classes.Values.Where(type =>
                    !context.IsExcludedClass(type.Path)
                    && WorldSourceData.IsClass(context, type.Path, className)
                )
            )
            {
                var placements = context
                    .Placements.Where(placement => placement.Actor.Class == type.Path)
                    .ToArray();
                if (placements.Length == 0)
                {
                    if (Create(type.Properties, []) is { } source)
                    {
                        yield return source;
                    }
                }
                foreach (var placement in placements)
                {
                    if (
                        Create(placement.Properties, [context.Places.Get(placement.Level)]) is
                        { } source
                    )
                    {
                        yield return source;
                    }
                }
                ItemSource? Create(JsonObject properties, IReadOnlyList<Place> places)
                {
                    if (RecordValues.AnyReference(properties[field]) is not { } item)
                    {
                        return null;
                    }
                    var quantity = context.Classes.TryGetValue(item, out var output)
                        ? WorldSourceData.Integer(
                            RecordValues.At(output.Properties, "ItemInfo.Quantity")
                        )
                        : null;
                    if (quantity is <= 0)
                    {
                        return null;
                    }
                    return new ItemSource(
                        item,
                        new Source(
                            SourceKind.Gathering,
                            ItemIdentity.Readable(ItemIdentity.ClassName(type.Path)),
                            places,
                            null,
                            quantity is { } count ? new Amount(count, count) : null,
                            null,
                            null,
                            [],
                            [
                                "Granted by the harvesting interaction. The number of harvesting actions depends on interaction state.",
                            ]
                        )
                    );
                }
            }
        }
    }
}
