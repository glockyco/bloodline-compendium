using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

/// <summary>
/// A chest that opens only with one key and then gives what its placement lists, read with <c>bloodline script</c>:
/// the overlap event asks <c>Check If Player Has Key In Inventory</c> for <c>Key Required</c> when
/// <c>Requires Key?</c> is set, keeps the key, and gives <c>Item(s) to give</c> and <c>Gold to give</c>.
/// </summary>
internal sealed class SpecialKeyChestMechanism : ISourceMechanism
{
    private const string Chest = "/Game/PuzzleProps/SpecialKeyTreasureChest";

    public IEnumerable<EntitySource> FindSources(CatalogContext context)
    {
        if (
            ReadFunction.Find(
                context,
                Chest + ":ExecuteUbergraph_SpecialKeyTreasureChest",
                "a818badcb450",
                "SpecialKeyChestMechanism"
            )
            is null
        )
            yield break;
        foreach (var placement in context.Placements)
        {
            if (placement.Actor.Class.Split('.')[0] != Chest)
                continue;
            var place = context.Places.Get(placement.Level);
            RelationCondition[] conditions = [];
            if (
                RecordValues.Boolean(placement.Properties["Requires Key?"]) == true
                && RecordValues.AnyReference(placement.Properties["Key Required"]) is { } key
            )
            {
                var link = context.ItemLink(key);
                conditions = [new(ConditionKind.GameState, link, null, "Requires " + link.Name)];
            }
            foreach (var item in RecordValues.Elements(placement.Properties["Item(s) to give"]))
            {
                if (RecordValues.AnyReference(item) is not { } path)
                    continue;
                yield return new EntitySource(
                    path,
                    new Source(
                        SourceKind.ContainerLoot,
                        "Locked Chest",
                        [place],
                        null,
                        new Amount(1, 1),
                        [],
                        []
                    )
                    {
                        Entity = WorldSourceData.Reference(
                            EntityKind.Container,
                            Chest + ".SpecialKeyTreasureChest_C",
                            "Locked Chest"
                        ),
                        Odds = new Odds(OddsEvent.Container, 1, 1, null),
                        Conditions = conditions,
                    }
                );
            }
        }
    }
}
