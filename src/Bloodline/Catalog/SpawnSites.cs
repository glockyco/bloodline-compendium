using System.Text.Json.Nodes;
using Bloodline.Code;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

/// <summary>
/// A function that spawns the classes it reads wherever its object is, read with <c>bloodline script</c>.
/// <see cref="Fingerprint"/> is the reviewed function's fingerprint, so a game update that changes it fails the
/// catalog until the function is read again. <see cref="Setting"/> names a count on the object that has to be above
/// zero for the spawn to happen, which a placement can set. <see cref="Except"/> holds classes the function reads
/// without spawning them.
/// </summary>
internal sealed record SpawnSite(string Citation, string Fingerprint, string? Setting = null)
{
    public IReadOnlyList<string> Except { get; init; } = [];
}

/// <summary>
/// Where the game brings a class into the world without a level holding it: a spawner's settings name it, a
/// level's own script spawns it, or an object that is there spawns it.
/// </summary>
internal sealed class SpawnSites
{
    private const string SpawnCall = "/Script/Engine:BeginDeferredActorSpawnFromClass";
    private static readonly string[] SpawnMapFields = ["Spawns"];
    private static readonly string[] PopulationFields = ["spawnDataArr", "npcParametersArr"];
    private static readonly string[] SpawnClassFields =
    [
        "Enemy to Spawn",
        "Enemy To Spawn",
        "Actor To Spawn",
        "Enemy Type",
    ];

    /// <summary>
    /// Each function spawns what it reads at the place of its object, and nothing but <see cref="SpawnSite.Setting"/>
    /// turns that off. An object can itself come from another site, as a bear spawner spawns sitting bears that
    /// each become a Bear. Scripts of other objects are not read for places, because most spawn only under a
    /// setting, as the open-world populator spawns Spiders by a count that is zero everywhere.
    /// </summary>
    internal static readonly SpawnSite[] Read =
    [
        // A Fairy Chest spawns its Fairy a second after it appears.
        new("/Game/PuzzleProps/FairyChest:ReceiveBeginPlay", "a736e416a45f"),
        // An Undead Ritual spawns spirits while it runs and an Ethereal Chest when the player stops it.
        new("/Game/PuzzleProps/UndeadRitual:Spawn Betwixt Spirit", "55b14cb57d1d"),
        new("/Game/PuzzleProps/UndeadRitual:Stopped Ritual", "530be74334bb"),
        // A city's guard becomes the Guard that fights once a fight with the player starts.
        new("/Game/FriendlyUnits/Soldiers/CityGuard_Dynamic:Begin Player Combat", "4e6485bb6e7e"),
        // A bear spawner spawns up to its maximum of sitting bears, and each becomes a Bear when it gets up.
        new(
            "/Game/Overworld/OpenWorldAreas/BearSpawner:Spawn Bears",
            "d8df1a8e403a",
            "Max Bears to Spawn"
        ),
        new("/Game/AIBehaviorToolkit/Blueprints/Bear_Sitting:Spawn Bear", "3183f5d77070"),
        // The mass worship site spawns its worshippers, and praying there spawns a hostile one.
        new(
            "/Game/Overworld/Villages/VillageBuildings/Necropolis_Structures/Necropolis_Events/Necropolis_MassWorshipSite:ReceiveBeginPlay",
            "4fc5edc84a07"
        ),
        new(
            "/Game/Overworld/Villages/VillageBuildings/Necropolis_Structures/Necropolis_Events/Necropolis_Worshipers:Spawn Hostile Worshipper",
            "f1011f20d058"
        ),
        // A purifying ritual shrine opens a warpgate once its ritual starts, and the gate spawns helots.
        new("/Game/PuzzleProps/PurifyingRitualShrine:Skip Delay and Spawn", "77a076f3ea70"),
        new("/Game/Overworld/OpenWorldAreas/Challenge_Warpgate:ReceiveTick", "c2e3baf1377c"),
        // A cave entrance builds its cave, which spawns Spiders.
        new("/Game/PuzzleProps/CaveEntrance:ReceiveBeginPlay", "671b1c4ca449"),
        new("/Game/PuzzleProps/Cave_Generator_Child:Spawn Enemies", "736d583f9f07"),
        // A lost spirit leaves Betwixt Petals where it appears.
        new("/Game/PuzzleProps/LostSpiritSpawner:Spawn Petals", "725320c1caa9"),
        // A bug box spawns between its minimum and maximum of Illumiscuttles.
        new(
            "/Game/BloodlinesAlpha/Interactables/BugInteractionObjects/BugSpawnerBox:ReceiveBeginPlay",
            "64917c888862",
            "Max Spawn"
        ),
        // A Fieldscourge and a Scarecrow spawn the scarecrow that gives straw when they appear.
        new("/Game/AIBehaviorToolkit/Blueprints/Fieldscourge:Turn into Scarecrow", "b8ef88609fa1"),
        new(
            "/Game/AIBehaviorToolkit/Blueprints/ScarecrowEnemy:Turn into Scarecrow",
            "ff36e608caac"
        ),
        // A goblin camp spawns the spawner that picks its goblins.
        new("/Game/Overworld/OpenWorldAreas/GoblinCampsite:ReceiveBeginPlay", "9a8de8ab124f"),
        // A bandit fortress spawns a random one of two bandits at each of its posts.
        new(
            "/Game/Overworld/OpenWorldAreas/Small_Event_Areas/Small_Event_Area_PropsAndBuildings/SmallEventAreaBuildings/GenericBuildings/Bandit_Fortress:ReceiveBeginPlay",
            "4d57a4a2550e"
        )
        {
            Except = ["/Game/AIBehaviorToolkit/Blueprints/Goblin_Base.Goblin_Base_C"],
        },
        // The ghost ship of the calendar leaves an Ethereal Chest when it is destroyed.
        new("/Game/PuzzleProps/UndeadAirship:Spawn Ethereal Chest", "143a9fdb1e2a"),
    ];

