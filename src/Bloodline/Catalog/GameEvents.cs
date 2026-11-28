using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

/// <summary>
/// One list of world map events with the terrains whose map tags choose it. <see cref="Field"/> is the list's
/// property on the event actor.
/// </summary>
internal sealed record EventTerrain(string Name, string Field, IReadOnlyList<string> Terrains);

/// <summary>
/// An event that appears near the player on the world map. <see cref="Chances"/> holds the chance that a new event
/// is this one for each entry of <see cref="GameEvents.Terrains"/>, null where the terrain's list leaves it out.
/// <see cref="Stays"/> is how many seconds it stays on the map, <see cref="Spawns"/> what the camp map brings in
/// once the player enters it, and <see cref="Also"/> a sentence on what it brings besides creatures.
/// </summary>
internal sealed record WorldMapEvent(
    string Name,
    int Stays,
    IReadOnlyList<double?> Chances,
    IReadOnlyList<string> Spawns,
    string? Also
);

/// <summary>
/// An event the calendar puts on a day in one area. <see cref="Areas"/> is empty when it can happen in every area,
/// <see cref="Chance"/> is the chance that a day's event is this one, and <see cref="Rate"/> its weight in the draw
/// among the events of the day's area.
/// </summary>
internal sealed record CalendarEvent(
    string Name,
    string Description,
    IReadOnlyList<string> Areas,
    double Chance,
    double Rate,
    IReadOnlyList<string> Spawns
);

/// <summary>
/// The events that bring creatures and objects into the world for a while, read with <c>bloodline script</c>. The
/// weights, rates and areas come from the build's records, and what an event spawns from the functions named here,
/// each held by its fingerprint.
/// </summary>
internal sealed class GameEvents
{
    public const string WorldMapPage = "World Map Events";
    public const string CalendarPage = "Calendar Events";

    private const string List = "GameEvents";
    private const string Blueprints = "/Game/AIBehaviorToolkit/Blueprints/";
    private const string Areas = "/Game/Overworld/OpenWorldAreas/";
    private const string EventProps =
        Areas + "Small_Event_Areas/Small_Event_Area_PropsAndBuildings/";
    private const string EventActor = "/Game/Eudros/OverworldEventLocations/Overworld_EventActor";
    private const string TerrainTag =
        "/Game/Eudros/OverworldEventLocations/Overworld_LocationThemeTagActor.Overworld_LocationThemeTagActor_C";
    private const string Popup = "/Game/fantasy_gui_4/widgets/templates/popup/";
    private const string Player = "/Game/TopDownCharacter_Working";
    private const string BanditSpawner =
        "/Game/ChallengeLocations/Leveled_Bandit_Spawner.Leveled_Bandit_Spawner_C";
    private const string GoblinSpawner =
        "/Game/ChallengeLocations/Leveled_Goblin_Spawn_Melee.Leveled_Goblin_Spawn_Melee_C";
    private const string Meteorite = Areas + "MoonstoneMeteorite.MoonstoneMeteorite_C";

    /// <summary>
    /// <c>Overworld_EventActor:Select Location Data Table</c> picks the list by the terrain tag nearest the player,
    /// and a tag it does not name gets the first list.
    /// </summary>
    private static readonly (string Name, string Field, string[] Tags)[] Lists =
    [
        ("Pine Forest", "Pine Forest Events", ["Pine Forest"]),
        ("Birch Forest", "Birch Forest Events", ["Birch Forest", "Autumnal Birch Forest"]),
        ("Rocky Hills", "Rocky Hills Events", ["Rocky Hills"]),
        ("Plains", "Grassy Plains Events", ["Grassy Plains", "Snowy Plains", "Autumnal Plains"]),
        ("Desert", "Dune Events", ["Dunes", "Palm Dunes", "Palm Buttes", "Dry Buttes"]),
    ];

    /// <summary>The seconds an event stays on the map, from <c>Overworld_EventActor:Choose Actor to Spawn</c>.</summary>
    private static readonly Dictionary<string, int> Stay = new(StringComparer.Ordinal)
    {
        ["Ogre Den"] = 30,
        ["Goblin Camp"] = 30,
        ["Bandit Ambush"] = 15,
        ["Goblin Ambush"] = 15,
        ["Roaming Bandits"] = 45,
        ["Woodcutting Hotspot"] = 30,
        ["Mining Hotspot"] = 30,
        ["Undead Horde"] = 30,
        ["Moonstone Meteorite"] = 60,
        ["Hostile Logging Camp"] = 60,
        ["Caravan Ambush"] = 30,
        ["Lionsmane Battle (Small)"] = 60,
        ["Wolf Pack"] = 20,
        ["Bear"] = 20,
        ["Deer Grazing Spot"] = 60,
        ["Hallowed Bellkeeper"] = 60,
        ["Goblin Boar Riders"] = 30,
        ["Airship"] = 60,
        ["Bandit Fortress"] = 60,
        ["Merchant Caravan"] = 60,
        ["Band of Goblins"] = 60,
    };

