using Bloodline.SiteData;

namespace Bloodline.Catalog;

/// <summary>
/// The tables of the mechanic pages on events, built from the game's records so that a game update changes them
/// with the rest of the catalog. A section of the written content names its table in <see cref="MechanicSection.Data"/>.
/// </summary>
internal static class EventTables
{
    private const string NextEventChance = "How likely the next event in that terrain is this one.";

    public static IReadOnlyList<MechanicContent> Fill(
        IReadOnlyList<MechanicContent> content,
        CatalogContext context
    ) =>
        [
            .. content.Select(page =>
                page with
                {
                    Sections = [.. page.Sections.Select(section => Fill(section, page, context))],
                }
            ),
        ];

    private static MechanicSection Fill(
        MechanicSection section,
        MechanicContent page,
        CatalogContext context
    ) =>
        section.Data switch
        {
            null => section,
            "calendarEventGuide" => section with { Data = null },
            "calendarEventAreas" => section with
            {
                Data = null,
                Paragraphs =
                [
                    .. section.Paragraphs,
                    string.Join(", ", context.Events.CalendarAreas) + ".",
                ],
            },
            var data => section with { Data = null, Table = Build(context, page, data) },
        };

    private static MechanicTable Build(CatalogContext context, MechanicContent page, string data) =>
        data switch
        {
            "worldMapEventChances" => Chances(
                context.Events,
                SectionAnchor(page, "worldMapEventTerrains")
            ),
            "worldMapEventTerrains" => Terrains(context.Events),
            "worldMapEvents" => WorldMap(
                context,
                linkChances: page.Sections.Any(section => section.Data == "worldMapEventChances")
            ),
            "worldMapEventsWithChances" => WorldMap(context, includeChances: true),
            "calendarEvents" => Calendar(
                context.Events,
                SectionAnchor(page, "calendarEventAreas"),
                SectionAnchor(page, "calendarEventGuide")
            ),
            _ => throw new InvalidDataException(
                $"The mechanic page {page.Name} names the table {data}, which the catalog does not build."
            ),
        };

    private static string? SectionAnchor(MechanicContent page, string data) =>
        page.Sections.FirstOrDefault(section => section.Data == data) is { } section
            ? ItemIdentity.Slug(section.Heading)
            : null;

    private static MechanicTable Chances(GameEvents events, string? terrainAnchor)
    {
        var ordered = OrderedWorldMap(events);
        return new MechanicTable(
            ["Event", .. events.Terrains.Select(terrain => terrain.Name)],
            [
                .. ordered.Select(happening =>
                    (IReadOnlyList<string>)[happening.Name, .. events.Terrains.Select(_ => "")]
                ),
            ]
        )
        {
            Anchors =
            [
                .. ordered.Select(happening => ChanceAnchor(ItemIdentity.Slug(happening.Name))),
            ],
            ChanceContext = NextEventChance,
            ArrivalLink = terrainAnchor is null ? null : new(terrainAnchor, "Find your terrain"),
            ChanceColumns = Enumerable
                .Range(0, events.Terrains.Count)
                .ToDictionary(
                    index => index + 1,
                    IReadOnlyList<IReadOnlyList<MechanicChance>> (index) =>
                        [
                            .. ordered.Select(happening =>
                                (IReadOnlyList<MechanicChance>)[new(happening.Chances[index] ?? 0)]
                            ),
                        ]
                ),
        };
    }

    private static string ChanceAnchor(string eventAnchor) => $"chances-{eventAnchor}";

    private static WorldMapEvent[] OrderedWorldMap(GameEvents events) =>
        [.. events.WorldMap.OrderBy(happening => happening.Name, StringComparer.Ordinal)];

    private static MechanicTable Terrains(GameEvents events) =>
        new(
            ["Terrain List", "Terrain Around You"],
            [
                .. events.Terrains.Select(terrain =>
                    (IReadOnlyList<string>)[terrain.Name, string.Join(", ", terrain.Terrains)]
                ),
            ]
        );