    private const string Replace =
        "/Game/Overworld/OverworldInteractables/ReplaceActorsWithOtherActors";
    private const string AidaDialogue =
        "/Game/Dialogue/Askil/TheFlowingSands/Aida_FlowingSands.Aida_FlowingSands";

    /// <summary>Where a class comes into the world: a place it is spawned in, or an event that brings it.</summary>
    private readonly record struct Spot(EntityReference? Place, SourceEvent? Event)
    {
        public static implicit operator Spot(EntityReference place) => new(place, null);
    }

    private readonly CatalogContext context;
    private readonly Dictionary<string, List<EntityReference>> places = new(
        StringComparer.OrdinalIgnoreCase
    );
    private readonly Dictionary<string, List<SourceEvent>> events = new(
        StringComparer.OrdinalIgnoreCase
    );
    private readonly Dictionary<string, List<string>> replaced = new(
        StringComparer.OrdinalIgnoreCase
    );
    private readonly HashSet<(string Class, string? Place)> read = [];
    private readonly Dictionary<string, bool> draws = new(StringComparer.OrdinalIgnoreCase);
    private readonly IReadOnlyList<CodeFunction> code;

    /// <summary>The places where something spawns the class, empty when nothing does.</summary>
    public IReadOnlyList<EntityReference> Places(string path) =>
        places.TryGetValue(path, out var found) ? found : [];

    /// <summary>The world map and calendar events that bring the class, also through what they spawn.</summary>
    public IReadOnlyList<SourceEvent> Events(string path) =>
        events.TryGetValue(path, out var found) ? found : [];

    private IReadOnlyList<string> ReplacementNotes(string path) =>
        replaced.TryGetValue(path, out var found) ? found : [];

    /// <summary>
    /// Whether every place of the class comes from a function in <see cref="Read"/>. Other places come from a
    /// spawner's settings or a level's script, where it is not read when the spawn happens.
    /// </summary>
    public bool IsRead(string path) =>
        Places(path).All(place => read.Contains((path.ToLowerInvariant(), place.Key)));