    /// <summary>
    /// What <c>SmallEventSpawner:Recieve Event</c> spawns in the camp map for the event the player entered. Roaming
    /// Bandits is the wandering actor on the map, which enters as Bandit Ambush. An event that is not listed spawns
    /// no creature or object of its own.
    /// </summary>
    private static readonly Dictionary<string, string[]> CampSpawns = new(StringComparer.Ordinal)
    {
        ["Bandit Ambush"] = [BanditSpawner],
        ["Roaming Bandits"] = [BanditSpawner],
        ["Goblin Ambush"] = [GoblinSpawner],
        ["Undead Horde"] =
        [
            Blueprints + "Undead_Shambler.Undead_Shambler_C",
            Blueprints + "Undead_Walker.Undead_Walker_C",
            Blueprints + "Undead_Bonebag.Undead_Bonebag_C",
        ],
        ["Wolf Pack"] = [Blueprints + "WildWolf.WildWolf_C"],
        ["Bear"] = [Blueprints + "Bear.Bear_C"],
        ["Ogre Den"] = [Blueprints + "GreatOgre.GreatOgre_C"],
        ["Goblin Camp"] = [Areas + "GoblinCampsite.GoblinCampsite_C"],
        ["Caravan Ambush"] = [EventProps + "Caravan_Ambush.Caravan_Ambush_C", BanditSpawner],
        ["Hallowed Bellkeeper"] = [Blueprints + "UndeadTrollBellkeeper.UndeadTrollBellkeeper_C"],
        ["Bandit Fortress"] =
        [
            EventProps
                + "SmallEventAreaBuildings/GenericBuildings/Bandit_Fortress.Bandit_Fortress_C",
        ],
        // The battle spawns Lionsmane mercenaries and then runs the bandit attack that no list holds on its own.
        ["Lionsmane Battle (Small)"] = [BanditSpawner],
        ["Moonstone Meteorite"] = [Meteorite],
    };

    /// <summary>
    /// What an event brings besides creatures, as its branch of <c>SmallEventSpawner:Recieve Event</c> or its
    /// actor on the map reads.
    /// </summary>
    private static readonly Dictionary<string, string> Also = new(StringComparer.Ordinal)
    {
        ["Airship"] = "An airship that you can board.",
        ["Deer Grazing Spot"] = "Up to 25 Deer at once.",
        ["Hostile Logging Camp"] = "Its own area, the Hostile Logging Camp.",
        ["Lionsmane Battle (Small)"] = "Lionsmane mercenaries fight them.",
        ["Merchant Caravan"] = "A merchant who trades with you on the world map.",
        ["Mining Hotspot"] = "More ore deposits than the area has otherwise.",
        ["Moonstone Meteorite"] = "A Moonstone Meteorite.",
        ["Woodcutting Hotspot"] = "More trees than the area has otherwise.",
    };

    /// <summary>
    /// The events whose troops <c>SmallEventSpawner:Spawn Hostile Troops</c> draws by weight from a row of the army
    /// table.
    /// </summary>
    private static readonly Dictionary<string, string> Armies = new(StringComparer.Ordinal)
    {
        ["Band of Goblins"] = "BandOfGoblins",
        ["Goblin Boar Riders"] = "GoblinBoarRiders",
    };

    /// <summary>
    /// What <c>CalendarEvent_Spawner:Recieve Event</c> spawns for the day's event, by the function it calls for the
    /// event's name.
    /// </summary>
    private static readonly (
        string Event,
        string Function,
        string Fingerprint,
        string[] Spawns
    )[] CalendarSpawns =
    [
        (
            "Ghost Ship",
            "Spawn Ghost Ship",
            "e43a5f5afad8",
            ["/Game/PuzzleProps/UndeadAirship.UndeadAirship_C"]
        ),
        (
            "Badbarrel",
            "Spawn Bad Barrel",
            "1dd84d52aae9",
            ["/Game/PuzzleProps/Badbarrel.Badbarrel_C"]
        ),
        (
            "Damaged Airship",
            "Spawn Damaged Airship",
            "482e1b24e319",
            ["/Game/PuzzleProps/DwarvenAirship.DwarvenAirship_C"]
        ),
        (
            "Airship Crash",
            "Airship Crash",
            "00d141c4273c",
            ["/Game/PuzzleProps/DwarvenAirship.DwarvenAirship_C"]
        ),
        (
            "Warp Strider",
            "Spawn Warp Strider",
            "21fd9e278de8",
            [
                "/Game/ProceduralWalkMeshes/SimpleProceduralWalkDemo/Meshes/WalkingTripod.WalkingTripod_C",
            ]
        ),
        ("Moonstone Meteorite", "Spawn Moonstone Meteorite", "41de043c5faa", [Meteorite]),
        (
            "Meteor Storm",
            "Meteor Storm Time Checker",
            "8d225511e8a7",
            ["/Game/UltraDynamicSky/Blueprints/ShootingStar_Meteor.ShootingStar_Meteor_C"]
        ),
        ("Rabbit Swarm", "Spawn Rabbits", "da55b7a6b8be", [Blueprints + "Rabbit.Rabbit_C"]),
        ("Bandit Horde", "Spawn Bandits", "4fab2f8051b2", [BanditSpawner]),
    ];

