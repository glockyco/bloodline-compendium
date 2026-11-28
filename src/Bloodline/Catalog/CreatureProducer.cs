using Bloodline.Output;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed record CreatureOutput(CreatureIndex Index, IReadOnlyList<CreaturePage> Pages);

/// <summary>
/// Produces a page per creature name from the item pages' Dropped by sources: every item it drops, by the same
/// relation the item page shows, and the areas and events those sources name.
/// </summary>
internal static class CreatureProducer
{
    public static CreatureOutput Produce(IEnumerable<ItemPage> items)
    {
        var drops = items
            .SelectMany(item =>
                item.Sources.Where(source =>
                        source.Kind == SourceKind.EnemyDrop && source.Entity.Key is not null
                    )
                    .Select(source => (Item: CatalogGraph.Reference(item), Source: source))
            )
            .GroupBy(entry => entry.Source.Entity.Key!, StringComparer.Ordinal);
        var pages = new List<CreaturePage>();
        foreach (var group in drops)
        {
            var creature = group.First().Source.Entity;
            // Rows that read the same, such as one item from two classes of one name, are one row.
            var rows = group
                .GroupBy(
                    entry =>
                        CanonicalJson.Serialize(
                            new
                            {
                                entry.Item.Key,
                                entry.Source.Odds,
                                entry.Source.Amount,
                                entry.Source.Notes,
                                entry.Source.Conditions,
                                entry.Source.Unconfirmed,
                            }
                        ),
                    StringComparer.Ordinal
                )
                .Select(same =>
                {
                    var (item, source) = same.First();
                    return new CreatureDrop(
                        item,
                        [.. same.Select(entry => entry.Source.RelationId).Distinct()],
                        source.Odds,
                        source.Amount,
                        // The creature page says once that the creature is placed nowhere, so its rows do not.
                        [.. source.Notes.Where(note => note != EnemyDropMechanism.Absent)]
                    )
                    {
                        Conditions = source.Conditions,
                        Places =
                        [
                            .. same.SelectMany(entry => entry.Source.Places)
                                .DistinctBy(place => place.Key ?? place.Name)
                                .OrderBy(place => place.Name, StringComparer.OrdinalIgnoreCase),
                        ],
                        Events =
                        [
                            .. same.SelectMany(entry => entry.Source.Events)
                                .DistinctBy(entry => (entry.Kind, entry.Name)),
                        ],
                        Unconfirmed = source.Unconfirmed,
                    };
                })
                .OrderBy(drop => drop.Unconfirmed)
                .ThenByDescending(drop => drop.Odds?.Chance ?? 0)
                .ThenBy(drop => drop.Item.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var sources = group.Select(entry => entry.Source).ToArray();
            pages.Add(
                new CreaturePage(
                    creature.Key!,
                    creature.Slug ?? ItemIdentity.Slug(creature.Name),
                    creature.Name,
                    rows,
                    [
                        .. sources
                            .SelectMany(source => source.Places)
                            .DistinctBy(place => place.Key ?? place.Name)
                            .OrderBy(place => place.Name, StringComparer.OrdinalIgnoreCase),
                    ],
                    [
                        .. sources
                            .SelectMany(source => source.Events)
                            .DistinctBy(entry => (entry.Kind, entry.Name))
                            .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase),
                    ]
                )
                {
                    Unconfirmed = sources.All(source => source.Unconfirmed),
                }
            );
        }
        var ordered = pages.OrderBy(page => page.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        return new CreatureOutput(
            new CreatureIndex([
                .. ordered.Select(page => new CreatureSummary(
                    page.Key,
                    page.Slug,
                    page.Name,
                    page.Drops.Select(drop => drop.Item.Key).Distinct().Count(),
                    page.Places.Count
                )
                {
                    Unconfirmed = page.Unconfirmed,
                }),
            ]),
            ordered
        );
    }
}
