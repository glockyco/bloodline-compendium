using Bloodline.Output;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

/// <summary>How one kind's entities of a name become versions of one page.</summary>
/// <param name="Kind">The kind whose entities the policy groups.</param>
/// <param name="Shown">What the page shows of an entity. Entities that show the same are copies of one version.</param>
/// <param name="Merge">Joins the sources and other relations of copies into the first.</param>
/// <param name="AsVersion">The entity as a version with its ID, tab label and accessible name.</param>
/// <param name="WithVersions">The page with its other versions, after the version that leads it.</param>
/// <param name="KindLabel">Labels that name versions before their facts do, such as an item's type.</param>
/// <param name="LastLabel">Labels that name versions when nothing else does, such as an item's sale price.</param>
internal sealed record VersionPolicy<T>(
    EntityKind Kind,
    Func<T, object> Shown,
    Func<IReadOnlyList<T>, T> Merge,
    Func<T, string?, string?, string?, T> AsVersion,
    Func<T, IReadOnlyList<T>, T> WithVersions,
    Func<T, string?>? KindLabel = null,
    Func<T, string?>? LastLabel = null
)
    where T : IVersionedPage;

internal sealed record Versioned<T>(
    IReadOnlyList<T> Entities,
    IReadOnlyDictionary<string, EntityReference> References
);

internal static class EntityVersions
{
    /// <summary>
    /// Groups entities by slug. Copies that show the same merge into one version, and a page with several versions
    /// gives each an ID from its key and a label that tells it apart. References map every original key to its
    /// version.
    /// </summary>
    public static Versioned<T> Group<T>(IReadOnlyList<T> entities, VersionPolicy<T> policy)
        where T : IVersionedPage
    {
        var result = new List<T>(entities.Count);
        var references = new Dictionary<string, EntityReference>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in entities.GroupBy(entity => entity.Slug, StringComparer.Ordinal))
        {
            var copies = group
                .OrderBy(entity => entity.Key, StringComparer.Ordinal)
                .GroupBy(
                    entity => CanonicalJson.Serialize(policy.Shown(entity)),
                    StringComparer.Ordinal
                )
                .Select(copy => copy.ToArray())
                .ToArray();
            var versions = copies.Select(copy => policy.Merge(copy)).ToArray();
            string?[] labels = versions.Length > 1 ? Labels(versions, policy) : [null];
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < versions.Length; index++)
            {
                var entity = versions[index];
                var version =
                    versions.Length > 1
                        ? policy.AsVersion(
                            entity,
                            ItemIdentity.ClassName(entity.Key).ToLowerInvariant(),
                            labels[index],
                            labels[index] ?? AccessibleName(entity, versions, index)
                        )
                        : entity;
                if (version.VersionId is { } id && !ids.Add(id))
                    throw new InvalidDataException(
                        $"{policy.Kind} {entity.Name} has duplicate version ID {id}."
                    );
                result.Add(version);
                foreach (var original in copies[index])
                    references.Add(original.Key, Reference(version, policy.Kind));
            }
        }
        return new Versioned<T>(result, references);
    }

    /// <summary>One page per slug, led by the first version in the given order, with the others after it.</summary>
    public static IEnumerable<T> Pages<T>(
        IEnumerable<T> entities,
        VersionPolicy<T> policy,
        Func<T, int>? order = null
    )
        where T : IVersionedPage =>
        entities
            .GroupBy(entity => entity.Slug, StringComparer.Ordinal)
            .Select(group =>
            {
                var versions = group
                    .OrderBy(entity => order?.Invoke(entity) ?? 0)
                    .ThenBy(entity => entity.Key, StringComparer.Ordinal)
                    .ToArray();
                return policy.WithVersions(versions[0], versions.Skip(1).ToArray());
            });

    public static EntityReference Reference(IVersionedPage entity, EntityKind kind) =>
        new(kind, entity.Key, entity.Name, entity.Slug, entity.Icon)
        {
            Fragment = entity.VersionId,
        };

    /// <summary>The status of copies joined: obtainable when any copy is, else not in normal play when any is.</summary>
    public static ItemStatus MergedStatus(IEnumerable<ItemStatus> statuses)
    {
        var all = statuses.ToArray();
        return all.Contains(ItemStatus.Obtainable) ? ItemStatus.Obtainable
            : all.Contains(ItemStatus.NotInNormalPlay) ? ItemStatus.NotInNormalPlay
            : ItemStatus.NoKnownSource;
    }

    public static T[] Union<T>(IEnumerable<T> values) =>
        values.DistinctBy(value => CanonicalJson.Serialize(value)).ToArray();

    private static string?[] Labels<T>(IReadOnlyList<T> versions, VersionPolicy<T> policy)
        where T : IVersionedPage
    {
        bool Distinct(IReadOnlyList<string?> labels) =>
            labels.All(label => !string.IsNullOrWhiteSpace(label) && label != "Unknown")
            && labels.Distinct(StringComparer.Ordinal).Count() == versions.Count;
        string?[] labels;
        if (policy.KindLabel is { } kindLabel)
        {
            labels = versions.Select(kindLabel).ToArray();
            if (Distinct(labels))
                return labels;
        }
        // Facts arrive in the same order as Details. Value is the final fallback.
        foreach (
            var stat in versions
                .SelectMany(entity => entity.Facts)
                .Select(fact => fact.Stat)
                .Where(stat => stat != Stat.Value)
                .Distinct()
        )
        {
            labels = versions
                .Select(entity => StatLabel(entity.Facts.FirstOrDefault(fact => fact.Stat == stat)))
                .ToArray();
            if (Distinct(labels))
                return labels;
        }
        if (
            versions.All(entity => entity.Icon is not null)
            && versions.Select(entity => entity.Icon).Distinct(StringComparer.Ordinal).Count()
                == versions.Count
        )
            return new string?[versions.Count];
        labels = versions.Select(entity => SourceLabel(entity, false)).ToArray();
        if (Distinct(labels))
            return labels;
        labels = versions.Select(entity => SourceLabel(entity, true)).ToArray();
        if (Distinct(labels))
            return labels;
        if (policy.LastLabel is { } lastLabel)
        {
            labels = versions.Select(lastLabel).ToArray();
            if (Distinct(labels))
                return labels;
        }
        throw new InvalidDataException(
            $"{policy.Kind} {versions[0].Name} has distinguishable versions without unique version labels."
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
            // A value with a unit reads as the game's tooltip line, such as "Cooldown: 15 seconds".
            _ when fact.Unit is { } unit => $"{fact.TooltipLabel ?? fact.Label}: {value} {unit}",
            _ => value + " " + fact.Label,
        };
    }

    private static string AccessibleName<T>(T entity, IReadOnlyList<T> versions, int index)
        where T : IVersionedPage
    {
        string? Sentence(T version) =>
            version.Description?.Split(['.', '!', '?'], 2)[0].Trim() is { Length: > 0 } text
                ? text
                : null;
        var description = Sentence(entity);
        return
            description is not null
            && versions.Count(version => Sentence(version) == description) == 1
            ? description
            : $"{entity.Name}, icon {index + 1}";
    }

    private static string Signed(Fact fact, string value) => fact.Number >= 0 ? "+" + value : value;

    private static string SourceLabel(IVersionedPage entity, bool named)
    {
        var source = entity.Sources.FirstOrDefault();
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