    /// <summary>The lists of world map events with the terrains in play that use each.</summary>
    public IReadOnlyList<EventTerrain> Terrains { get; }
    public IReadOnlyList<WorldMapEvent> WorldMap { get; }
    public IReadOnlyList<CalendarEvent> Calendar { get; }
    public IReadOnlyList<string> CalendarAreas { get; }

    /// <summary>
    /// The calendar events that can happen in an area, each with the chance that a day brings it there: the day picks
    /// the area among all of them, then the event by rate among those the area allows.
    /// </summary>
    public IEnumerable<(CalendarEvent Event, double Chance)> CalendarIn(string area)
    {
        var pool = Calendar
            .Where(happening =>
                happening.Areas.Count == 0 || happening.Areas.Contains(area, StringComparer.Ordinal)
            )
            .ToArray();
        var total = pool.Sum(happening => happening.Rate);
        return pool.Select(happening => (happening, happening.Rate / total / CalendarAreas.Count));
    }

    public GameEvents(CatalogContext context)
    {
        Terrains = [];
        WorldMap = [];
        Calendar = [];
        CalendarAreas = [];
        if (Pinned(context, EventActor + ":Select Location Data Table", "9194e1b24180"))
        {
            Pinned(context, EventActor + ":Choose Actor to Spawn", "013bc615e833");
            Pinned(context, EventProps + "SmallEventSpawner:Recieve Event", "a5eba602f385");
            Pinned(context, EventProps + "SmallEventSpawner:Spawn Hostile Troops", "ff4d029f45a6");
            // The World Map Events page writes how often an event appears and how many the map holds.
            Pinned(context, Player + ":Spawn Overworld Event Actor", "5a3e37496fb6");
            Pinned(context, Player + ":Cooldown Overworld Event Spawn", "2734d40e24fa");
            (Terrains, WorldMap) = ReadWorldMap(context);
        }
        if (Pinned(context, Popup + "CalendarEvent_Spawner:Recieve Event", "afc40d234a45"))
        {
            Pinned(context, Popup + "CalendarEventManager:Generate Event", "9f298e1b103c");
            Pinned(
                context,
                Popup + "CalendarEventManager:Assemble Event For Given Location",
                "deab447ae7f9"
            );
            foreach (var (_, function, fingerprint, _) in CalendarSpawns)
                Pinned(context, Popup + "CalendarEvent_Spawner:" + function, fingerprint);
            (CalendarAreas, Calendar) = ReadCalendar(context);
        }
    }

    /// <summary>
    /// A number the Calendar Events page writes in a sentence, which the catalog cannot change with the records.
    /// </summary>
    private static void Written(string what, double? found, double written)
    {
        if (found != written)
            throw new InvalidDataException(
                $"{what} is {found} and the {CalendarPage} page says {written}. Update the page and {List}."
            );
    }

    private static bool Pinned(CatalogContext context, string citation, string fingerprint) =>
        ReadFunction.Find(context, citation, fingerprint, List) is not null;

