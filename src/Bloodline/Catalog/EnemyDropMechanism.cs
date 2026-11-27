using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed class EnemyDropMechanism : ISourceMechanism
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
    /// Functions of objects that stand in levels, each read with <c>bloodline script</c>: the function spawns its
    /// enemies wherever the object stands, and no setting of the object turns that off. A Fairy Chest spawns its
    /// Fairy a second after it appears, an Undead Ritual spawns spirits while it runs, and a city's guard becomes
    /// the Guard that fights once a fight starts. Scripts of other objects are not read for places, because most
    /// spawn only under a setting, as the open-world populator spawns Spiders by a count that is zero everywhere.
    /// </summary>
    private static readonly string[] ReadSpawnSites =
    [
        "/Game/PuzzleProps/FairyChest:ReceiveBeginPlay",
        "/Game/PuzzleProps/UndeadRitual:Spawn Betwixt Spirit",
        "/Game/FriendlyUnits/Soldiers/CityGuard_Dynamic:Begin Player Combat",
    ];

    public IEnumerable<EntitySource> FindSources(CatalogContext context)
    {
        var spawnedPlaces = new Dictionary<string, List<EntityReference>>(
            StringComparer.OrdinalIgnoreCase
        );
        var code = context.Records.Code?.Functions ?? [];
        var draws = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        // An Enemies table counts only where a script of the class or of a class it derives from draws from it
        // when it spawns, as the fort on The Scorched Coast does. The dungeon populator holds such tables and never
        // reads them (design-facts.md, item 18).
        bool DrawsEnemies(string owner)
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
            bool Reads(Code.CodeFunction function) =>
                function.Reads.Any(read =>
                    owners.Any(candidate => read.Path == candidate + ":Enemies")
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
        foreach (var placement in context.Placements)
        {
            var place = context.Places.Get(placement.Level);
            Spawns(place, placement.Properties, [], placement.Actor.Class);
            // A placement holds only what it changes, so what its class spawns by default counts too.
            if (context.Classes.TryGetValue(placement.Actor.Class, out var placed))
                Spawns(place, placed.Properties, [], placement.Actor.Class);
        }
        // A level's own script can spawn an enemy, as Glennheim Hills spawns The Copper King. What a stream's chat
        // spawns is left out, because a player does not meet it. A level's script also opens the window that lists
        // the enemies a player can call there, as in Harbingers Arena.
        var levels = context
            .Records.Levels.Select(level => level.Path)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var function in code)
        {
            if (!levels.Contains(function.Package))
                continue;
            var place = context.Places.Get(function.Package);
            var read = function
                .Reads.Where(read => read.Kind == Code.CodeTargetKind.Object)
                .Select(read => read.Path)
                .ToArray();
            if (
                function.Calls.Contains(SpawnCall)
                && !function.Name.Contains("TwitchIntegrator", StringComparison.Ordinal)
            )
                Spawns(place, [], read, null);
            foreach (var path in read)
            {
                if (context.Classes.TryGetValue(path, out var window))
                    Spawns(
                        place,
                        [],
                        RecordValues
                            .Elements(window.Properties["Available Enemies"])
                            .Select(RecordValues.AnyReference)
                            .OfType<string>(),
                        null
                    );
            }
        }
        var standing = context.Placements.ToLookup(
            placement => placement.Actor.Class.Split('.')[0],
            placement => context.Places.Get(placement.Level),
            StringComparer.OrdinalIgnoreCase
        );
        foreach (var site in ReadSpawnSites)
        {
            var places = standing[site.Split(':')[0]].Distinct().ToArray();
            if (places.Length == 0)
                continue;
            // A site that an update removes or stops spawning from has to be read again.
            var function =
                code.SingleOrDefault(function =>
                    function.Citation == site && function.Calls.Contains(SpawnCall)
                ) ?? throw new InvalidDataException($"Spawn site {site} no longer spawns.");
            var read = function
                .Reads.Where(read => read.Kind == Code.CodeTargetKind.Object)
                .Select(read => read.Path)
                .ToArray();
            foreach (var place in places)
                Spawns(place, [], read, null);
        }

        void Spawns(
            EntityReference place,
            System.Text.Json.Nodes.JsonObject root,
            IEnumerable<string> classes,
            string? owner
        )
        {
            foreach (var spawned in classes)
                Add(spawned);
            Scan(root, owner);

            void Scan(System.Text.Json.Nodes.JsonObject properties, string? owner)
            {
                foreach (var field in PopulationFields)
                foreach (var entry in RecordValues.Elements(properties[field]))
                {
                    if (RecordValues.Integer(RecordValues.At(entry, "maxSpawnNPC")) is <= 0)
                        continue;
                    Add(RecordValues.AnyReference(RecordValues.At(entry, "classNPC")));
                }
                foreach (var field in SpawnMapFields)
                foreach (var (key, value) in RecordValues.MapEntries(properties[field]))
                {
                    if (RecordValues.Number(value) is > 0)
                        Add(RecordValues.AnyReference(key));
                }
                if (
                    owner is not null
                    && properties["Enemies"] is { } enemies
                    && DrawsEnemies(owner)
                )
                {
                    foreach (var (key, value) in RecordValues.MapEntries(enemies))
                        if (RecordValues.Number(value) is > 0)
                            Add(RecordValues.AnyReference(key));
                }
                foreach (var field in SpawnClassFields)
                    Add(RecordValues.AnyReference(properties[field]));
                foreach (var (field, value) in properties)
                {
                    if (
                        field == "Quest Units To Spawn"
                        || field.StartsWith("Wave ", StringComparison.Ordinal)
                    )
                        foreach (var entry in RecordValues.Elements(value))
                            Add(RecordValues.AnyReference(entry));
                    if (
                        field.StartsWith("Level ", StringComparison.Ordinal)
                        && field.EndsWith(" Spawn Rates", StringComparison.Ordinal)
                    )
                        foreach (var (key, weight) in RecordValues.MapEntries(value))
                            if (RecordValues.Number(weight) is > 0)
                                Add(RecordValues.AnyReference(key));
                }
            }

            void Add(string? enemy)
            {
                if (enemy is null || context.IsExcludedClass(enemy))
                    return;
                if (!spawnedPlaces.TryGetValue(enemy, out var places))
                    spawnedPlaces.Add(enemy, places = []);
                if (places.Contains(place))
                    return;
                places.Add(place);
                // A spawner can spawn another spawner, as a camp's population spawns the bandit spawner that
                // picks its bandits, so what the spawned class spawns stands in the same place.
                if (context.Classes.TryGetValue(enemy, out var type))
                    Scan(type.Properties, enemy);
            }
        }
        foreach (
            var (path, properties, places) in LootRecords.Instances(
                context,
                type =>
                    type.Properties.ContainsKey("Enemy Name")
                    && (
                        type.Properties.ContainsKey("Loot Table")
                        || type.Properties.ContainsKey("Rare Pickup Loot")
                    )
            )
        )
        {
            var name = LootRecords.Name(properties, "Enemy Name", path);
            // Spawners use class defaults, not overrides of another placed enemy.
            var sourcePlaces =
                places.Count == 0 && spawnedPlaces.TryGetValue(path, out var spawned)
                    ? spawned
                    : places;
            // A creature that no level holds, no spawner lists and nothing else names cannot be met.
            var absent = sourcePlaces.Count == 0 && !context.NamedElsewhere(path);
            foreach (var (field, picks) in Pools(context, path))
            foreach (
                var source in LootRecords.Pool(
                    context,
                    properties[field],
                    SourceKind.EnemyDrop,
                    name,
                    sourcePlaces,
                    RollNotes(field),
                    WorldSourceData.Reference(EntityKind.Creature, path, name),
                    OddsEvent.Kill,
                    picks,
                    conditions: WorldSourceData.Conditions(
                        context,
                        properties,
                        sourcePlaces.Count == 1 ? sourcePlaces[0].Key : null
                    )
                )
            )
                yield return places.Count == 0 && sourcePlaces.Count > 0
                    ? source with
                    {
                        Unresolved = [SpawnerUnread],
                    }
                : absent
                    ? source with
                    {
                        Source = source.Source with
                        {
                            Unconfirmed = true,
                            Notes =
                            [
                                .. source.Source.Notes,
                                "No creature of this kind is placed anywhere in the world.",
                            ],
                        },
                    }
                : source;
        }
        var placedClasses = context
            .Placements.Select(placement => placement.Actor.Class)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var (path, places) in spawnedPlaces)
        {
            if (!context.Classes.TryGetValue(path, out var type) || !placedClasses.Contains(path))
                continue;
            var name = LootRecords.Name(type.Properties, "Enemy Name", path);
            foreach (var (field, picks) in Pools(context, path))
            foreach (
                var source in LootRecords.Pool(
                    context,
                    type.Properties[field],
                    SourceKind.EnemyDrop,
                    name,
                    places,
                    RollNotes(field),
                    WorldSourceData.Reference(EntityKind.Creature, path, name),
                    OddsEvent.Kill,
                    picks,
                    conditions: WorldSourceData.Conditions(
                        context,
                        type.Properties,
                        places.Count == 1 ? places[0].Key : null
                    )
                )
            )
                yield return source with
                {
                    Unresolved = [SpawnerUnread],
                };
        }
    }

    private const string SpawnerUnread =
        "A spawner places this enemy, and when it spawns is not read.";

    private static IReadOnlyList<string> RollNotes(string field) =>
        field == "Loot Table"
            ? []
            : ["Comes from a separate roll, in addition to the normal drop."];

    // Goblin_Base:Die picks once from the normal pool for every enemy. The rare pool drops only where a death
    // event spawns a Loot_Dropped with it, which then picks once, and these are the classes whose Drop Loot does
    // so with the authored pool, with the chance it gates the drop by. Bandit_Cutter, LobGob, Goblin_Archer,
    // Undead_Lancer and Herald_Emberbound also drop a rare pool, but replace it with their own weapon on
    // construction, and Fenraen's pool is what it digs up.
    private static readonly Dictionary<string, double> RareDrops = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["CopperCrownBandit"] = 1,
        ["Herald_Ashbinder"] = 0.25,
        ["Herald_Ashwarden"] = 0.25,
        ["Herald_Cindersworn"] = 0.25,
    };

    private static IEnumerable<(string Field, PickCount Picks)> Pools(
        CatalogContext context,
        string path
    )
    {
        yield return ("Loot Table", PickCount.Fixed(1));
        var classes = context.Classes.TryGetValue(path, out var type)
            ? type.Ancestors.Prepend(path)
            : [path];
        foreach (var name in classes.Select(ItemIdentity.ClassName))
        {
            if (RareDrops.TryGetValue(name, out var chance))
            {
                yield return ("Rare Pickup Loot", PickCount.Sometimes(chance));
                yield break;
            }
        }
    }
}
