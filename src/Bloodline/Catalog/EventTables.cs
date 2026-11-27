using Bloodline.SiteData;

namespace Bloodline.Catalog;

/// <summary>
/// The tables of the mechanic pages on events, built from the game's records so that a game update changes them
/// with the rest of the catalog. A section of the written content names its table in <see cref="MechanicSection.Data"/>.
/// </summary>
internal static class EventTables
{
    public static IReadOnlyList<MechanicContent> Fill(
        IReadOnlyList<MechanicContent> content,
        CatalogContext context
    ) =>
        [
            .. content.Select(page =>
                page with
                {
                    Sections =
                    [
                        .. page.Sections.Select(section =>
                            section.Data is { } data
                                ? section with
                                {
                                    Data = null,
                                    Table = Build(context, page.Name, data),
                                }
                                : section
                        ),
                    ],
                }
            ),
        ];

    private static MechanicTable Build(CatalogContext context, string page, string data) =>
        data switch
        {
            "worldMapEventChances" => Chances(context.Events),
            "worldMapEventTerrains" => Terrains(context.Events),
            "worldMapEvents" => WorldMap(context),
            "worldMapEventsWithChances" => WorldMap(context, includeChances: true),
            "calendarEvents" => Calendar(context.Events),
            _ => throw new InvalidDataException(
                $"The mechanic page {page} names the table {data}, which the catalog does not build."
            ),
        };

    private static MechanicTable Chances(GameEvents events)
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

    private static WorldMapEvent[] OrderedWorldMap(GameEvents events) =>
        [.. events.WorldMap.OrderBy(happening => happening.Name, StringComparer.Ordinal)];

    private static MechanicTable Terrains(GameEvents events) =>
        new(
            ["Column", "Terrain Around You"],
            [
                .. events.Terrains.Select(terrain =>
                    (IReadOnlyList<string>)[terrain.Name, string.Join(", ", terrain.Terrains)]
                ),
            ]
        );

    private static MechanicTable WorldMap(CatalogContext context, bool includeChances = false)
    {
        var events = OrderedWorldMap(context.Events);
        return new MechanicTable(
            includeChances
                ? ["Event", "What It Brings", "Chance by Terrain", "Stays on the Map"]
                : ["Event", "What It Brings", "Stays on the Map"],
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
                    return (IReadOnlyList<string>)
                        [
                            happening.Name,
                            creatures.Length > 0 && happening.Also is not null
                                ? creatures + ". " + happening.Also
                                : happening.Also ?? creatures,
                            .. (includeChances ? new[] { "" } : []),
                            FormattableString.Invariant($"{happening.Stays} seconds"),
                        ];
                }),
            ]
        )
        {
            Anchors = [.. events.Select(happening => ItemIdentity.Slug(happening.Name))],
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
                                            context.Events.Terrains[entry.index].Name
                                        )),
                                ]
                        ),
                    ],
                }
                : null,
        };
    }

    private static MechanicTable Calendar(GameEvents events)
    {
        var ordered = events
            .Calendar.OrderByDescending(happening => happening.Chance)
            .ThenBy(happening => happening.Name, StringComparer.Ordinal)
            .ToArray();
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