    private static MechanicTable WorldMap(
        CatalogContext context,
        bool includeChances = false,
        bool linkChances = false
    )
    {
        var events = OrderedWorldMap(context.Events);
        string[] anchors = [.. events.Select(happening => ItemIdentity.Slug(happening.Name))];
        return new MechanicTable(
            includeChances
                ? ["Event", "What You Find", "Chance by Terrain", "Stays on the Map"]
                : ["Event", "What You Find", "Stays on the Map"],
            [
                .. events.Select(happening =>
                {
                    var creatures = string.Join(
                        ", ",
                        Creatures(
                            context,
                            GameEvents.Reference(SourceEventKind.WorldMap, happening.Name)
                        )
                    );
                    var brings =
                        creatures.Length > 0 && happening.Also is not null
                            ? creatures + ". " + happening.Also
                            : happening.Also ?? creatures;
                    var stays = FormattableString.Invariant($"{happening.Stays} seconds");
                    return includeChances
                        ? (IReadOnlyList<string>)[happening.Name, brings, "", stays]
                        : [happening.Name, brings, stays];
                }),
            ]
        )
        {
            Anchors = anchors,
            CellLinks = linkChances
                ? new Dictionary<int, IReadOnlyDictionary<int, MechanicCellLink>>
                {
                    [0] = Enumerable
                        .Range(0, events.Length)
                        .ToDictionary(
                            index => index,
                            index => new MechanicCellLink(
                                ChanceAnchor(anchors[index]),
                                $"Terrain chances for {events[index].Name}"
                            )
                        ),
                }
                : null,
            ChanceContext = includeChances ? NextEventChance : null,
            ChanceColumns = includeChances
                ? new Dictionary<int, IReadOnlyList<IReadOnlyList<MechanicChance>>>
                {
                    [2] =
                    [
                        .. events.Select(happening =>
                            (IReadOnlyList<MechanicChance>)
                                [
                                    .. happening
                                        .Chances.Select((chance, index) => (chance, index))
                                        .Where(entry => entry.chance is > 0)
                                        .Select(entry => new MechanicChance(
                                            entry.chance!.Value,
                                            context.Events.Terrains[entry.index].Name,
                                            "terrain"
                                        )),
                                ]
                        ),
                    ],
                }
                : null,
        };
    }

    private static MechanicTable Calendar(
        GameEvents events,
        string? areasAnchor,
        string? guideAnchor
    )
    {
        var ordered = events
            .Calendar.OrderByDescending(happening => happening.Chance)
            .ThenBy(happening => happening.Name, StringComparer.Ordinal)
            .ToArray();
        var ghostShip = Array.FindIndex(ordered, happening => happening.Name == "Ghost Ship");
        var areaLink = areasAnchor is null ? null : new MechanicCellLink(areasAnchor);
        Dictionary<int, MechanicCellLink>? areaLinks = null;
        if (areaLink is not null)
        {
            for (var index = 0; index < ordered.Length; index++)
                if (ordered[index].Areas.Count == 0)
                    (areaLinks ??= [])[index] = areaLink;
        }
        return new MechanicTable(
            ["Event", "Calendar Text", "Areas", "Chance per Day"],
            [
                .. ordered.Select(happening =>
                    (IReadOnlyList<string>)
                        [
                            happening.Name,
                            happening.Description,
                            happening.Areas.Count == 0
                                ? "All calendar event areas"
                                : string.Join(", ", happening.Areas),
                            "",
                        ]
                ),
            ]
        )
        {
            Anchors = [.. ordered.Select(happening => ItemIdentity.Slug(happening.Name))],
            GameColumns = [1],
            ArrivalLink = guideAnchor is null
                ? null
                : new(guideAnchor, "Check Calendar for the date and area"),
            CellLinks = areaLinks is null
                ? null
                : new Dictionary<int, IReadOnlyDictionary<int, MechanicCellLink>>
                {
                    [2] = areaLinks,
                },
            RowNotes =
                ghostShip >= 0
                    ? new Dictionary<int, string>
                    {
                        [ghostShip] = "Destroy the ship to find its chest.",
                    }
                    : null,
            ChanceColumns = new Dictionary<int, IReadOnlyList<IReadOnlyList<MechanicChance>>>
            {
                [3] =
                [
                    .. ordered.Select(happening =>
                        (IReadOnlyList<MechanicChance>)[new(happening.Chance)]
                    ),
                ],
            },
        };
    }

    /// <summary>The names of the creatures an event brings, as item pages name them.</summary>
    private static IEnumerable<string> Creatures(CatalogContext context, SourceEvent happening) =>
        context
            .SpawnSites.Brought(happening)
            .Where(path =>
                context.Classes.TryGetValue(path, out var type)
                && type.Properties.ContainsKey("Enemy Name")
                && (
                    type.Properties.ContainsKey("Loot Table")
                    || type.Properties.ContainsKey("Rare Pickup Loot")
                )
            )
            .Select(path => LootRecords.Name(context.Classes[path].Properties, "Enemy Name", path))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal);
}
