using Bloodline.SiteData;

namespace Bloodline.Catalog;

/// <summary>How many picks an event makes, as the probability of each count.</summary>
internal sealed record PickCount(IReadOnlyList<(int Picks, double Probability)> Counts)
{
    public static PickCount Fixed(int picks) => new([(picks, 1)]);

    /// <summary>One pick that happens with <paramref name="chance"/>, and none otherwise.</summary>
    public static PickCount Sometimes(double chance) => new([(1, chance)]);

    /// <summary>
    /// LootableObject:Generate New Loot picks while its counter, starting at 0, is at most a bound it draws again
    /// between the minimum and maximum on every pass. Pass i runs when the draw is at least i, so the count is
    /// at least n with the product of those chances for the passes 0 to n - 1.
    /// </summary>
    public static PickCount Container(int minimum, int maximum)
    {
        var span = maximum - minimum + 1;
        double AtLeast(int pass) =>
            pass <= minimum ? 1
            : pass > maximum ? 0
            : (double)(maximum - pass + 1) / span;
        var counts = new List<(int, double)>();
        var reach = 1.0;
        for (var picks = 1; picks <= maximum + 1; picks++)
        {
            reach *= AtLeast(picks - 1);
            var next = reach * AtLeast(picks);
            if (reach - next > 0)
                counts.Add((picks, reach - next));
        }
        return new(counts);
    }
}

internal static class LootOdds
{
    /// <summary>
    /// The odds of at least one copy across the event's picks, optionally across two bounds for the share per pick.
    /// </summary>
    public static Odds Of(
        OddsEvent oddsEvent,
        double perPick,
        PickCount picks,
        double? highestPerPick = null
    )
    {
        var chance = 0.0;
        var highest = 0.0;
        var expectedPicks = 0.0;
        foreach (var (count, probability) in picks.Counts)
        {
            chance += probability * AtLeastOnce(perPick, count);
            expectedPicks += probability * count;
            if (highestPerPick is { } upper)
                highest += probability * AtLeastOnce(upper, count);
        }
        return new Odds(oddsEvent, chance, expectedPicks * perPick, null)
        {
            HighestChance = highest > chance ? highest : null,
        };
    }

    // One pick keeps the pool share exact instead of rounding it through 1 - (1 - p).
    private static double AtLeastOnce(double perPick, int picks) =>
        picks == 1 ? perPick : 1 - Math.Pow(1 - perPick, picks);

    public static Odds Unknown(OddsEvent oddsEvent, string reason) =>
        new(oddsEvent, null, null, reason);
}
