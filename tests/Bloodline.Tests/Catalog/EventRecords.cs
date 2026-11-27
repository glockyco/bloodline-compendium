using System.Text.Json.Nodes;
using Bloodline.Catalog;
using Bloodline.Code;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Tests.Catalog;

internal static class EventRecords
{
    public const string EventActor = "/Game/Eudros/OverworldEventLocations/Overworld_EventActor";
    public const string EventActorClass = EventActor + ".Overworld_EventActor_C";
    public const string TerrainTagClass =
        "/Game/Eudros/OverworldEventLocations/Overworld_LocationThemeTagActor.Overworld_LocationThemeTagActor_C";
    public const string EventProps =
        "/Game/Overworld/OpenWorldAreas/Small_Event_Areas/Small_Event_Area_PropsAndBuildings/";
    public const string ArmyTable = EventProps + "All_NPC_Army_Types.All_NPC_Army_Types";
    public const string Popup = "/Game/fantasy_gui_4/widgets/templates/popup/";
    public const string CalendarSpawner = Popup + "CalendarEvent_Spawner";
    public const string CalendarManager = Popup + "CalendarEventManager.CalendarEventManager_C";
    public const string CalendarTable = Popup + "CalendarEventInfo.CalendarEventInfo";
    public const string BanditSpawner =
        "/Game/ChallengeLocations/Leveled_Bandit_Spawner.Leveled_Bandit_Spawner_C";
    public const string Robber = "/Game/Invented/Robber.Robber_C";
    public const string Item = "/Game/Invented/Token.Token_C";
    public const string Level = "/Game/Invented/Meadow";

    public static JsonObject Map(params (string Name, double Weight)[] entries) =>
        new()
        {
            ["$map"] = new JsonArray(
                entries
                    .Select(entry =>
                        (JsonNode?)new JsonArray(entry.Name, JsonValue.Create(entry.Weight))
                    )
                    .ToArray()
            ),
        };

    public static JsonObject ReferenceMap(params (string Class, double Weight)[] entries) =>
        new()
        {
            ["$map"] = new JsonArray(
                entries
                    .Select(entry =>
                        (JsonNode?)
                            new JsonArray(
                                new JsonObject { ["$ref"] = entry.Class },
                                JsonValue.Create(entry.Weight)
                            )
                    )
                    .ToArray()
            ),
        };

    public static CatalogContext Create(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>>? worldLists = null,
        IReadOnlyList<string>? terrainTags = null,
        bool worldMap = true,
        IReadOnlyList<TableRow>? calendarRows = null,
        bool calendar = false,
        int areaCount = 18,
        double foresightRange = 14,
        IReadOnlyList<(string Class, double Weight)>? armyTroops = null,
        IReadOnlyList<ClassRecord>? additionalClasses = null,
        IReadOnlyList<TableRecord>? additionalTables = null
    )
    {
        var classes = new List<ClassRecord>();
        var functions = new List<CodeFunction>();
        if (worldMap)
        {
            var properties = new JsonObject
            {
                ["Pine Forest Events"] = Map([.. Entries("Pine Forest Events")]),
                ["Birch Forest Events"] = Map([.. Entries("Birch Forest Events")]),
                ["Rocky Hills Events"] = Map([.. Entries("Rocky Hills Events")]),
                ["Grassy Plains Events"] = Map([.. Entries("Grassy Plains Events")]),
                ["Dune Events"] = Map([.. Entries("Dune Events")]),
            };
            classes.Add(new ClassRecord(EventActorClass, [], properties));
            functions.Add(
                Function(EventActor, EventActorClass, "Select Location Data Table", "9194e1b24180")
            );
            functions.Add(
                Function(EventActor, EventActorClass, "Choose Actor to Spawn", "013bc615e833")
            );
        }
        if (calendar)
        {
            classes.Add(
                new ClassRecord(
                    CalendarManager,
                    [],
                    new JsonObject
                    {
                        ["All Calendar Event Location Names"] = new JsonArray(
                            Enumerable
                                .Range(1, areaCount)
                                .Select(index => (JsonNode?)JsonValue.Create($"Area {index}"))
                                .ToArray()
                        ),
                        ["Foresight Range"] = foresightRange,
                    }
                )
            );
            functions.Add(
                Function(CalendarSpawner, CalendarSpawner, "Recieve Event", "afc40d234a45")
            );
            functions.Add(
                Function(
                    Popup + "CalendarEventManager",
                    CalendarManager,
                    "Generate Event",
                    "9f298e1b103c"
                )
            );
            functions.Add(
                Function(
                    Popup + "CalendarEventManager",
                    CalendarManager,
                    "Assemble Event For Given Location",
                    "deab447ae7f9"
                )
            );
        }

        classes.AddRange(additionalClasses ?? []);
        var actors = (terrainTags ?? [])
            .Select(
                (tag, index) =>
                    new ActorRecord(
                        $"Terrain {index}",
                        TerrainTagClass,
                        new JsonObject { ["Location Type"] = tag }
                    )
            )
            .ToArray();
        var tables = new List<TableRecord>
        {
            new(
                ArmyTable,
                null,
                [
                    new TableRow(
                        "BandOfGoblins",
                        new JsonObject
                        {
                            ["ArmyTroopsSpawnRatesHostiles"] = ReferenceMap(
                                (armyTroops ?? [])
                                    .Select(entry => (entry.Class, entry.Weight))
                                    .ToArray()
                            ),
                        }
                    ),
                ]
            ),
        };
        if (calendar)
            tables.Add(new TableRecord(CalendarTable, null, calendarRows ?? []));
        tables.AddRange(additionalTables ?? []);
        var records = new CatalogRecords(
            new BuildRecord(1159290, 1, []),
            classes,
            tables,
            [],
            [new LevelRecord(Level, [], actors)],
            [],
            [],
            []
        )
        {
            Code = CodeIndex.Create(functions),
        };
        return new CatalogContext(records, new Dictionary<string, ItemPage>(), []);

        IEnumerable<(string Name, double Weight)> Entries(string field)
        {
            if (worldLists is not null && worldLists.TryGetValue(field, out var entries))
                return entries.Select(entry => (entry.Key, entry.Value));
            return [];
        }
    }

    public static TableRow CalendarRow(
        string name,
        string description,
        double rate,
        params string[] areas
    ) =>
        new(
            name,
            new JsonObject
            {
                ["EventName"] = name,
                ["EventDescription"] = description,
                ["AreasWhereEventCanSpawn"] = new JsonArray(
                    areas.Select(area => (JsonNode?)JsonValue.Create(area)).ToArray()
                ),
                ["EventSpawnRate"] = rate,
            }
        );

    private static CodeFunction Function(
        string package,
        string owner,
        string name,
        string fingerprint
    ) => new(package, owner, name, false, fingerprint, CodeParseStatus.Complete, [], [], [], []);
}
