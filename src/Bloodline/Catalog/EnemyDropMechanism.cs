using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed class EnemyDropMechanism : ISourceMechanism
{
    private static readonly string[] PopulationFields = ["spawnDataArr", "npcParametersArr"];
    private static readonly string[] SpawnClassFields =
    [
        "Enemy to Spawn",
        "Enemy To Spawn",
        "Actor To Spawn",
        "Enemy Type",
    ];

    public IEnumerable<ItemSource> FindSources(CatalogContext context)
    {
        var spawnedPlaces = new Dictionary<string, List<EntityReference>>(
            StringComparer.OrdinalIgnoreCase
        );
        foreach (var placement in context.Placements)
        {
            foreach (var field in PopulationFields)
            foreach (var entry in RecordValues.Elements(placement.Properties[field]))
            {
                if (RecordValues.Integer(RecordValues.At(entry, "maxSpawnNPC")) is <= 0)
                    continue;
                Add(RecordValues.AnyReference(RecordValues.At(entry, "classNPC")));
            }
            foreach (var (key, value) in RecordValues.MapEntries(placement.Properties["Spawns"]))
            {
                if (RecordValues.Number(value) is > 0)
                    Add(RecordValues.AnyReference(key));
            }
            foreach (var field in SpawnClassFields)
                Add(RecordValues.AnyReference(placement.Properties[field]));
            foreach (var (field, value) in placement.Properties)
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

            void Add(string? enemy)
            {
                if (enemy is null || context.IsExcludedClass(enemy))
                    return;
                if (!spawnedPlaces.TryGetValue(enemy, out var places))
                    spawnedPlaces.Add(enemy, places = []);
                var place = context.Places.Get(placement.Level);
                if (!places.Contains(place))
                    places.Add(place);
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
        field == "Loot Table" ? [] : ["A rare drop, chosen apart from the normal drop."];

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
