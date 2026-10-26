using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed class PickupObjectMechanism : ISourceMechanism
{
    internal const string LootItem =
        "/Game/BloodlinesAlpha/Interactables/LootItem_Pickup.LootItem_Pickup_C";
    private static readonly (string Class, string Field, int? Quantity)[] Collectibles =
    [
        (
            "/Game/BloodlinesAlpha/Interactables/CragstoneShard.CragstoneShard_C",
            "Cragstone Shard",
            null
        ),
        (
            "/Game/BloodlinesAlpha/Interactables/Frostvein_Stone.Frostvein_Stone_C",
            "Frostvein Chunk",
            null
        ),
        ("/Game/Blueprints/SpiritStone.SpiritStone_C", "Item To Add", null),
        (
            "/Game/BloodlinesAlpha/Interactables/BugInteractionObjects/Illumiscuttle_Pickup.Illumiscuttle_Pickup_C",
            "Illumiscuttle",
            1
        ),
        ("/Game/PuzzleProps/Betwixt_Petals_Pickup.Betwixt_Petals_Pickup_C", "Petals", null),
        (
            "/Game/BloodlinesAlpha/Interactables/EmberboughLeaf_Pickup.EmberboughLeaf_Pickup_C",
            "Emberbough Leaf",
            null
        ),
        ("/Game/Halloween_Assets/GhostgreenApple.GhostGreenApple_C", "Ghost Green Apple", 1),
    ];

    public IEnumerable<ItemSource> FindSources(CatalogContext context)
    {
        foreach (var (path, properties, places) in LootRecords.Instances(context, IsPickupObject))
        {
            var type = context.Classes[path];
            if (RecordValues.AnyReference(properties["Item"]) is { } item)
            {
                var usesItemQuantity = type.Path == LootItem || type.Ancestors.Contains(LootItem);
                var count = RecordValues.Integer(properties["Amount of Items"]);
                var multiple = RecordValues.Boolean(properties["Multiple Items?"]);
                if (!usesItemQuantity && multiple == true && count is <= 0)
                    continue;
                Amount? amount;
                IReadOnlyList<string> notes = [];
                if (usesItemQuantity || multiple == false || count is null && multiple is null)
                    amount = LootRecords.Quantity(context, item);
                else if (multiple == true)
                    amount = count is > 0 and <= int.MaxValue
                        ? new Amount((int)count.Value, (int)count.Value)
                        : null;
                else
                {
                    amount = null;
                    notes =
                    [
                        $"The listed amount is {count} items, but whether multiple items are granted is unknown.",
                    ];
                }
                yield return Grant(
                    item,
                    amount,
                    places,
                    WorldSourceData.Reference(
                        EntityKind.Resource,
                        path,
                        WorldSourceData.Name(properties, path)
                    ),
                    WorldSourceData.Conditions(
                        context,
                        properties,
                        places.Count == 1 ? places[0].Key : null
                    ),
                    notes
                );
            }
            foreach (var rule in Collectibles)
            {
                if (type.Path != rule.Class && !type.Ancestors.Contains(rule.Class))
                    continue;
                if (RecordValues.AnyReference(properties[rule.Field]) is not { } collectible)
                    continue;
                var amount = rule.Quantity is { } quantity
                    ? new Amount(quantity, quantity)
                    : LootRecords.Quantity(context, collectible);
                yield return Grant(
                    collectible,
                    amount,
                    places,
                    WorldSourceData.Reference(
                        EntityKind.Resource,
                        path,
                        WorldSourceData.Name(properties, path)
                    ),
                    WorldSourceData.Conditions(
                        context,
                        properties,
                        places.Count == 1 ? places[0].Key : null
                    )
                );
            }
        }
    }

    private static bool IsPickupObject(ClassRecord type) =>
        type.Path == ItemIdentity.Interactable
        || type.Ancestors.Contains(ItemIdentity.Interactable)
        || type.Path == LootItem
        || type.Ancestors.Contains(LootItem)
        || Collectibles.Any(rule => type.Path == rule.Class || type.Ancestors.Contains(rule.Class));

    private static ItemSource Grant(
        string item,
        Amount? amount,
        IReadOnlyList<EntityReference> places,
        EntityReference entity,
        IReadOnlyList<RelationCondition> conditions,
        IReadOnlyList<string>? notes = null
    ) =>
        new(
            item,
            new Source(SourceKind.Pickup, "World pickup", places, null, amount, [], notes ?? [])
            {
                Entity = entity,
                Conditions = conditions,
            }
        );
}
