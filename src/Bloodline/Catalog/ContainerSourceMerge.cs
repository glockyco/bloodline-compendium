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

    public IReadOnlyList<string> Notes =>
        Loose ? ["Lies loose on the ground instead of in a container."] : [];
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
            // Ranges describe the containers at one place, not variants in unrelated places.
            foreach (var place in places.Length == 0 ? new EntityReference?[] { null } : places)
            {
                var located = source with { Places = place is null ? [] : [place] };
                var key = CanonicalJson.Serialize(
                    new
                    {
                        candidate.ItemClass,
                        Loose = candidate.Container?.Loose ?? false,
                        Source = located with
                        {
                            Entity = located.Entity with { Key = null },
                            Odds = null,
                            Spawn = null,
                            Amount = null,
                            Notes = [],
                        },
                    }
                );
                if (!groups.TryGetValue(key, out var group))
                {
                    group = [];
                    groups.Add(key, group);
                }
                group.Add(candidate with { Source = located });
            }
        }
        var merged = groups
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => MergePlace(pair.Value));
        foreach (
            var group in merged.GroupBy(candidate =>
                CanonicalJson.Serialize(
                    new
                    {
                        candidate.ItemClass,
                        Loose = candidate.Container?.Loose ?? false,
                        Source = candidate.Source with
                        {
                            Entity = candidate.Source.Entity with { Key = null },
                            Places = [],
                            Notes = [],
                        },
                    }
                )
            )
        )
        {
            var variants = group.ToArray();
            var notes = MergeNotes(variants);
            if (
                variants.Any(candidate => candidate.Source.Places.Count == 0)
                && variants.Any(candidate => candidate.Source.Places.Count > 0)
            )
            {
                notes.Add("Some containers have no known location.");
            }
            yield return variants[0].Source with
            {
                Entity = variants
                    .Select(candidate => candidate.Source.Entity)
                    .OrderBy(entity => entity.Key, StringComparer.Ordinal)
                    .First(),
                Places = variants
                    .SelectMany(candidate => candidate.Source.Places)
                    .DistinctBy(place => place.Key, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                Notes = notes,
            };
        }
    }

    private static ItemSource MergePlace(IReadOnlyList<ItemSource> group)
    {
        // Same-named containers are indistinguishable in play, even when their Blueprint classes differ.
        var first = group.MinBy(candidate => candidate.Source.Entity.Key, StringComparer.Ordinal)!;
        var amounts = group.Select(candidate => candidate.Source.Amount).ToArray();
        var chances = group
            .Select(candidate => candidate.Container?.AppearanceChance ?? 1)
            .ToArray();
        var minimumSpawn = chances.Min();
        return first with
        {
            Source = first.Source with
            {
                Amount = amounts.Any(amount => amount is null)
                    ? null
                    : new Amount(
                        amounts.Min(amount => amount!.Min),
                        amounts.Max(amount => amount!.Max)
                    ),
                Odds = MergeOdds(group),
                Spawn =
                    first.Container?.Loose != true && minimumSpawn < 1
                        ? new ChanceRange(minimumSpawn, chances.Max())
                        : null,
                Notes = MergeNotes(group),
            },
        };
    }

    private static Odds? MergeOdds(IReadOnlyList<ItemSource> group)
    {
        var odds = group.Select(candidate => candidate.Source.Odds).ToArray();
        if (odds.All(value => value is null))
        {
            return null;
        }
        var first = odds.First(value => value is not null)!;
        var unknown = odds.Any(value => value?.Chance is null);
        var minimum = unknown ? (double?)null : odds.Min(value => value!.Chance!.Value);
        var maximum = unknown
            ? (double?)null
            : odds.Max(value => value!.HighestChance ?? value.Chance!.Value);
        var reasons = odds.Select(value => value?.Unknown)
            .OfType<string>()
            .Distinct(StringComparer.Ordinal);
        return first with
        {
            Chance = minimum,
            HighestChance = maximum > minimum ? maximum : null,
            Expected = odds.Max(value => value?.Expected),
            Unknown = unknown ? string.Join(" ", reasons) : null,
        };
    }

    private static List<string> MergeNotes(IReadOnlyList<ItemSource> group) =>
        group
            .SelectMany(candidate => candidate.Source.Notes)
            .Distinct(StringComparer.Ordinal)
            .Select(note =>
                note.StartsWith("Some containers: ", StringComparison.Ordinal)
                || group.All(candidate =>
                    candidate.Source.Notes.Contains(note, StringComparer.Ordinal)
                )
                    ? note
                    : "Some containers: " + note
            )
            .ToList();
}
