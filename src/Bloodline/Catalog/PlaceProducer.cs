using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed record PlaceOutput(PlaceIndex Index, IReadOnlyList<PlacePage> Pages);

/// <summary>
/// Produces a page per place name from the item pages' confirmed sources that name it: each thing there that gives
/// items, by the kind of way, with every item it gives there and its category, the most likely first, the skills and
/// spellsongs its books and note sheets teach, and the calendar events that can happen there.
/// </summary>
internal static class PlaceProducer
{
    public static PlaceOutput Produce(
        IEnumerable<ItemPage> items,
        IEnumerable<EntityReference> places,
        IEnumerable<RelationRow> relations,
        GameEvents events
    )
    {
        places = [.. places.DistinctBy(place => place.Key)];
        var entries = items
            .SelectMany(item =>
                item.Sources.Where(source => !source.Unconfirmed)
                    .SelectMany(source =>
                        source
                            .Places.Where(place => place.Key is not null)
                            .Select(place =>
                                (
                                    Place: place,
                                    Source: source,
                                    Page: item,
                                    Item: CatalogGraph.Reference(item)
                                )
                            )
                    )
            )
            .GroupBy(entry => entry.Place.Key!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var lessons = relations
            .Where(row =>
                row.Source.Kind == EntityKind.Place
                && row.Kind is nameof(SourceKind.SkillBook) or nameof(SourceKind.NoteSheet)
            )
            .ToLookup(row => row.Source.Key!, StringComparer.Ordinal);
        var happenings = CalendarAreas(events, places);
        var pages = new List<PlacePage>();
        // Every place the graph knows gets a page, so every link to a place leads somewhere, even where nothing gives
        // items.
        foreach (var reference in places)
        {
            var place = entries.GetValueOrDefault(reference.Key!) ?? [];
            var groups = place
                .GroupBy(entry => entry.Source.Kind)
                .OrderBy(group => group.Key)
                .Select(group => new PlaceGroup(
                    group.Key,
                    [
                        .. group
                            .GroupBy(
                                entry => entry.Source.Entity.Key ?? entry.Source.Entity.Name,
                                StringComparer.Ordinal
                            )
                            .Select(thing =>
                            {
                                var given = thing
                                    .OrderByDescending(entry => entry.Source.Odds?.Chance ?? 1)
                                    .ThenBy(
                                        entry => entry.Item.Name,
                                        StringComparer.OrdinalIgnoreCase
                                    )
                                    .DistinctBy(entry => entry.Item.Key)
                                    .Select(entry => new PlaceItem(entry.Item, entry.Page.Category))
                                    .ToArray();
                                return new PlaceThing(thing.First().Source.Entity, given)
                                {
                                    RelationIds =
                                    [
                                        .. thing
                                            .Where(entry =>
                                                entry.Source.Entity.Key == reference.Key
                                            )
                                            .Select(entry => entry.Source.RelationId)
                                            .Distinct(),
                                    ],
                                };
                            })
                            .OrderBy(thing => thing.Entity.Name, StringComparer.OrdinalIgnoreCase),
                    ]
                ))
                .ToArray();
            pages.Add(
                new PlacePage(
                    reference.Key!,
                    reference.Slug ?? ItemIdentity.Slug(reference.Name),
                    reference.Name,
                    groups
                )
                {
                    Lessons =
                    [
                        .. lessons[reference.Key!]
                            .Select(row => new PlaceLesson(
                                row.Target,
                                Enum.Parse<SourceKind>(row.Kind),
                                row.Id
                            ))
                            .OrderBy(
                                lesson => lesson.Entity.Name,
                                StringComparer.OrdinalIgnoreCase
                            ),
                    ],
                    Events = happenings.TryGetValue(reference.Key!, out var area)
                        ?
                        [
                            .. events
                                .CalendarIn(area)
                                .OrderByDescending(entry => entry.Chance)
                                .ThenBy(entry => entry.Event.Name, StringComparer.Ordinal)
                                .Select(entry => new PlaceEvent(
                                    GameEvents.Reference(
                                        SourceEventKind.Calendar,
                                        entry.Event.Name
                                    ),
                                    entry.Event.Description,
                                    entry.Chance
                                )),
                        ]
                        : [],
                }
            );
        }
        var ordered = pages.OrderBy(page => page.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        return new PlaceOutput(
            new PlaceIndex([
                .. ordered.Select(page => new PlaceSummary(
                    page.Key,
                    page.Slug,
                    page.Name,
                    page.Groups.Sum(group => group.Things.Count)
                )),
            ]),
            ordered
        );
    }

    /// <summary>
    /// The calendar area of each place that is one, by place key. The calendar names an area as the game shows it,
    /// such as "Glennheim Hills" for The Glennheim Hills or "Three Sister's Forest" for Three Sisters Forest, so names
    /// match without a leading "The", apostrophes or case. An area without a place fails the build, so a new area
    /// gets its page.
    /// </summary>
    private static Dictionary<string, string> CalendarAreas(
        GameEvents events,
        IEnumerable<EntityReference> places
    )
    {
        static string Plain(string name) =>
            (name.StartsWith("The ", StringComparison.OrdinalIgnoreCase) ? name[4..] : name)
                .Replace("'", "", StringComparison.Ordinal)
                .ToLowerInvariant();
        var byName = places
            .GroupBy(place => Plain(place.Name), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var areas = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var area in events.CalendarAreas)
        {
            if (!byName.TryGetValue(Plain(area), out var matched) || matched.Length != 1)
                throw new InvalidDataException(
                    $"The calendar area {area} matches {matched?.Length ?? 0} places. Name it as one place."
                );
            areas.Add(matched[0].Key!, area);
        }
        return areas;
    }
}
