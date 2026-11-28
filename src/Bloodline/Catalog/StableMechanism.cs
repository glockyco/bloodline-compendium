using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

/// <summary>
/// A stable sells the mounts its placement lists, read with <c>bloodline script</c>: the window shows the price from
/// the mount's row of <c>MountDataTable</c>, in the column the table names Health, and buying takes that gold and
/// unlocks the mount. The lab showed 10,000 for the Brown Direwolf and 15,000 for the White Direwolf.
/// </summary>
internal sealed class StableMechanism : ISourceMechanism
{
    private const string List = "StableMechanism";
    private const string Window = "/Game/fantasy_gui_4/widgets/templates/popup/StableShop";
    private const string Stable = "/Game/fantasy_gui_4/widgets/templates/popup/StableShop_Actor";
    private const string Table = "/Game/BloodlinesAlpha/UI/Inventory/MountDataTable.MountDataTable";

    public IEnumerable<EntitySource> FindSources(CatalogContext context)
    {
        if (
            ReadFunction.Find(
                context,
                Window + ":ExecuteUbergraph_StableShop",
                "3fefcdbd494b",
                List
            )
                is null
            || ReadFunction.Find(
                context,
                Stable + ":Get Mount Data Table Info",
                "b20ac857efd7",
                List
            )
                is null
        )
            yield break;
        var rows = context.Records.Tables.SingleOrDefault(table => table.Path == Table)?.Rows ?? [];
        foreach (var placement in context.Placements)
        {
            if (placement.Actor.Class.Split('.')[0] != Stable)
                continue;
            var place = context.Places.Get(placement.Level);
            foreach (var entry in RecordValues.Elements(placement.Properties["Mounts for Sale"]))
            {
                if (RecordValues.AnyReference(entry) is not { } mount)
                    continue;
                // Get Mount Data Table Info finds the row named like the mount's name without its spaces.
                var key = context.ItemLink(mount).Name.Replace(" ", "", StringComparison.Ordinal);
                var row = rows.SingleOrDefault(row =>
                    string.Equals(row.Name, key, StringComparison.OrdinalIgnoreCase)
                );
                var price = row is null
                    ? null
                    : RecordValues.Integer(RecordValues.At(row.Values, "Health"));
                yield return new EntitySource(
                    mount,
                    new Source(
                        SourceKind.Stable,
                        "Stable",
                        [place],
                        price is { } gold ? new Price(checked((int)gold), null) : null,
                        new Amount(1, 1),
                        [],
                        []
                    )
                    {
                        Entity = WorldSourceData.PlacementEntity(
                            context,
                            placement,
                            EntityKind.Merchant,
                            "Stable"
                        ),
                        Conditions = WorldSourceData.Conditions(
                            context,
                            placement.Properties,
                            placement.Level
                        ),
                    }
                );
            }
        }
    }
}
