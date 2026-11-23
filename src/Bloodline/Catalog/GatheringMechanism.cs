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

    public IEnumerable<EntitySource> FindSources(CatalogContext context)
    {
        CheckPresets(context);
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
            var spawned =
                ItemIdentity.ClassName(type.Path) == "HarvestableInteractionObject"
                    ? SpawnedHarvestables(context, type).ToArray()
                    : [];
            // Compare resource identifiers before converting them to display names.
            var placedPools = placements
                .Select(placement => SelectedPool(context, type.Path, placement.Properties))
                .ToHashSet(StringComparer.Ordinal);
            // A placed preset with a pool's name and loot, such as Pine and Pine Tree Loot (run presets2), gives that
            // pool's items, so the pool needs no unplaced row. Hetrum's loot differs from Hetrum Deposit Loot's.
            var placedPresets = placements
                .Select(placement => placement.Properties)
                .Concat(spawned.Select(spot => spot.Properties))
                .SelectMany(properties => PresetNames(properties).OfType<string>())
                .Distinct(StringComparer.Ordinal)
                .Select(name => Preset(context, name))
                .ToArray();
            placedPools.UnionWith(
                HarvestPools.Values.Where(pool =>
                    placedPresets.Any(preset =>
                        PresetName(preset) == SourceName(pool)
                        && SameLoot(preset, WorldSourceData.Pool(type.Properties[pool]))
                    )
                )
            );
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
            foreach (var spot in spawned)
            {
                foreach (
                    var source in Sources(
                        context,
                        type.Path,
                        spot.Properties,
                        [context.Places.Get(spot.Level)],
                        true
                    )
                )
                {
                    yield return source;
                }
            }
        }
    }

    /// <summary>
    /// Every gathering spot in play: each placement and each spawner of a gatherer, with the resources its rows name in
    /// that place. A placement with a custom spawn chance is there on a load with that chance, which the game rolls
    /// again each time the place loads, as The Elderglen Woods held 97 Coal deposits on one load and 36 on the next
    /// (runs yield1 and yield7). A spawner holds as many as it spawns on average and at most, or null for a dungeon
    /// populator, whose outcrop count no run has measured.
    /// </summary>
    public static IEnumerable<GatherSpot> Spots(CatalogContext context)
    {
        foreach (
            var type in context.Classes.Values.Where(type =>
                !context.IsExcludedClass(type.Path) && IsGatherer(context, type.Path)
            )
        )
        {
            var spawned =
                ItemIdentity.ClassName(type.Path) == "HarvestableInteractionObject"
                    ? SpawnedHarvestables(context, type)
                    : [];
            var spots = context
                .Placements.Where(placement => placement.Actor.Class == type.Path)
                .Select(placement =>
                    (
                        placement.Properties,
                        placement.Level,
                        Spawner: false,
                        Count: (double?)1,
                        Most: (int?)1
                    )
                )
                .Concat(
                    spawned.Select(spot =>
                        (spot.Properties, spot.Level, Spawner: true, spot.Count, spot.Most)
                    )
                );
            foreach (var (properties, level, spawner, count, most) in spots)
            {
                var place = context.Places.Get(level);
                var resources = Sources(context, type.Path, properties, [place], true)
                    .Where(source => !source.Source.Unconfirmed)
                    .Select(source => source.Source.Entity)
                    .DistinctBy(entity => entity.Key)
                    .ToArray();
                var chance =
                    !spawner && RecordValues.Boolean(properties["Use Custom Spawn Chance?"]) == true
                        ? RecordValues.Number(properties["Custom Spawn Chance"]) ?? 1
                        : 1;
                var shares = Shares(context, properties);
                foreach (var resource in resources)
                    yield return new GatherSpot(
                        resource,
                        place,
                        count,
                        most,
                        chance,
                        shares.TryGetValue(resource.Name, out var share) ? share
                            : resources.Length == 1 ? 1
                            : throw new InvalidDataException(
                                $"A {ItemIdentity.ClassName(type.Path)} spot in {place.Name} gives several resources without a table to draw them from."
                            )
                    );
            }
        }
    }

    /// <summary>
    /// The chance a spot is each resource it can be: 1 for a named preset, and for a spot that draws its preset from a
    /// table the preset's weight over the table's sum, as `Choose Preset` draws (run presets2).
    /// </summary>
    private static Dictionary<string, double> Shares(CatalogContext context, JsonObject properties)
    {
        var shares = new Dictionary<string, double>(StringComparer.Ordinal);
        if (RecordValues.Boolean(properties["Custom Preset"]) != true)
            return shares;
        if (RecordValues.Boolean(properties["Table Based Preset?"]) != true)
        {
            if (RecordValues.String(properties["Custom Preset Name"]) is { } name)
                shares[PresetName(Preset(context, name))] = 1;
            return shares;
        }
        var entries = RecordValues
            .MapEntries(properties["Custom Preset Table"])
            .Select(entry =>
                (
                    Name: RecordValues.String(entry.Key),
                    Weight: RecordValues.Number(entry.Value) ?? 0
                )
            )
            .Where(entry => entry.Name is not null && entry.Weight > 0)
            .ToArray();
        var total = entries.Sum(entry => entry.Weight);
        foreach (var (name, weight) in entries)
        {
            var resource = PresetName(Preset(context, name!));
            shares[resource] = shares.GetValueOrDefault(resource) + weight / total;
        }
        return shares;
    }

    /// <summary>
    /// The custom presets each level can hold in play, placed or spawned, by level path, which the harvest check
    /// compares with the harvestables a census run found.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlySet<string>> PresetsByLevel(
        CatalogContext context
    )
    {
        var harvestable = context.Classes.Values.SingleOrDefault(type =>
            ItemIdentity.ClassName(type.Path) == "HarvestableInteractionObject"
        );
        if (harvestable is null)
            return new Dictionary<string, IReadOnlySet<string>>();
        return context
            .Placements.Where(placement =>
                !context.IsExcludedClass(placement.Actor.Class)
                && WorldSourceData.IsClass(
                    context,
                    placement.Actor.Class,
                    "HarvestableInteractionObject_C"
                )
            )
            .Select(placement => (placement.Properties, placement.Level))
            .Concat(
                SpawnedHarvestables(context, harvestable)
                    .Select(spot => (spot.Properties, spot.Level))
            )
            .GroupBy(spot => spot.Level, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group =>
                    (IReadOnlySet<string>)
                        group
                            .SelectMany(spot => PresetNames(spot.Properties).OfType<string>())
                            .ToHashSet(StringComparer.Ordinal),
                StringComparer.OrdinalIgnoreCase
            );
    }

    /// <summary>
    /// The harvestables that spawners place in play, each as the harvestable class's defaults with the preset the
    /// spawner gives it, and the level of its spawner. A spawner box draws each harvestable's preset from the General
    /// Mushroom Spawn Table or General Ore Spawn Table when its flag is set, and from its own Custom Preset Table
    /// otherwise, as every box in run census2 held once it had spawned. Boxes that do not spawn on their own spawn when
    /// the level, a cave or an event calls their Spawn. An open-world populator with an Ore (Iron) Count spawns that
    /// many Iron deposits (run outcrops1), and its tree, stone and copper counts spawned nothing in the runs, so they give
    /// no harvestables here. A dungeon populator spawns outcrops from its Ore Chances when its Outcrop Spawn Chances is
    /// above zero, and trees from its Tree Chances when its Tree Spawn Chances is, each taking the preset its type names
    /// first, such as Iron for Iron Deposit.
    /// </summary>
    private static IEnumerable<(
        JsonObject Properties,
        string Level,
        double? Count,
        int? Most
    )> SpawnedHarvestables(CatalogContext context, ClassRecord harvestable)
    {
        foreach (var placement in context.Placements)
        {
            if (context.IsExcludedClass(placement.Actor.Class))
                continue;
            JsonObject? spawned = null;
            double? count = null;
            int? most = null;
            if (
                WorldSourceData.IsClass(
                    context,
                    placement.Actor.Class,
                    "Harvestable_Item_Spawner_Box_C"
                )
            )
            {
                var properties = placement.Properties;
                var table =
                    RecordValues.Boolean(properties["General Mushroom Spawn Rates"]) == true
                        ? properties["General Mushroom Spawn Table"]
                    : RecordValues.Boolean(properties["General Ore Spawn Rates"]) == true
                        ? properties["General Ore Spawn Table"]
                    : properties["Custom Preset Table"];
                spawned = new JsonObject
                {
                    ["Custom Preset"] = true,
                    ["Table Based Preset?"] = true,
                    ["Custom Preset Table"] = table?.DeepClone(),
                };
                // Spawn draws its amount with RandomIntegerInRange from Min to Max Spawn Count, as 617 boxes in run
                // census3 held between the two, on average halfway.
                var defaults = context.Classes.GetValueOrDefault(placement.Actor.Class)?.Properties;
                var min =
                    RecordValues.Number(properties["Min Spawn Count"])
                    ?? RecordValues.Number(defaults?["Min Spawn Count"])
                    ?? 0;
                var max =
                    RecordValues.Number(properties["Max Spawn Count"])
                    ?? RecordValues.Number(defaults?["Max Spawn Count"])
                    ?? 0;
                count = (min + max) / 2;
                most = (int)max;
            }
            else if (
                WorldSourceData.IsClass(context, placement.Actor.Class, "OpenWorld_Populator_C")
                && RecordValues.Number(placement.Properties["Ore (Iron) Count"]) is > 0
            )
            {
                spawned = new JsonObject
                {
                    ["Custom Preset"] = true,
                    ["Table Based Preset?"] = false,
                    ["Custom Preset Name"] = "Iron",
                };
                count = RecordValues.Number(placement.Properties["Ore (Iron) Count"]);
                most = (int?)count;
            }
            if (
                WorldSourceData.IsClass(
                    context,
                    placement.Actor.Class,
                    "RepeatableDungeonPopulator_C"
                )
            )
            {
                foreach (
                    var (chance, table) in new[]
                    {
                        ("Outcrop Spawn Chances", "Ore Chances"),
                        ("Tree Spawn Chances", "Tree Chances"),
                    }
                )
                {
                    if (RecordValues.Number(placement.Properties[chance]) is not > 0)
                        continue;
                    // An outcrop takes the preset its ore type names first, as Iron Deposit spawned Iron (run census3).
                    var presets = new JsonArray([
                        .. RecordValues
                            .MapEntries(placement.Properties[table])
                            .Where(entry => RecordValues.Number(entry.Value) is > 0)
                            .Select(entry => new JsonArray(
                                (
                                    entry.Key?[RecordFormat.EnumDisplay]?.GetValue<string>() ?? ""
                                ).Split(' ')[0],
                                entry.Value?.DeepClone()
                            )),
                    ]);
                    if (presets.Count == 0)
                        continue;
                    yield return (
                        RecordMerge.OverlayProperties(
                            harvestable.Properties,
                            new JsonObject
                            {
                                ["Custom Preset"] = true,
                                ["Table Based Preset?"] = true,
                                ["Custom Preset Table"] = new JsonObject
                                {
                                    [RecordFormat.Map] = presets,
                                },
                            }
                        ),
                        placement.Level,
                        null,
                        null
                    );
                }
            }
            if (spawned is not null)
            {
                yield return (
                    RecordMerge.OverlayProperties(harvestable.Properties, spawned),
                    placement.Level,
                    count,
                    most
                );
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
            // A custom preset's loot comes from harvest-presets.json, not from a pool the records name.
            return null;
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

    private static IEnumerable<EntitySource> Sources(
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
        if (harvest && RecordValues.Boolean(properties["Custom Preset"]) == true)
        {
            foreach (var source in PresetSources(context, path, properties, places, placed))
            {
                yield return source;
            }
            yield break;
        }
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
                yield return new EntitySource(
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
                yield return new EntitySource(
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
                                "Whether this gathering method is active is unknown"
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

    /// <summary>
    /// The loot of a custom preset, from harvest-presets.json. A placement whose preset a table draws can become any
    /// preset of the table in play, so it gives the loot of each, with a note that not every spot is that one.
    /// </summary>
    private static IEnumerable<EntitySource> PresetSources(
        CatalogContext context,
        string path,
        JsonObject properties,
        IReadOnlyList<EntityReference> places,
        bool placed
    )
    {
        var drawn = RecordValues.Boolean(properties["Table Based Preset?"]) == true;
        foreach (var name in PresetNames(properties).OfType<string>())
        {
            var preset = Preset(context, name);
            var sourceName = PresetName(preset);
            var notes = HarvestNotes(properties).ToList();
            if (!placed)
            {
                notes.Add("No gathering spot in the world is known to give it.");
            }
            if (drawn)
            {
                notes.Add(
                    "The game chooses at random what each of these spots is, so only some of them give it."
                );
            }
            var total = preset.Loot.Sum(loot => loot.Weight);
            foreach (var loot in preset.Loot)
            {
                yield return new EntitySource(
                    loot.Item,
                    new Source(SourceKind.Gathering, sourceName, places, null, null, [], notes)
                    {
                        Unconfirmed =
                            !placed
                            || RecordValues.Number(
                                properties["Maximum amount of items that can be harvested"]
                            )
                                is <= 0,
                        Entity = WorldSourceData.Reference(EntityKind.Resource, path, sourceName),
                        Conditions = HarvestConditions(
                            context,
                            properties,
                            places.Count == 1 ? places[0].Key : null,
                            preset
                        ),
                        // A gather picks once, and gathering bonuses can add picks.
                        Odds =
                            total > 0
                                ? LootOdds.Of(
                                    OddsEvent.Pick,
                                    (double)(loot.Weight / total),
                                    PickCount.Fixed(1)
                                )
                                : null,
                    }
                );
            }
        }
    }

    /// <summary>
    /// The presets a custom-preset placement can become: its name, or each name of the table it draws from with a
    /// weight above 0. A missing name is null, so the check can report it.
    /// </summary>
    private static IEnumerable<string?> PresetNames(JsonObject properties) =>
        RecordValues.Boolean(properties["Custom Preset"]) != true ? []
        : RecordValues.Boolean(properties["Table Based Preset?"]) == true
            ? RecordValues
                .MapEntries(properties["Custom Preset Table"])
                // A preset of weight 0 is never drawn, as a Scorched Coast box lists Aloe beside Golden Barrel Cactus.
                .Where(entry => RecordValues.Number(entry.Value) is not <= 0)
                .Select(entry => RecordValues.String(entry.Key))
                .Distinct(StringComparer.Ordinal)
        : [RecordValues.String(properties["Custom Preset Name"])];

    private static HarvestPreset Preset(CatalogContext context, string name) =>
        context.Records.HarvestPresets.Presets.First(preset => preset.Preset == name);

    /// <summary>Whether a preset gives each item of a pool with the same chance, and nothing else.</summary>
    private static bool SameLoot(
        HarvestPreset preset,
        IEnumerable<(string Item, Weight Weight)> pool
    )
    {
        var total = preset.Loot.Sum(loot => loot.Weight);
        var chances = pool.ToDictionary(
            entry => entry.Item,
            entry => WeightTotals.Chance(entry.Weight),
            StringComparer.OrdinalIgnoreCase
        );
        return total > 0
            && chances.Count == preset.Loot.Count
            && preset.Loot.All(loot =>
                chances.TryGetValue(loot.Item, out var chance)
                && chance is { } value
                && Math.Abs(value - (double)(loot.Weight / total)) < 1e-6
            );
    }

    /// <summary>A tree for a Woodcutting preset and a deposit for a Mining one, as the game's own pools name Pine Tree and Iron Deposit.</summary>
    internal static string PresetName(HarvestPreset preset) =>
        preset.Checks switch
        {
            "Woodcutting" => preset.Preset + " Tree",
            "Mining" => preset.Preset + " Deposit",
            _ => preset.Preset,
        };

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
        string? levelPath,
        HarvestPreset? preset = null
    )
    {
        var conditions = WorldSourceData.Conditions(context, properties, levelPath);
        // Check Player Level sets the required level by the preset name and ignores the level a placement records.
        // Every character is at least level 1, so that requirement restricts nothing.
        return preset is { Level: > 1 }
            ?
            [
                .. conditions,
                new RelationCondition(
                    ConditionKind.GameState,
                    null,
                    preset.Level,
                    FormattableString.Invariant($"Requires {preset.Checks} level {preset.Level}")
                ),
            ]
            : conditions;
    }

    /// <summary>
    /// Fails when the harvestable class's level lists and the measured presets name different presets, or a placement
    /// names or draws a preset that was not measured, because a game update that adds or moves a preset needs a new
    /// run.
    /// </summary>
    public static void CheckPresets(CatalogContext context)
    {
        var type = context.Classes.Values.SingleOrDefault(type =>
            ItemIdentity.ClassName(type.Path) == "HarvestableInteractionObject"
        );
        if (type is null)
            return;
        var listed = HarvestPresets
            .Lists.SelectMany(list =>
                RecordValues
                    .Elements(type.Properties[list.Key])
                    .Select(RecordValues.String)
                    .OfType<string>()
                    .Select(name => (Preset: name, Checks: list.Value))
            )
            .ToHashSet();
        var measured = context
            .Records.HarvestPresets.Presets.Select(preset => (preset.Preset, preset.Checks))
            .ToHashSet();
        var incomplete = context
            .Records.HarvestPresets.Presets.Where(preset =>
                preset.Experience <= 0
                || preset.Bonus is null
                || (preset.Checks != "Foraging" && preset.Bonus.Count == 0)
            )
            .Select(preset => preset.Preset)
            .ToArray();
        if (incomplete.Length > 0)
            throw new InvalidDataException(
                "harvest-presets.json lacks the experience or bonus chances of "
                    + string.Join(", ", incomplete)
                    + ". Run tools/runtime/checks/harvest-yield.lua."
            );
        if (!listed.SetEquals(measured))
            throw new InvalidDataException(
                "harvest-presets.json and HarvestableInteractionObject's level lists differ: "
                    + string.Join(
                        ", ",
                        listed
                            .Except(measured)
                            .Select(entry => $"{entry.Preset} ({entry.Checks}) unmeasured")
                            .Concat(
                                measured
                                    .Except(listed)
                                    .Select(entry => $"{entry.Preset} ({entry.Checks}) not listed")
                            )
                    )
            );
        var names = measured.Select(entry => entry.Preset).ToHashSet(StringComparer.Ordinal);
        var unmeasured = context
            .Placements.Where(placement =>
                !context.IsExcludedClass(placement.Actor.Class)
                && WorldSourceData.IsClass(
                    context,
                    placement.Actor.Class,
                    "HarvestableInteractionObject_C"
                )
            )
            .Select(placement => placement.Properties)
            .Concat(SpawnedHarvestables(context, type).Select(spot => spot.Properties))
            .SelectMany(PresetNames)
            .Where(name => name is null || !names.Contains(name))
            .Select(name => name ?? "a preset without a name")
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (unmeasured.Length > 0)
            throw new InvalidDataException(
                "Placed harvestables name presets that harvest-presets.json does not measure: "
                    + string.Join(", ", unmeasured)
            );
    }

    private static IEnumerable<EntitySource> BespokeSources(CatalogContext context)
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
                EntitySource? Create(JsonObject properties, IReadOnlyList<EntityReference> places)
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
                    return new EntitySource(
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
