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
            // Compare resource identifiers before converting them to display names.
            var placedPools = placements
                .Select(placement => SelectedPool(context, type.Path, placement.Properties))
                .ToHashSet(StringComparer.Ordinal);
            foreach (
                var source in Sources(context, type.Path, type.Properties, [], false, placedPools)
            )
            {
                yield return source;
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
        IReadOnlyList<EntityReference> places,
        bool placed,
        IReadOnlySet<string?>? placedPools = null
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
            if ((placed && selected != pool) || (!placed && placedPools?.Contains(pool) == true))
            {
                continue;
            }
            var notes = HarvestNotes(properties).ToList();
            if (!placed)
            {
                notes.Add("No gathering spot in the world is known to give it.");
            }
            foreach (var (item, weight) in WorldSourceData.Pool(properties[pool]))
            {
                var perPick = WeightTotals.Chance(weight);
                yield return new ItemSource(
                    item,
                    new Source(
                        SourceKind.Gathering,
                        SourceName(pool),
                        places,
                        null,
                        null,
                        [],
                        notes
                    )
                    {
                        Unconfirmed =
                            !placed
                            || RecordValues.Number(
                                properties["Maximum amount of items that can be harvested"]
                            )
                                is <= 0,
                        Entity = WorldSourceData.Reference(
                            EntityKind.Resource,
                            path,
                            SourceName(pool)
                        ),
                        Conditions = HarvestConditions(
                            context,
                            properties,
                            places.Count == 1 ? places[0].Key : null
                        ),
                        // A gather picks once, and gathering bonuses can add picks.
                        Odds = perPick is { } chance
                            ? LootOdds.Of(OddsEvent.Pick, chance, PickCount.Fixed(1))
                            : null,
                    }
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
                        SourceName(pool),
                        [],
                        null,
                        null,
                        [],
                        [
                            .. HarvestNotes(properties),
                            "No gathering spot in the world is known to use this gathering method.",
                        ]
                    )
                    {
                        Unconfirmed = true,
                        Entity = WorldSourceData.Reference(
                            EntityKind.Resource,
                            path,
                            SourceName(pool)
                        ),
                        Conditions =
                        [
                            .. WorldSourceData.Conditions(
                                context,
                                properties,
                                places.Count == 1 ? places[0].Key : null
                            ),
                            new RelationCondition(
                                ConditionKind.Unknown,
                                null,
                                null,
                                "Whether this gathering method is active is unknown."
                            ),
                        ],
                        Odds = LootOdds.Of(
                            OddsEvent.Pick,
                            (double)group.Count() / entries.Length,
                            PickCount.Fixed(1)
                        ),
                    }
                );
            }
        }
    }

    private static string SourceName(string resource) =>
        resource switch
        {
            "Iron Deposit Loot" => "Iron Deposit",
            "Copper Deposit Loot" => "Copper Deposit",
            "Yew Tree Loot" => "Yew Tree",
            "Pine Tree Loot" => "Pine Tree",
            "Stone Loot" => "Stone",
            "Berry Bush Loot" or "Berries Bush Items" => "Berry Bush",
            "Mushroom Patch Loot" => "Mushroom Patch",
            "Bone Pile Loot" => "Bone Pile",
            "Magma Stone Loot" => "Magma Stone",
            "Hetrum Deposit Loot" => "Hetrum Deposit",
            "Cinderberry Bush Table" or "Cinderberry Bush items" => "Cinderberry Bush",
            "Shimmerberry Bush Table" or "Shimmerberry Bush items" => "Shimmerberry Bush",
            "Candyberry Bush Table" or "Candyberry Bush items" => "Candyberry Bush",
            "Abyssberry Bush Table" or "Abyssberry Bush items" => "Abyssberry Bush",
            "TreeLootTable" or "Tree Items" => "Tree",
            "Ore Deposit Table" or "Ore Deposit Items" => "Ore Deposit",
            "MushroomLootTable" or "Mushroom Grove items" => "Mushroom Grove",
            "Waterfront Mushroom Grove Table" or "WaterfrontMushroom Items" =>
                "Waterfront Mushroom Grove",
            "Crownberry Bush items" => "Crownberry Bush",
            "Flower Grove items" => "Flower Grove",
            _ => "Gathering point",
        };

    private static IEnumerable<string> HarvestNotes(JsonObject properties)
    {
        if (
            RecordValues.Number(properties["Maximum amount of items that can be harvested"]) is
            { } limit
        )
        {
            yield return FormattableString.Invariant(
                $"Gives up to {limit} {(limit == 1 ? "item" : "items")} before it regrows."
            );
        }
    }

    private static IReadOnlyList<RelationCondition> HarvestConditions(
        CatalogContext context,
        JsonObject properties,
        string? levelPath
    ) =>
        // Every character is at least level 1, so that requirement restricts nothing.
        RecordValues.Number(properties["Required level"])
            is > 1
                and var level
            ?
            [
                .. WorldSourceData.Conditions(context, properties, levelPath),
                new RelationCondition(
                    ConditionKind.GameState,
                    null,
                    level,
                    FormattableString.Invariant($"Requires level {level}.")
                ),
            ]
        : properties.ContainsKey("Required level")
        && RecordValues.Number(properties["Required level"]) is null
            ?
            [
                .. WorldSourceData.Conditions(context, properties, levelPath),
                new RelationCondition(
                    ConditionKind.Unknown,
                    null,
                    null,
                    "The required gathering level could not be read."
                ),
            ]
        : WorldSourceData.Conditions(context, properties, levelPath);

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
                ItemSource? Create(JsonObject properties, IReadOnlyList<EntityReference> places)
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
                    return new ItemSource(
                        item,
                        new Source(
                            SourceKind.Gathering,
                            className == "WheatField_Volume_C" ? "Wheat Patch" : "Warp Spike",
                            places,
                            null,
                            quantity is { } count ? new Amount(count, count) : null,
                            [],
                            [
                                .. HarvestNotes(properties),
                                .. places.Count == 0
                                    ? new[]
                                    {
                                        "No gathering spot in the world is known to give it.",
                                    }
                                    : [],
                                .. quantity is <= 0
                                    ? new[]
                                    {
                                        "The harvested item's quantity is not positive, so it gives no items.",
                                    }
                                    : [],
                                "Received when harvesting. The number of harvesting actions depends on the state of the gathering point.",
                            ]
                        )
                        {
                            Unconfirmed =
                                places.Count == 0
                                || quantity is <= 0
                                || RecordValues.Number(
                                    properties["Maximum amount of items that can be harvested"]
                                )
                                    is <= 0,
                            Entity = WorldSourceData.Reference(
                                EntityKind.Resource,
                                type.Path,
                                className == "WheatField_Volume_C" ? "Wheat Patch" : "Warp Spike"
                            ),
                            Conditions = HarvestConditions(
                                context,
                                properties,
                                places.Count == 1 ? places[0].Key : null
                            ),
                        }
                    );
                }
            }
        }
    }
}
