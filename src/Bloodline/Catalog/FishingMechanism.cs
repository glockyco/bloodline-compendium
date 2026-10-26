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
            notes.Add("Each successful catch adds one item. Fish size is determined separately.");
            if (RecordValues.Boolean(properties["Use Specific Loot table?"]) == true)
            {
                foreach (
                    var source in Sources(
                        properties["Specific Loot Table"],
                        "Fishing spot",
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
                            "Lava catches are listed, but whether they are available at this spot is unknown.",
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
                        SourceName(pool),
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
                        SourceName(pool),
                        [],
                        [
                            "Possible fishing catches. Availability depends on the spot being active and its difficulty.",
                        ]
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
