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
    // A container picks while its counter, starting at zero, is at most a bound drawn again on every pass. The
    // loose-loot placer's loop starts at one and stops after its maximum.
    public PickCount? Picks =>
        LootLimits is not { } limits ? null
        : Loose ? PickCount.Fixed(Math.Max(0, limits.Max))
        : limits.Min >= 0 && limits.Max >= limits.Min && limits.Max < int.MaxValue
            ? PickCount.Container(limits.Min, limits.Max)
        : null;

    public IReadOnlyList<string> Notes => Describe([this]);

    public static IReadOnlyList<string> Describe(IReadOnlyList<ContainerDetails> variants)
    {
        var notes = new List<string>();
        var containers = variants.Where(variant => !variant.Loose).ToArray();
        // Only classes that declare Spawn Chance roll it in their BeginPlay, so a container without one always
        // appears.
        var chances = containers
            .Select(variant => variant.AppearanceChance ?? 1)
            .Distinct()
            .Order()
            .ToArray();
        if (chances.Length > 0 && chances[0] < 1)
        {
            var minimum = Percent(chances[0]);
            var maximum = Percent(chances[^1]);
            notes.Add(
                chances.Length > 1
                    ? $"The container is there only {minimum}% to {maximum}% of the time, depending on the container."
                    : $"The container is there only {minimum}% of the time."
            );
        }
        if (variants.Any(variant => variant.Loose))
        {
            notes.Add("Lies loose on the ground instead of in a container.");
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
                new { candidate.ItemClass, Source = source with { Places = [], Notes = [] } }
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
            yield return group[0].Source with
            {
                Places = group
                    .SelectMany(candidate => candidate.Source.Places)
                    .DistinctBy(place => place.Key, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                Notes = notes,
            };
        }
    }
}
