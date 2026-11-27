using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed class EnemyDropMechanism : ISourceMechanism
{
    public IEnumerable<EntitySource> FindSources(CatalogContext context)
    {
        var sites = context.SpawnSites;
        var placedClasses = context
            .Placements.Select(placement => placement.Actor.Class)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
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
            var spawned = !placedClasses.Contains(path) && places.Count > 0;
            var sourcePlaces = places;
            // A creature that no level holds, no spawner lists and nothing in play leads to cannot be met.
            var absent = sourcePlaces.Count == 0 && !context.CanAppear(path);
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
                yield return spawned && !sites.IsRead(path)
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
        foreach (var path in sites.Classes)
        {
            var places = sites.Places(path);
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
                yield return sites.IsRead(path)
                    ? source
                    : source with
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