    private static (IReadOnlyList<EventTerrain>, IReadOnlyList<WorldMapEvent>) ReadWorldMap(
        CatalogContext context
    )
    {
        var actor = context.Classes[EventActor + ".Overworld_EventActor_C"];
        var named = Lists.SelectMany(list => list.Tags).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var tags = context
            .Placements.Where(placement => placement.Actor.Class == TerrainTag)
            .Select(placement => RecordValues.String(placement.Properties["Location Type"]))
            .OfType<string>()
            .Where(tag => tag.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var terrains = Lists
            .Select(
                (list, index) =>
                    new EventTerrain(
                        list.Name,
                        list.Field,
                        [
                            .. tags.Where(tag =>
                                list.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)
                                || index == 0 && !named.Contains(tag)
                            ),
                        ]
                    )
            )
            .ToArray();
        var weights = terrains
            .Select(terrain =>
                RecordValues
                    .MapEntries(actor.Properties[terrain.Field])
                    .Select(pair =>
                        (
                            Name: RecordValues.String(pair.Key),
                            Weight: RecordValues.Number(pair.Value)
                        )
                    )
                    .Where(entry => entry.Name is not null && entry.Weight is > 0)
                    .ToDictionary(entry => entry.Name!, entry => entry.Weight!.Value)
            )
            .ToArray();
        var army = context.Records.Tables.SingleOrDefault(table =>
            table.Path == EventProps + "All_NPC_Army_Types.All_NPC_Army_Types"
        );
        var events = new List<WorldMapEvent>();
        foreach (var name in weights.SelectMany(list => list.Keys).Distinct(StringComparer.Ordinal))
        {
            if (!Stay.TryGetValue(name, out var stays))
                throw new InvalidDataException(
                    $"The world map event {name} is new. Read what it brings and update {List}."
                );
            var spawns = CampSpawns.GetValueOrDefault(name) ?? [];
            if (Armies.TryGetValue(name, out var row))
                spawns =
                [
                    .. RecordValues
                        .MapEntries(
                            army?.Rows.Single(entry => entry.Name == row)
                                .Values["ArmyTroopsSpawnRatesHostiles"]
                        )
                        .Where(pair => RecordValues.Number(pair.Value) is > 0)
                        .Select(pair => RecordValues.AnyReference(pair.Key))
                        .OfType<string>(),
                ];
            events.Add(
                new WorldMapEvent(
                    name,
                    stays,
                    [
                        .. weights.Select(list =>
                            list.TryGetValue(name, out var weight)
                                ? weight / list.Values.Sum()
                                : (double?)null
                        ),
                    ],
                    spawns,
                    Also.GetValueOrDefault(name)
                )
            );
        }
        return (terrains, events);
    }

    private static (IReadOnlyList<string> Areas, IReadOnlyList<CalendarEvent> Events) ReadCalendar(
        CatalogContext context
    )
    {
        var manager = context.Classes[Popup + "CalendarEventManager.CalendarEventManager_C"];
        var areas = RecordValues
            .Elements(manager.Properties["All Calendar Event Location Names"])
            .Select(RecordValues.String)
            .OfType<string>()
            .ToArray();
        Written("The number of areas with calendar events", areas.Length, 18);
        Written(
            "The number of days the calendar shows ahead",
            RecordValues.Number(manager.Properties["Foresight Range"]),
            14
        );
        var rows = context
            .Records.Tables.Single(table =>
                table.Path == Popup + "CalendarEventInfo.CalendarEventInfo"
            )
            .Rows.Select(row =>
                (
                    Name: RecordValues.String(row.Values["EventName"])
                        ?? RecordValues.Text(row.Values["EventName"])
                        ?? row.Name,
                    Description: RecordValues.String(row.Values["EventDescription"])
                        ?? RecordValues.Text(row.Values["EventDescription"])
                        ?? "",
                    Areas: RecordValues
                        .Elements(row.Values["AreasWhereEventCanSpawn"])
                        .Select(RecordValues.String)
                        .OfType<string>()
                        .ToArray(),
                    Rate: RecordValues.Number(row.Values["EventSpawnRate"]) ?? 0
                )
            )
            .ToArray();
        // Each free day picks one area at random and then draws one of the events that can happen there by rate.
        var chances = rows.ToDictionary(row => row.Name, _ => 0.0, StringComparer.Ordinal);
        foreach (var area in areas)
        {
            var pool = rows.Where(row =>
                    row.Rate > 0 && (row.Areas.Contains("ALL") || row.Areas.Contains(area))
                )
                .ToArray();
            foreach (var row in pool)
                chances[row.Name] += row.Rate / pool.Sum(entry => entry.Rate) / areas.Length;
        }
        var spawns = CalendarSpawns.ToDictionary(
            entry => entry.Event,
            entry => entry.Spawns,
            StringComparer.OrdinalIgnoreCase
        );
        return (
            areas,
            [
                .. rows.Where(row => chances[row.Name] > 0)
                    .Select(row => new CalendarEvent(
                        row.Name,
                        row.Description,
                        row.Areas.Contains("ALL") ? [] : row.Areas,
                        chances[row.Name],
                        row.Rate,
                        spawns.GetValueOrDefault(row.Name) ?? []
                    )),
            ]
        );
    }

    /// <summary>The entry of an event on its mechanic page, which a way that the event brings links to.</summary>
    public static SourceEvent Reference(SourceEventKind kind, string name)
    {
        var page = kind == SourceEventKind.WorldMap ? WorldMapPage : CalendarPage;
        return new SourceEvent(
            kind,
            name,
            new EntityReference(
                EntityKind.Mechanic,
                "Mechanic:" + page,
                page,
                ItemIdentity.Slug(page),
                null
            )
            {
                Fragment = ItemIdentity.Slug(name),
            }
        );
    }
}
