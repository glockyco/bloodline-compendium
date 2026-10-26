using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed class SettlementDeliveryMechanism : ISourceMechanism
{
    public IEnumerable<ItemSource> FindSources(CatalogContext context)
    {
        foreach (
            var (table, row) in context
                .Records.Tables.Where(table =>
                    WorldSourceData.IsTable(table, "BuildingResourceAndIncomeValues")
                )
                .SelectMany(table => table.Rows.Select(row => (table, row)))
        )
        {
            var count = WorldSourceData.Integer(row.Values["LootDeliveryQuantity"]);
            if (count is <= 0)
            {
                continue;
            }
            var building = WorldSourceData.Reference(
                EntityKind.Building,
                table.Path + ":" + row.Name,
                RecordValues.Text(row.Values["BuildingName"])
                    ?? RecordValues.String(row.Values["BuildingName"])
                    ?? ItemIdentity.Readable(row.Name)
            );
            foreach (var (item, weight) in WorldSourceData.Pool(row.Values["LootTable"]))
            {
                yield return new ItemSource(
                    item,
                    new Source(
                        SourceKind.SettlementDelivery,
                        RecordValues.Text(row.Values["BuildingName"])
                            ?? RecordValues.String(row.Values["BuildingName"])
                            ?? ItemIdentity.Readable(row.Name),
                        [],
                        null,
                        null,
                        [],
                        []
                    )
                    {
                        // BP_PersistantGameInstance:Check Plot Data makes LootDeliveryQuantity weighted picks.
                        Odds =
                            count is { } picks && WeightTotals.Chance(weight) is { } chance
                                ? LootOdds.Of(OddsEvent.Delivery, chance, PickCount.Fixed(picks))
                                : LootOdds.Unknown(
                                    OddsEvent.Delivery,
                                    "How many items a delivery brings is unknown."
                                ),
                        Entity = building,
                        Conditions = [WorldSourceData.BuildingCondition(building)],
                    }
                );
            }
        }
    }
}
