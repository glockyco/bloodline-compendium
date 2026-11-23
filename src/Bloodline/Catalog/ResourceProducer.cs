using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed record ResourceOutput(ResourceIndex Index, IReadOnlyList<ResourcePage> Pages);

/// <summary>
/// Produces a page per gathering resource from the item pages' sources and the gathering spots: every item it gives,
/// by the same relation the item page shows, the places it stands in with their spots, and for a harvest preset what a
/// harvest gives as lab runs measured it.
/// </summary>
internal static class ResourceProducer
{
    public static ResourceOutput Produce(
        IEnumerable<ItemPage> items,
        IEnumerable<GatherSpot> spots,
        HarvestPresets presets
    )
    {
        var loot = new Dictionary<
            string,
            (EntityReference Resource, List<(ResourceLoot Row, Source Source, string? Icon)> Rows)
        >(StringComparer.Ordinal);
        foreach (var item in items)
        {
            var reference = CatalogGraph.Reference(item);
            foreach (var source in item.Sources)
            {
                if (source.Entity is not { Kind: EntityKind.Resource, Key: { } key } resource)
                    continue;
                if (!loot.TryGetValue(key, out var entry))
                    loot[key] = entry = (resource, []);
                // Spots that are always this resource and spots drawn from a table give the same item by separate
                // relations, which one row carries together.
                var same = entry.Rows.FindIndex(row =>
                    row.Row.Item.Key == reference.Key && row.Row.Chance == source.Odds?.Chance
                );
                if (same >= 0)
                {
                    var merged = entry.Rows[same];
                    entry.Rows[same] = merged with
                    {
                        Row = merged.Row with
                        {
                            RelationIds = [.. merged.Row.RelationIds, source.RelationId],
                            Unconfirmed = merged.Row.Unconfirmed && source.Unconfirmed,
                        },
                    };
                    continue;
                }
                entry.Rows.Add(
                    (
                        new ResourceLoot(
                            reference,
                            [source.RelationId],
                            source.Odds?.Chance,
                            source.Unconfirmed
                        )
                        {
                            Conditions = source.Conditions,
                            Value = item
                                .Facts.FirstOrDefault(fact => fact.Stat == Stat.Value)
                                ?.Number,
                        },
                        source,
                        item.Icon
                    )
                );
            }
        }
        var spotsByResource = spots.ToLookup(
            spot => spot.Resource.Key ?? "",
            StringComparer.Ordinal
        );
        var byName = presets.Presets.ToDictionary(
            GatheringMechanism.PresetName,
            StringComparer.Ordinal
        );
        var pages = loot
            .Values.Select(entry =>
            {
                var (resource, rows) = entry;
                var preset = byName.GetValueOrDefault(resource.Name);
                var resourceSpots = spotsByResource[resource.Key!].ToArray();
                var ordered = rows.OrderByDescending(row => row.Row.Chance ?? -1)
                    .ThenBy(row => row.Row.Item.Name, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                return new ResourcePage(
                    resource.Key!,
                    resource.Slug!,
                    resource.Name,
                    preset?.Checks
                        ?? (
                            rows.Any(row => row.Source.Kind == SourceKind.Farming)
                                ? "Farming"
                                : "Gathering"
                        ),
                    ordered.Select(row => row.Row).ToArray(),
                    Places(resourceSpots)
                )
                {
                    Icon = ordered.Select(row => row.Icon).FirstOrDefault(icon => icon is not null),
                    Harvest = preset is null ? null : Harvest(preset),
                    Drawn = resourceSpots.Any(spot => spot.Share < 1),
                };
            })
            .OrderBy(page => page.Category, StringComparer.Ordinal)
            .ThenBy(page => page.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return new ResourceOutput(
            new ResourceIndex(
                pages
                    .Select(page => new ResourceSummary(
                        page.Key,
                        page.Slug,
                        page.Name,
                        page.Category,
                        page.Loot.Select(row => row.Item.Key).Distinct().Count()
                    )
                    {
                        Icon = page.Icon,
                        Profession = Need(page)?.Profession,
                        Level = Need(page)?.Level,
                    })
                    .ToArray()
            ),
            pages
        );
    }

    /// <summary>
    /// A tree or deposit gives three items, one per swing, and its experience and bonus roll only on a hit of the
    /// minigame's green area. A mushroom or herb is one pick without the minigame. A used tree or foraged spot is
    /// removed, and a used deposit stays (runs yield7 and forage1).
    /// </summary>
    private static ResourceHarvest Harvest(HarvestPreset preset)
    {
        var foraging = preset.Checks == "Foraging";
        return new ResourceHarvest(
            preset.Checks,
            preset.Level,
            preset.Experience,
            !foraging,
            foraging ? 1 : 3,
            preset.Checks == "Mining" ? ResourceAfterUse.Stays : ResourceAfterUse.Removed,
            preset.BonusFrom,
            preset.Bonus.Select(chance => (double)chance).ToArray()
        );
    }

    /// <summary>
    /// Each place with how many of the resource a visit finds on average, each spot or spawner counting what it holds
    /// times the chance it is there times the chance it is this resource, and how many it holds at most.
    /// </summary>
    private static ResourcePlace[] Places(IEnumerable<GatherSpot> spots) =>
        spots
            .GroupBy(spot => spot.Place.Key ?? spot.Place.Name, StringComparer.Ordinal)
            .Select(group => new ResourcePlace(
                group.First().Place,
                Math.Round(group.Sum(spot => (spot.Count ?? 0) * spot.Chance * spot.Share), 1),
                group.Sum(spot => spot.Most ?? 0),
                group.Any(spot => spot.Count is null)
            ))
            .OrderByDescending(place => place.Expected)
            .ThenBy(place => place.Place.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static (string Profession, int Level)? Need(ResourcePage page)
    {
        if (page.Harvest is { } harvest)
            return (harvest.Profession, harvest.Level);
        var condition = page
            .Loot.SelectMany(row => row.Conditions)
            .FirstOrDefault(condition =>
                condition.Kind == ConditionKind.ProficiencyLevel && condition.Entity is not null
            );
        return condition is { Entity: { } profession, Value: { } value }
            ? (profession.Name, (int)value)
            : null;
    }
}
