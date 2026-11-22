using Bloodline.SiteData;

namespace Bloodline.Catalog;

/// <summary>
/// The gaps every producer and mechanism finds while cataloging, collected by kind of gap for the maintainer
/// report.
/// </summary>
internal sealed class Coverage
{
    private readonly Dictionary<GapKind, List<GapEntry>> entries = Enum.GetValues<GapKind>()
        .ToDictionary(kind => kind, _ => new List<GapEntry>());

    public void Add(GapKind kind, GapEntry entry) => entries[kind].Add(entry);

    public void Add(GapKind kind, string label, EntityReference? entity, string? detail = null) =>
        entries[kind].Add(new GapEntry(label, entity, detail));

    /// <summary>Points every entry's entity at its canonical reference, such as an item version's page.</summary>
    public void Canonicalize(Func<EntityReference, EntityReference> canonical)
    {
        foreach (var list in entries.Values)
        {
            for (var index = 0; index < list.Count; index++)
            {
                if (list[index].Entity is { } entity)
                    list[index] = list[index] with { Entity = canonical(entity) };
            }
        }
    }

    public void RemoveAll(GapKind kind, Predicate<GapEntry> match) =>
        entries[kind].RemoveAll(match);

    public MaintainerReport Report(IReadOnlyList<StatusEvidence> statuses) =>
        new(
            entries
                .Where(pair => pair.Value.Count > 0)
                .OrderBy(pair => pair.Key)
                .Select(pair => new ReportGap(
                    pair.Key,
                    pair.Value.Distinct()
                        .OrderBy(entry => entry.Label, StringComparer.Ordinal)
                        .ThenBy(entry => entry.Entity?.Slug, StringComparer.Ordinal)
                        .ThenBy(entry => entry.Detail, StringComparer.Ordinal)
                        .ToArray()
                ))
                .ToArray(),
            statuses
        );
}
