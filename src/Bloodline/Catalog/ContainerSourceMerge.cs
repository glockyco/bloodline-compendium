using System.Globalization;
using Bloodline.Output;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

// Acquisition data stays separate from prose so count and appearance ranges never depend on parsing a note.
internal sealed record ContainerDetails(
    double? AppearanceChance,
    Amount? LootLimits,
    bool Loose = false
)
{
    // The container loop starts at zero and tests an inclusive, freshly rolled limit.
    public Amount? Picks =>
        LootLimits is not { } limits ? null
        : Loose ? new Amount(Math.Max(0, limits.Max), Math.Max(0, limits.Max))
        : limits.Min >= 0 && limits.Max >= limits.Min && limits.Max < int.MaxValue
            ? new Amount(limits.Min + 1, limits.Max + 1)
        : null;

    public IReadOnlyList<string> Notes => Describe([this]);

    public static IReadOnlyList<string> Describe(IReadOnlyList<ContainerDetails> variants)
    {
        var notes = new List<string>();
        var containers = variants.Where(variant => !variant.Loose).ToArray();
        var limits = containers
            .Select(variant => variant.Picks)
            .OfType<Amount>()
            .Distinct()
            .ToArray();
        if (limits.Length > 0)
        {
            var minimum = limits.Min(limit => limit.Min);
            var maximum = limits.Max(limit => limit.Max);
            notes.Add(
                minimum == maximum ? $"Makes {maximum} loot picks. Picks can find nothing."
                : limits.Length == 1
                    ? $"Makes {minimum} to {maximum} loot picks. Picks can find nothing."
                : $"Makes {minimum} to {maximum} loot picks, depending on the container. Picks can find nothing."
            );
        }
        if (containers.Any(variant => variant.Picks is null))
        {
            notes.Add("The number of loot picks some containers make is unknown.");
        }
        var chances = containers
            .Select(variant => variant.AppearanceChance)
            .OfType<double>()
            .Distinct()
            .Order()
            .ToArray();
        if (chances.Length > 0)
        {
            var minimum = Percent(chances[0]);
            var maximum = Percent(chances[^1]);
            notes.Add(
                chances.Length > 1
                    ? $"Appears {minimum}% to {maximum}% of the time, depending on the container."
                : chances[0] == 1 ? "Always appears."
                : $"Appears {minimum}% of the time."
            );
        }
        if (containers.Any(variant => variant.AppearanceChance is null))
        {
            notes.Add("The appearance chance of some containers is unknown.");
        }
        var loose = variants.Where(variant => variant.Loose).ToArray();
        var looseLimits = loose
            .Select(variant => variant.Picks)
            .OfType<Amount>()
            .Distinct()
            .ToArray();
        if (looseLimits.Length > 0)
        {
            var minimum = looseLimits.Min(limit => limit.Min);
            var maximum = looseLimits.Max(limit => limit.Max);
            notes.Add(
                minimum == maximum
                    ? $"Makes {maximum} loose loot picks. Empty outcomes do not produce an item."
                    : $"Makes {minimum} to {maximum} loose loot picks, depending on the source. Empty outcomes do not produce an item."
            );
        }
        if (loose.Any(variant => variant.LootLimits is null))
        {
            notes.Add("The number of loose items at some sources is unknown.");
        }
        return notes;
    }

    private static string Percent(double chance) =>
        (chance * 100).ToString("G", CultureInfo.InvariantCulture);
}

internal static class ContainerSourceMerge
{
    public static IEnumerable<Source> Merge(
        IEnumerable<ItemSource> candidates,
        IReadOnlySet<string> excludedLevels
    )
    {
        var groups = new Dictionary<string, List<ItemSource>>(StringComparer.Ordinal);
        foreach (var candidate in candidates)
        {
            var source = candidate.Source;
            var places = source
                .Places.Where(place => place.Key is null || !excludedLevels.Contains(place.Key))
                .ToArray();
            if (source.Places.Count > 0 && places.Length == 0)
            {
                continue;
            }
            source = source with { Places = places };
            if (source.Kind != SourceKind.ContainerLoot)
            {
                yield return source;
                continue;
            }
            var key = CanonicalJson.Serialize(
                new
                {
                    candidate.ItemClass,
                    Source = source with
                    {
                        Places = [],
                        Notes = [],
                        Picks = null,
                    },
                }
            );
            if (!groups.TryGetValue(key, out var group))
            {
                group = [];
                groups.Add(key, group);
            }
            group.Add(candidate with { Source = source });
        }
        foreach (
            var group in groups
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => pair.Value)
        )
        {
            var details = group
                .Select(candidate => candidate.Container)
                .OfType<ContainerDetails>()
                .ToArray();
            var variableNotes = details
                .SelectMany(detail => detail.Notes)
                .ToHashSet(StringComparer.Ordinal);
            var notes = group
                .SelectMany(candidate => candidate.Source.Notes)
                .Where(note => !variableNotes.Contains(note))
                .Distinct(StringComparer.Ordinal)
                .Select(note =>
                    group.All(candidate =>
                        candidate.Source.Notes.Contains(note, StringComparer.Ordinal)
                    )
                        ? note
                        : "Some containers: " + note
                )
                .ToList();
            notes.AddRange(ContainerDetails.Describe(details));
            if (
                group.Any(candidate => candidate.Source.Places.Count == 0)
                && group.Any(candidate => candidate.Source.Places.Count > 0)
            )
            {
                notes.Add("Some containers have no known location.");
            }
            var picks = group
                .Select(candidate => candidate.Source.Picks)
                .OfType<Amount>()
                .ToArray();
            yield return group[0].Source with
            {
                Places = group
                    .SelectMany(candidate => candidate.Source.Places)
                    .DistinctBy(place => place.Key, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                Notes = notes,
                Picks =
                    picks.Length == group.Count
                        ? new Amount(picks.Min(value => value.Min), picks.Max(value => value.Max))
                        : null,
            };
        }
    }
}
