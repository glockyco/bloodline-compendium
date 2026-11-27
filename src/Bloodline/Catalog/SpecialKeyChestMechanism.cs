using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

/// <summary>
/// A treasure chest that gives what its placement lists, read with <c>bloodline script</c>: the overlap event asks
/// <c>Check If Player Has Key In Inventory</c> for <c>Key Required</c> when <c>Requires Key?</c> is set, keeps the
/// key, and gives <c>Item(s) to give</c> and <c>Gold to give</c>. The special key chest names its key as an item,
/// and the plain treasure chest opens without one in every placement.
/// </summary>
internal sealed class SpecialKeyChestMechanism : ISourceMechanism
{
    private static readonly (string Package, string Function, string Fingerprint)[] Chests =
    [
        (
            "/Game/PuzzleProps/SpecialKeyTreasureChest",
            "ExecuteUbergraph_SpecialKeyTreasureChest",
            "a818badcb450"
        ),
        ("/Game/PuzzleProps/TreasureChest", "ExecuteUbergraph_TreasureChest", "927b0a9db04f"),
    ];

    public IEnumerable<EntitySource> FindSources(CatalogContext context)
    {
        var read = Chests
            .Where(chest =>
                ReadFunction.Find(
                    context,
                    chest.Package + ":" + chest.Function,
                    chest.Fingerprint,
                    "SpecialKeyChestMechanism"
                )
                    is not null
            )
            .Select(chest => chest.Package)
            .ToHashSet(StringComparer.Ordinal);
        foreach (var placement in context.Placements)
        {
            if (!read.Contains(placement.Actor.Class.Split('.')[0]))
                continue;
            var place = context.Places.Get(placement.Level);
            var name = "Treasure Chest";
            RelationCondition[] conditions = [];
            if (
                RecordValues.Boolean(placement.Properties["Requires Key?"]) == true
                && RecordValues.AnyReference(placement.Properties["Key Required"]) is { } key
            )
            {
                var link = context.ItemLink(key);
                conditions = [new(ConditionKind.GameState, link, null, "Requires " + link.Name)];
                name = "Locked Chest";
            }
            foreach (var item in RecordValues.Elements(placement.Properties["Item(s) to give"]))
            {
                if (RecordValues.AnyReference(item) is not { } path)
                    continue;
                yield return new EntitySource(
                    path,
                    new Source(
                        SourceKind.ContainerLoot,
                        name,
                        [place],
                        null,
                        new Amount(1, 1),
                        [],
                        []
                    )
                    {
                        Entity = WorldSourceData.Reference(
                            EntityKind.Container,
                            placement.Actor.Class,
                            name
                        ),
                        Odds = new Odds(OddsEvent.Container, 1, 1, null),
                        Conditions = conditions,
                    }
                );
            }
        }
    }
}
