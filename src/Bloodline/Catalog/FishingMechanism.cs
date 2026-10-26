using System.Text.Json.Nodes;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed class FishingMechanism : ISourceMechanism
{
    private static readonly string[] DifficultyPools =
    [
        "Beginner Loot Table",
        "Easy Loot Table",
        "Medium Loot Table",
        "Hard Loot Table",
        "Expert Loot Table",
    ];

    public IEnumerable<ItemSource> FindSources(CatalogContext context)
    {
        var defaults = context.Classes.Values.FirstOrDefault(type =>
            ItemIdentity.ClassName(type.Path) == "Fishing_Minigame"
        );
        var spots = context
            .Placements.Where(placement =>
                WorldSourceData.IsClass(context, placement.Actor.Class, "FishingSpot_C")
            )
            .ToArray();
        var placedPools = new HashSet<string>(StringComparer.Ordinal);
        foreach (var spot in spots)
        {
            var properties = spot.Properties;
            if (WorldSourceData.Integer(properties["Remaining Fish"]) is <= 0)
            {
                continue;
            }
            var notes = new List<string>();
            if (RecordValues.Number(properties["Min Difficulty"]) is { } minimum)
            {
                notes.Add(FormattableString.Invariant($"Minimum fishing difficulty: {minimum}."));
            }
            if (RecordValues.Number(properties["Max Difficulty"]) is { } maximum)
            {
                notes.Add(FormattableString.Invariant($"Maximum fishing difficulty: {maximum}."));
            }
            if (RecordValues.Number(properties["Remaining Fish"]) is { } remaining)
            {
                notes.Add(
                    FormattableString.Invariant($"Fish remaining at this spot: {remaining}.")
                );
            }
            notes.Add(
                "Each successful catch makes one loot roll. Fish size is determined separately."
            );
            if (RecordValues.Boolean(properties["Use Specific Loot table?"]) == true)
            {
                foreach (
                    var source in Sources(
                        properties["Specific Loot Table"],
                        "Fishing spot",
                        [context.Places.Get(spot.Level)],
                        notes,
                        WorldSourceData.Reference(EntityKind.Activity, spot.Actor.Class, "Fishing"),
                        WorldSourceData.Conditions(context, properties, spot.Level)
                    )
                )
                {
                    yield return source;
                }
                continue;
            }
            if (defaults is null)
            {
                continue;
            }
            if (RecordValues.Boolean(properties["Lava Fish"]) == true)
            {
                placedPools.Add("Standard Lava Loot");
                foreach (
                    var source in Sources(
                        defaults.Properties["Standard Lava Loot"],
                        "Lava fishing",
                        [context.Places.Get(spot.Level)],
                        [
                            .. notes,
                            "Lava catches are listed, but whether they are available at this spot is unknown.",
                        ],
                        WorldSourceData.Reference(EntityKind.Activity, spot.Actor.Class, "Fishing"),
                        [
                            .. WorldSourceData.Conditions(context, properties, spot.Level),
                            new RelationCondition(
                                ConditionKind.Unknown,
                                null,
                                null,
                                "Lava catch availability is unknown."
                            ),
                        ],
                        confirmedChoice: false
                    )
                )
                {
                    yield return source;
                }
                continue;
            }
            var min = WorldSourceData.Integer(properties["Min Difficulty"]);
            var max = WorldSourceData.Integer(properties["Max Difficulty"]);
            for (var difficulty = 1; difficulty <= DifficultyPools.Length; difficulty++)
            {
                if (difficulty < min || difficulty > max)
                {
                    continue;
                }
                var pool = DifficultyPools[difficulty - 1];
                placedPools.Add(pool);
                foreach (
                    var source in Sources(
                        defaults.Properties[pool],
                        SourceName(pool),
                        [context.Places.Get(spot.Level)],
                        notes,
                        WorldSourceData.Reference(EntityKind.Activity, spot.Actor.Class, "Fishing"),
                        WorldSourceData.Conditions(context, properties, spot.Level)
                    )
                )
                {
                    yield return source;
                }
            }
        }
        if (defaults is not null)
        {
            // Unplaced authored pools remain visible without inventing a location.
            foreach (var pool in DifficultyPools.Append("Standard Lava Loot"))
            {
                if (placedPools.Contains(pool))
                {
                    continue;
                }
                foreach (
                    var source in Sources(
                        defaults.Properties[pool],
                        SourceName(pool),
                        [],
                        [
                            "Possible fishing catches. Availability depends on the spot being active and its difficulty.",
                        ],
                        WorldSourceData.Reference(EntityKind.Activity, defaults.Path, "Fishing"),
                        [
                            new RelationCondition(
                                ConditionKind.GameState,
                                null,
                                null,
                                "Requires an active fishing spot with the matching difficulty."
                            ),
                        ],
                        confirmedChoice: pool != "Standard Lava Loot"
                    )
                )
                {
                    yield return source;
                }
            }
        }
    }

    private static string SourceName(string difficulty) =>
        difficulty switch
        {
            "Beginner Loot Table" => "Fishing, beginner difficulty",
            "Easy Loot Table" => "Fishing, easy difficulty",
            "Medium Loot Table" => "Fishing, medium difficulty",
            "Hard Loot Table" => "Fishing, hard difficulty",
            "Expert Loot Table" => "Fishing, expert difficulty",
            _ => "Lava fishing",
        };

    private static IEnumerable<ItemSource> Sources(
        JsonNode? pool,
        string name,
        IReadOnlyList<EntityReference> places,
        IReadOnlyList<string> notes,
        EntityReference entity,
        IReadOnlyList<RelationCondition> conditions,
        bool confirmedChoice = true
    )
    {
        foreach (var (item, weight) in WorldSourceData.Pool(pool))
        {
            var chance = WeightTotals.Chance(weight);
            yield return new ItemSource(
                item,
                new Source(SourceKind.Fishing, name, places, null, null, [], notes)
                {
                    Entity = entity,
                    Conditions = conditions,
                    // Fishing_Minigame:Catch Fish picks once from the pool for each fish caught.
                    Odds =
                        confirmedChoice && chance is { } perCatch
                            ? LootOdds.Of(OddsEvent.Catch, perCatch, PickCount.Fixed(1))
                            : LootOdds.Unknown(
                                OddsEvent.Catch,
                                "How lava fishing chooses its catch is unknown."
                            ),
                }
            );
        }
    }
}
