using Bloodline.Output;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed record VersionedItems(
    IReadOnlyList<ItemPage> Items,
    IReadOnlyDictionary<string, EntityReference> References
);

internal static class ItemVersions
{
    public static VersionedItems Group(IReadOnlyList<ItemPage> items)
    {
        var result = new List<ItemPage>(items.Count);
        var references = new Dictionary<string, EntityReference>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in items.GroupBy(item => item.Slug, StringComparer.Ordinal))
        {
            var members = group.ToArray();
            if (members.Length == 1)
            {
                result.Add(members[0]);
                references.Add(members[0].Key, CatalogGraph.Reference(members[0]));
                continue;
            }
            var copies = members
                .OrderBy(item => item.Key, StringComparer.Ordinal)
                .GroupBy(Identity, StringComparer.Ordinal)
                .ToArray();
            var versions = copies.Select(copy => Merge(copy.ToArray())).ToArray();
            string?[] labels = versions.Length > 1 ? Labels(versions) : [null];
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < versions.Length; index++)
            {
                var item = versions[index];
                var version = item with
                {
                    VersionId =
                        versions.Length > 1
                            ? ItemIdentity.ClassName(item.Key).ToLowerInvariant()
                            : null,
                    VersionLabel = labels[index],
                    VersionName =
                        versions.Length > 1
                            ? labels[index] ?? AccessibleName(item, versions, index)
                            : null,
                };
                if (version.VersionId is { } id && !ids.Add(id))
                    throw new InvalidDataException(
                        $"Item {item.Name} has duplicate version ID {id}."
                    );
                result.Add(version);
                foreach (var original in copies[index])
                    references.Add(original.Key, CatalogGraph.Reference(version));
            }
        }
        return new VersionedItems(result, references);
    }

    public static IEnumerable<ItemPage> Pages(IEnumerable<ItemPage> items) =>
        items
            .GroupBy(item => item.Slug, StringComparer.Ordinal)
            .Select(group =>
            {
                var versions = group.OrderBy(item => item.Key, StringComparer.Ordinal).ToArray();
                return versions[0] with { Versions = versions.Skip(1).ToArray() };
            });

    private static string Identity(ItemPage item) =>
        CanonicalJson.Serialize(
            new
            {
                item.Type,
                item.Category,
                item.Icon,
                item.Description,
                item.Facts,
                item.OnHitEffects,
                item.ProjectileGroups,
            }
        );

    private static ItemPage Merge(IReadOnlyList<ItemPage> copies) =>
        copies.Count == 1
            ? copies[0]
            : copies[0] with
            {
                Sources = Union(copies.SelectMany(item => item.Sources)),
                Uses = Union(copies.SelectMany(item => item.Uses)),
                Notes = Union(copies.SelectMany(item => item.Notes)),
                Status =
                    copies.Any(item => item.Status == ItemStatus.Obtainable) ? ItemStatus.Obtainable
                    : copies.Any(item => item.Status == ItemStatus.NotInNormalPlay)
                        ? ItemStatus.NotInNormalPlay
                    : ItemStatus.NoKnownSource,
            };

    private static T[] Union<T>(IEnumerable<T> values) =>
        values.DistinctBy(value => CanonicalJson.Serialize(value)).ToArray();

    private static string?[] Labels(IReadOnlyList<ItemPage> versions)
    {
        bool Distinct(IReadOnlyList<string?> labels) =>
            labels.All(label => !string.IsNullOrWhiteSpace(label) && label != "Unknown")
            && labels.Distinct(StringComparer.Ordinal).Count() == versions.Count;
        var labels = versions.Select(item => (string?)item.Type).ToArray();
        if (Distinct(labels))
            return labels;
        // Facts arrive in the same order as Details. Value is the final fallback.
        foreach (
            var stat in versions
                .SelectMany(item => item.Facts)
                .Select(fact => fact.Stat)
                .Where(stat => stat != Stat.Value)
                .Distinct()
        )
        {
            labels = versions
                .Select(item => StatLabel(item.Facts.FirstOrDefault(fact => fact.Stat == stat)))
                .ToArray();
            if (Distinct(labels))
                return labels;
        }
        if (
            versions.All(item => item.Icon is not null)
            && versions.Select(item => item.Icon).Distinct(StringComparer.Ordinal).Count()
                == versions.Count
        )
            return new string?[versions.Count];
        labels = versions.Select(item => SourceLabel(item, false)).ToArray();
        if (Distinct(labels))
            return labels;
        labels = versions.Select(item => SourceLabel(item, true)).ToArray();
        if (Distinct(labels))
            return labels;
        labels = versions
            .Select(item =>
                item.Facts.FirstOrDefault(fact => fact.Stat == Stat.Value) is { } fact
                && StatVocabulary.Display(fact) is { } value
                    ? $"Worth {value} gold"
                    : null
            )
            .ToArray();
        if (Distinct(labels))
            return labels;
        throw new InvalidDataException(
            $"Item {versions[0].Name} has distinguishable versions without unique version labels."
        );
    }

    private static string? StatLabel(Fact? fact)
    {
        if (fact is null || StatVocabulary.Display(fact) is not { } value)
            return null;
        return fact.Stat switch
        {
            Stat.ArmorType => value == "Unarmored" ? value : value + " Armor",
            Stat.Speed => value + " Speed",
            Stat.DamageContribution => Signed(fact, value) + " Damage",
            Stat.SpeedContribution => Signed(fact, value) + " Speed",
            _ => value + " " + fact.Label,
        };
    }

    private static string AccessibleName(ItemPage item, IReadOnlyList<ItemPage> versions, int index)
    {
        string? Sentence(ItemPage version) =>
            version.Description?.Split(['.', '!', '?'], 2)[0].Trim() is { Length: > 0 } text
                ? text
                : null;
        var description = Sentence(item);
        return
            description is not null
            && versions.Count(version => Sentence(version) == description) == 1
            ? description
            : $"{item.Name}, icon {index + 1}";
    }

    private static string Signed(Fact fact, string value) => fact.Number >= 0 ? "+" + value : value;

    private static string SourceLabel(ItemPage item, bool named)
    {
        var source = item.Sources.FirstOrDefault();
        if (source is null)
            return "No known source";
        return source.Kind switch
        {
            SourceKind.Pickup => source.Places.FirstOrDefault() is { } place
                ? "Picked up in " + place.Name
                : "Picked up",
            SourceKind.Shop or SourceKind.CurrencyShop => "Sold by " + source.Name,
            SourceKind.ContainerLoot => named ? "Found in " + source.Name : "Found in containers",
            SourceKind.RandomLoot => named ? source.Name : "Random loot",
            SourceKind.Dialogue => "Given by " + source.Name,
            SourceKind.BountyNote => "From the bounty " + source.Name,
            SourceKind.Smithing => "Smithed as " + source.Name,
            _ => source.Name,
        };
    }
}
