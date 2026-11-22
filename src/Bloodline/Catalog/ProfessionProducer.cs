using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed record ProfessionOutput(ProfessionIndex Index, IReadOnlyList<ProfessionPage> Pages);

/// <summary>
/// Produces a page per profession from the item pages' sources: every recipe a profession makes, crop it grows and
/// fish it catches, with the level the game sets. A source names its profession through its recipe or through its
/// profession level condition, so the pages list exactly what the item pages show.
/// </summary>
internal static class ProfessionProducer
{
    public static ProfessionOutput Produce(IEnumerable<ItemPage> items)
    {
        var rows = new Dictionary<string, List<ProfessionRow>>(StringComparer.Ordinal);
        var stations = new Dictionary<string, Dictionary<string, EntityReference>>(
            StringComparer.Ordinal
        );
        foreach (var item in items)
        foreach (var (source, profession, level) in Professions.Of(item))
        {
            var key = profession.Key!;
            var list = rows.TryGetValue(key, out var found) ? found : rows[key] = [];
            list.Add(
                new ProfessionRow(
                    CatalogGraph.Reference(item),
                    source.Kind,
                    level,
                    source.Recipe?.Experience
                )
            );
            if (source.Recipe?.Station is { Key: { } station } reference)
                (
                    stations.TryGetValue(key, out var known)
                        ? known
                        : stations[key] = new(StringComparer.Ordinal)
                ).TryAdd(station, reference);
        }
        var pages = Professions
            .All.Select(profession =>
            {
                var own = rows.GetValueOrDefault(profession.Key!) ?? [];
                return new ProfessionPage(
                    profession.Key!,
                    profession.Slug!,
                    profession.Name,
                    (stations.GetValueOrDefault(profession.Key!)?.Values.AsEnumerable() ?? [])
                        .OrderBy(station => station.Name, StringComparer.Ordinal)
                        .ToArray(),
                    // One row per thing and level, as several fishing spots set the same level for one fish.
                    own.DistinctBy(row =>
                            (row.Product.Key, row.Product.Fragment, row.Kind, row.Level)
                        )
                        .OrderBy(row => row.Level ?? int.MaxValue)
                        .ThenBy(row => row.Product.Name, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(row => row.Product.Key, StringComparer.Ordinal)
                        .ToArray()
                );
            })
            // A profession nothing names has nothing to show, and no reference leads to it.
            .Where(page => page.Rows.Count > 0)
            .ToArray();
        return new ProfessionOutput(
            new ProfessionIndex(
                pages
                    .Select(page => new ProfessionSummary(
                        page.Key,
                        page.Slug,
                        page.Name,
                        // An item made two ways, such as at two levels, counts once, as the page lists it once.
                        page.Rows.Select(row => (row.Product.Key, row.Product.Fragment)).Distinct().Count()
                    )
                    {
                        Icon = page
                            .Rows.Select(row => row.Product.Icon)
                            .FirstOrDefault(icon => icon is not null),
                        Stations = page.Stations,
                        MinLevel = page.Rows.Min(row => row.Level),
                        MaxLevel = page.Rows.Max(row => row.Level),
                    })
                    .ToArray()
            ),
            pages
        );
    }
}
