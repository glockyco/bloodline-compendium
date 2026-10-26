using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed class EnemyDropMechanism : ISourceMechanism
{
    public IEnumerable<ItemSource> FindSources(CatalogContext context)
    {
        var spawnedPlaces = new Dictionary<string, List<Place>>(StringComparer.OrdinalIgnoreCase);
        foreach (var placement in context.Placements)
        {
            foreach (var field in new[] { "spawnDataArr", "npcParametersArr" })
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
            Add(RecordValues.AnyReference(placement.Properties["Enemy to Spawn"]));

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
            foreach (var field in new[] { "Loot Table", "Rare Pickup Loot" })
            foreach (
                var source in LootRecords.Pool(
                    context,
                    properties[field],
                    SourceKind.EnemyDrop,
                    name,
                    sourcePlaces,
                    [
                        field == "Loot Table"
                            ? "Corpse loot roll. Separate from the rare pickup roll."
                            : "Rare pickup roll. Separate from corpse loot.",
                    ]
                )
            )
                yield return source;
        }
        foreach (var (path, places) in spawnedPlaces)
        {
            if (
                !context.Classes.TryGetValue(path, out var type)
                || !context.Placements.Any(placement => placement.Actor.Class == path)
            )
                continue;
            var name = LootRecords.Name(type.Properties, "Enemy Name", path);
            foreach (var field in new[] { "Loot Table", "Rare Pickup Loot" })
            foreach (
                var source in LootRecords.Pool(
                    context,
                    type.Properties[field],
                    SourceKind.EnemyDrop,
                    name,
                    places,
                    [
                        field == "Loot Table"
                            ? "Corpse loot roll. Separate from the rare pickup roll."
                            : "Rare pickup roll. Separate from corpse loot.",
                    ]
                )
            )
                yield return source;
        }
    }
}