    public IEnumerable<string> Classes => places.Keys;

    /// <summary>The classes an event brings, also through what it spawns.</summary>
    public IEnumerable<string> Brought(SourceEvent happening) =>
        events.Where(pair => pair.Value.Contains(happening)).Select(pair => pair.Key);

    /// <summary>
    /// The way with how its giver comes into the world besides its places: the events that bring it, and the
    /// person it replaces in a conversation.
    /// </summary>
    public EntitySource Arriving(EntitySource candidate)
    {
        if (candidate.Source.Entity.Key is not { } key)
            return candidate;
        // A container or pickup is keyed by its class and the name it shows.
        var path = key.Split('#')[0];
        var during = Events(path);
        var notes = ReplacementNotes(path);
        if (during.Count == 0 && notes.Count == 0)
            return candidate;
        return candidate with
        {
            Source = candidate.Source with
            {
                Events =
                    during.Count == 0
                        ? candidate.Source.Events
                        :
                        [
                            .. during
                                .OrderBy(happening => happening.Kind)
                                .ThenBy(happening => happening.Name, StringComparer.Ordinal),
                        ],
                Notes =
                    notes.Count == 0
                        ? candidate.Source.Notes
                        : [.. candidate.Source.Notes, .. notes],
            },
        };
    }

    public SpawnSites(CatalogContext context, IReadOnlyList<SpawnSite>? sites = null)
    {
        this.context = context;
        code = context.Records.Code?.Functions ?? [];
        foreach (var placement in context.Placements)
        {
            var place = context.Places.Get(placement.Level);
            Scan(place, placement.Properties, placement.Actor.Class, false);
            // A placement holds only what it changes, so what its class spawns by default counts too.
            if (context.Classes.TryGetValue(placement.Actor.Class, out var placed))
                Scan(place, placed.Properties, placement.Actor.Class, false);
        }
        // A level's own script can spawn an enemy, as Glennheim Hills spawns The Copper King. What a stream's chat
        // spawns is left out, because a player does not meet it. A level's script also opens the window that lists
        // the enemies a player can call there, as in Harbingers Arena. An excluded level's script counts too, so
        // that what only it spawns has that level as its place and leaves the pages with it.
        var levels = context
            .Records.Levels.Select(level => level.Path)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var function in code)
        {
            if (!levels.Contains(function.Package))
                continue;
            var place = context.Places.Get(function.Package);
            var objects = Objects(function);
            if (
                function.Calls.Contains(SpawnCall)
                && !function.Name.Contains("TwitchIntegrator", StringComparison.Ordinal)
            )
                foreach (var spawned in objects)
                    Add(place, spawned, false);
            foreach (var path in objects)
            {
                if (context.Classes.TryGetValue(path, out var window))
                    foreach (
                        var enemy in RecordValues.Elements(window.Properties["Available Enemies"])
                    )
                        Add(place, RecordValues.AnyReference(enemy), false);
            }
        }
        foreach (var happening in context.Events.WorldMap)
        foreach (var spawned in happening.Spawns)
            Add(
                new Spot(null, GameEvents.Reference(SourceEventKind.WorldMap, happening.Name)),
                spawned,
                true
            );
        foreach (var happening in context.Events.Calendar)
        foreach (var spawned in happening.Spawns)
            Add(
                new Spot(null, GameEvents.Reference(SourceEventKind.Calendar, happening.Name)),
                spawned,
                true
            );
        Replacements();
        ReadSites(sites ?? Read);
    }

    /// <summary>
    /// A conversation can replace a person with another object: its event finds every object with a tag, removes
    /// it and spawns its own class there, as Aida's conversation spawns the Demon of Grief.
    /// </summary>
    private void Replacements()
    {
        if (
            ReadFunction.Find(
                context,
                Replace + ":RecieveEventTriggered",
                "f2356bab079f",
                "SpawnSites.Replacements"
            )
            is null
        )
            return;
        foreach (var dialogue in context.Records.Dialogues)
        foreach (var inner in dialogue.Objects)
        {
            if (
                inner.Class != Replace + ".ReplaceActorsWithOtherActors_C"
                || RecordValues.AnyReference(inner.Properties["Actor To Spawn"]) is not { } spawned
                || RecordValues.String(inner.Properties["Tag to Find"]) is not { } tag
            )
                continue;
            foreach (var placement in context.Placements)
            {
                if (
                    !RecordValues
                        .Elements(placement.Properties["Tags"])
                        .Any(entry => RecordValues.String(entry) == tag)
                )
                    continue;
                Add(context.Places.Get(placement.Level), spawned, true);
                if (!replaced.TryGetValue(spawned, out var notes))
                    replaced.Add(spawned, notes = []);
                var name = WorldSourceData.Name(placement.Properties, placement.Actor.Class);
                var note = ReplacementNote(dialogue, inner, name);
                if (!notes.Contains(note))
                    notes.Add(note);
            }
        }
    }

    private static string ReplacementNote(
        AssetRecord dialogue,
        InnerObjectRecord replacement,
        string name
    )
    {
        // The lab reached this conversation through Force Talk, then advanced through the replacement's reply.
        if (dialogue.Path == AidaDialogue)
        {
            foreach (var node in RecordValues.Elements(dialogue.Properties["Data"]))
            {
                var reply = RecordValues.Text(RecordValues.At(node, "Text"));
                if (
                    reply == "*Fight!*"
                    && RecordValues.Boolean(RecordValues.At(node, "isPlayer")) == true
                    && RecordValues
                        .Elements(RecordValues.At(node, "Events"))
                        .Any(evt =>
                            string.Equals(
                                RecordValues.AnyReference(evt),
                                replacement.Path,
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                )
                    return $"Choose \"{reply}\" in {name}'s conversation to make the demon appear";
            }
        }
        return $"Replaces {name} as you advance the conversation";
    }

    private void ReadSites(IReadOnlyList<SpawnSite> reviewed)
    {
        var sites = new List<(SpawnSite Site, CodeFunction Function)>();
        foreach (var site in reviewed)
        {
            // A build without the object has nothing to read, but one that changed it has to be read again.
            var function = ReadFunction.Find(
                context,
                site.Citation,
                site.Fingerprint,
                "SpawnSites.Read"
            );
            if (function is null)
                continue;
            if (!function.Calls.Contains(SpawnCall))
                throw new InvalidDataException(
                    $"{site.Citation} spawns nothing any more. Read it again and update SpawnSites.Read."
                );
            sites.Add((site, function));
        }
        var standing = context
            .Placements.GroupBy(
                placement => placement.Actor.Class.Split('.')[0],
                StringComparer.OrdinalIgnoreCase
            )
            .ToDictionary(group => group.Key, group => group.ToArray());
        bool changed;
        do
        {
            changed = false;
            foreach (var (site, function) in sites)
            {
                var package = function.Package;
                var here = new List<Spot>();
                foreach (var placement in standing.GetValueOrDefault(package) ?? [])
                {
                    if (
                        site.Setting is null
                        || RecordValues.Number(placement.Properties[site.Setting]) is > 0
                    )
                        here.Add(context.Places.Get(placement.Level));
                }
                // An object that another site or an event spawns keeps its class defaults.
                var owner = function.Owner;
                if (
                    site.Setting is null
                    || context.Classes.TryGetValue(owner, out var type)
                        && RecordValues.Number(type.Properties[site.Setting]) is > 0
                )
                {
                    here.AddRange(Places(owner).Select(place => (Spot)place));
                    here.AddRange(Events(owner).Select(happening => new Spot(null, happening)));
                }
                foreach (var spot in here.Distinct().ToArray())
                foreach (var spawned in Objects(function))
                    if (!site.Except.Contains(spawned, StringComparer.OrdinalIgnoreCase))
                        changed |= Add(spot, spawned, true);
            }
        } while (changed);
    }

    private static string[] Objects(CodeFunction function) =>
        function
            .Reads.Where(target => target.Kind == CodeTargetKind.Object)
            .Select(target => target.Path)
            .ToArray();

    /// <summary>
    /// An Enemies table counts only where a script of the class or of a class it derives from draws from it when it
    /// spawns, as the fort on The Scorched Coast does. The dungeon populator holds such tables and never reads them
    /// (design-facts.md, item 18).
    /// </summary>
    internal bool DrawsEnemies(string owner)
    {
        if (draws.TryGetValue(owner, out var known))
            return known;
        string[] owners =
        [
            owner,
            .. context.Classes.TryGetValue(owner, out var type) ? type.Ancestors : [],
        ];
        var scripts = code.Where(function =>
                owners.Any(candidate => candidate.Split('.')[0] == function.Package)
            )
            .ToArray();
        bool Reads(CodeFunction function) =>
            function.Reads.Any(target =>
                owners.Any(candidate => target.Path == candidate + ":Enemies")
            );
        return draws[owner] = scripts.Any(function =>
            function.Calls.Contains(SpawnCall)
            && (
                Reads(function)
                || scripts.Any(called =>
                    Reads(called)
                    && function.Calls.Any(call =>
                        call == called.Citation || call == "virtual:" + called.Name
                    )
                )
            )
        );
    }

    private void Scan(Spot spot, JsonObject properties, string? owner, bool site)
    {
        foreach (var field in PopulationFields)
        foreach (var entry in RecordValues.Elements(properties[field]))
        {
            if (RecordValues.Integer(RecordValues.At(entry, "maxSpawnNPC")) is <= 0)
                continue;
            Add(spot, RecordValues.AnyReference(RecordValues.At(entry, "classNPC")), site);
        }
        foreach (var field in SpawnMapFields)
        foreach (var (key, value) in RecordValues.MapEntries(properties[field]))
        {
            if (RecordValues.Number(value) is > 0)
                Add(spot, RecordValues.AnyReference(key), site);
        }
        if (owner is not null && properties["Enemies"] is { } enemies && DrawsEnemies(owner))
        {
            foreach (var (key, value) in RecordValues.MapEntries(enemies))
                if (RecordValues.Number(value) is > 0)
                    Add(spot, RecordValues.AnyReference(key), site);
        }
        foreach (var field in SpawnClassFields)
            Add(spot, RecordValues.AnyReference(properties[field]), site);
        foreach (var (field, value) in properties)
        {
            if (
                field == "Quest Units To Spawn"
                || field.StartsWith("Wave ", StringComparison.Ordinal)
            )
                foreach (var entry in RecordValues.Elements(value))
                    Add(spot, RecordValues.AnyReference(entry), site);
            if (
                field.StartsWith("Level ", StringComparison.Ordinal)
                && field.EndsWith(" Spawn Rates", StringComparison.Ordinal)
            )
                foreach (var (key, weight) in RecordValues.MapEntries(value))
                    if (RecordValues.Number(weight) is > 0)
                        Add(spot, RecordValues.AnyReference(key), site);
        }
    }

    private bool Add(Spot spot, string? spawned, bool site)
    {
        if (spawned is null || context.IsExcludedClass(spawned))
            return false;
        if (spot.Event is { } happening)
        {
            if (!events.TryGetValue(spawned, out var during))
                events.Add(spawned, during = []);
            if (during.Contains(happening))
                return false;
            during.Add(happening);
        }
        else
        {
            var place = spot.Place!;
            if (!places.TryGetValue(spawned, out var known))
                places.Add(spawned, known = []);
            if (known.Contains(place))
                return false;
            known.Add(place);
            if (site)
                read.Add((spawned.ToLowerInvariant(), place.Key));
        }
        // A spawner can spawn another spawner, as a camp's population spawns the bandit spawner that picks its
        // bandits, so what the spawned class spawns stands in the same place or comes with the same event.
        if (context.Classes.TryGetValue(spawned, out var type))
            Scan(spot, type.Properties, spawned, site);
        return true;
    }
}
