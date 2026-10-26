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
            var notes = WorldSourceData
                .Notes(properties, "Min Difficulty", "Max Difficulty", "Remaining Fish")
                .ToList();
            notes.Add(
                "One inventory addition per successful catch. Fish size is generated separately."
            );
            if (RecordValues.Boolean(properties["Use Specific Loot table?"]) == true)
            {
                foreach (
                    var source in Sources(
                        properties["Specific Loot Table"],
                        "Specific fishing pool",
                        [context.Places.Get(spot.Level)],
                        notes
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
                            "The data defines a lava pool. Its runtime pool assignment is unresolved.",
                        ]
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
                        pool,
                        [context.Places.Get(spot.Level)],
                        notes
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
                        pool,
                        [],
                        [
                            "Fishing pool defined by the minigame. Spot activation and difficulty determine availability.",
                        ]
                    )
                )
                {
                    yield return source;
                }
            }
        }
    }

    private static IEnumerable<ItemSource> Sources(
        JsonNode? pool,
        string name,
        IReadOnlyList<Place> places,
        IReadOnlyList<string> notes
    )
    {
        foreach (var (item, weight) in WorldSourceData.Pool(pool))
        {
            yield return new ItemSource(
                item,
                new Source(SourceKind.Fishing, name, places, null, null, weight, null, [], notes)
            );
        }
    }
}
