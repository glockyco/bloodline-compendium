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
    private static readonly string[] LootFields = ["Loot Table", "Rare Pickup Loot"];

    public IEnumerable<ItemSource> FindSources(CatalogContext context)
    {
        var spawnedPlaces = new Dictionary<string, List<Place>>(StringComparer.OrdinalIgnoreCase);
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
            var nameNotes = new List<string>();
            var name = LootRecords.Name(properties, "Enemy Name", path, nameNotes);
            // Spawners use class defaults, not overrides of another placed enemy.
            var sourcePlaces =
                places.Count == 0 && spawnedPlaces.TryGetValue(path, out var spawned)
                    ? spawned
                    : places;
            foreach (var field in LootFields)
            foreach (
                var source in LootRecords.Pool(
                    context,
                    properties[field],
                    SourceKind.EnemyDrop,
                    name,
                    sourcePlaces,
                    RollNotes(field, places.Count == 0 && sourcePlaces.Count > 0, nameNotes),
                    "Chance per kill"
                )
            )
                yield return source;
        }
        var placedClasses = context
            .Placements.Select(placement => placement.Actor.Class)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var (path, places) in spawnedPlaces)
        {
            if (!context.Classes.TryGetValue(path, out var type) || !placedClasses.Contains(path))
                continue;
            var nameNotes = new List<string>();
            var name = LootRecords.Name(type.Properties, "Enemy Name", path, nameNotes);
            foreach (var field in LootFields)
            foreach (
                var source in LootRecords.Pool(
                    context,
                    type.Properties[field],
                    SourceKind.EnemyDrop,
                    name,
                    places,
                    RollNotes(field, true, nameNotes),
                    "Chance per kill"
                )
            )
                yield return source;
        }
    }

    private static IReadOnlyList<string> RollNotes(
        string field,
        bool fromSpawner,
        IReadOnlyList<string> nameNotes
    )
    {
        var roll =
            field == "Loot Table"
                ? "Dropped when defeated."
                : "Rare loot dropped when defeated. Checked separately from normal loot.";
        return fromSpawner
            ? [roll, .. nameNotes, "Comes from a spawner whose conditions are unknown."]
            : [roll, .. nameNotes];
    }
}
