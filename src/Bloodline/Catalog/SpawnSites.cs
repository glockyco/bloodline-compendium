using System.Text.Json.Nodes;
using Bloodline.Code;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

/// <summary>
/// A function that spawns the classes it reads wherever its object is, read with <c>bloodline script</c>.
/// <see cref="Fingerprint"/> is the reviewed function's fingerprint, so a game update that changes it fails the
/// catalog until the function is read again. <see cref="Setting"/> names a count on the object that has to be above
/// zero for the spawn to happen, which a placement can set.
/// </summary>
internal sealed record SpawnSite(string Citation, string Fingerprint, string? Setting = null);

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
    ];

    private readonly CatalogContext context;
    private readonly Dictionary<string, List<EntityReference>> places = new(
        StringComparer.OrdinalIgnoreCase
    );
    private readonly HashSet<(string Class, string? Place)> read = [];
    private readonly Dictionary<string, bool> draws = new(StringComparer.OrdinalIgnoreCase);
    private readonly IReadOnlyList<CodeFunction> code;

    /// <summary>The places where something spawns the class, empty when nothing does.</summary>
    public IReadOnlyList<EntityReference> Places(string path) =>
        places.TryGetValue(path, out var found) ? found : [];

    /// <summary>
    /// Whether every place of the class comes from a function in <see cref="Read"/>. Other places come from a
    /// spawner's settings or a level's script, where it is not read when the spawn happens.
    /// </summary>
    public bool IsRead(string path) =>
        Places(path).All(place => read.Contains((path.ToLowerInvariant(), place.Key)));

    public IEnumerable<string> Classes => places.Keys;

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
        ReadSites(sites ?? Read);
    }

    private void ReadSites(IReadOnlyList<SpawnSite> reviewed)
    {
        var sites = new List<(SpawnSite Site, CodeFunction Function)>();
        foreach (var site in reviewed)
        {
            var function = code.SingleOrDefault(function => function.Citation == site.Citation);
            // A build without the object has nothing to read, but one that changed it has to be read again.
            if (
                function is null
                && context.Classes.Keys.Any(path =>
                    path.Split('.')[0] == site.Citation.Split(':')[0]
                )
            )
                throw new InvalidDataException(
                    $"{site.Citation} is gone, but its class is not. Read the class again and update SpawnSites.Read."
                );
            if (function is null)
                continue;
            if (
                !function.Fingerprint.StartsWith(site.Fingerprint, StringComparison.Ordinal)
                || !function.Calls.Contains(SpawnCall)
            )
                throw new InvalidDataException(
                    $"{site.Citation} changed since it was read as a spawn site. Read it again and update SpawnSites.Read."
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
                var here = new List<EntityReference>();
                foreach (var placement in standing.GetValueOrDefault(package) ?? [])
                {
                    if (
                        site.Setting is null
                        || RecordValues.Number(placement.Properties[site.Setting]) is > 0
                    )
                        here.Add(context.Places.Get(placement.Level));
                }
                // An object that another site spawns keeps its class defaults.
                var owner = function.Owner;
                if (
                    site.Setting is null
                    || context.Classes.TryGetValue(owner, out var type)
                        && RecordValues.Number(type.Properties[site.Setting]) is > 0
                )
                    here.AddRange(Places(owner));
                foreach (var place in here.Distinct())
                foreach (var spawned in Objects(function))
                    changed |= Add(place, spawned, true);
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

    private void Scan(EntityReference place, JsonObject properties, string? owner, bool site)
    {
        foreach (var field in PopulationFields)
        foreach (var entry in RecordValues.Elements(properties[field]))
        {
            if (RecordValues.Integer(RecordValues.At(entry, "maxSpawnNPC")) is <= 0)
                continue;
            Add(place, RecordValues.AnyReference(RecordValues.At(entry, "classNPC")), site);
        }
        foreach (var field in SpawnMapFields)
        foreach (var (key, value) in RecordValues.MapEntries(properties[field]))
        {
            if (RecordValues.Number(value) is > 0)
                Add(place, RecordValues.AnyReference(key), site);
        }
        if (owner is not null && properties["Enemies"] is { } enemies && DrawsEnemies(owner))
        {
            foreach (var (key, value) in RecordValues.MapEntries(enemies))
                if (RecordValues.Number(value) is > 0)
                    Add(place, RecordValues.AnyReference(key), site);
        }
        foreach (var field in SpawnClassFields)
            Add(place, RecordValues.AnyReference(properties[field]), site);
        foreach (var (field, value) in properties)
        {
            if (
                field == "Quest Units To Spawn"
                || field.StartsWith("Wave ", StringComparison.Ordinal)
            )
                foreach (var entry in RecordValues.Elements(value))
                    Add(place, RecordValues.AnyReference(entry), site);
            if (
                field.StartsWith("Level ", StringComparison.Ordinal)
                && field.EndsWith(" Spawn Rates", StringComparison.Ordinal)
            )
                foreach (var (key, weight) in RecordValues.MapEntries(value))
                    if (RecordValues.Number(weight) is > 0)
                        Add(place, RecordValues.AnyReference(key), site);
        }
    }

    private bool Add(EntityReference place, string? spawned, bool site)
    {
        if (spawned is null || context.IsExcludedClass(spawned))
            return false;
        if (!places.TryGetValue(spawned, out var known))
            places.Add(spawned, known = []);
        if (known.Contains(place))
            return false;
        known.Add(place);
        if (site)
            read.Add((spawned.ToLowerInvariant(), place.Key));
        // A spawner can spawn another spawner, as a camp's population spawns the bandit spawner that picks its
        // bandits, so what the spawned class spawns stands in the same place.
        if (context.Classes.TryGetValue(spawned, out var type))
            Scan(place, type.Properties, spawned, site);
        return true;
    }
}
