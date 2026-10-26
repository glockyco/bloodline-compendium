using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed class SettlementDeliveryMechanism : ISourceMechanism
{
    public IEnumerable<ItemSource> FindSources(CatalogContext context)
    {
        foreach (
            var row in context
                .Records.Tables.Where(table =>
                    WorldSourceData.IsTable(table, "BuildingResourceAndIncomeValues")
                )
                .SelectMany(table => table.Rows)
        )
        {
            var count = WorldSourceData.Integer(row.Values["LootDeliveryQuantity"]);
            if (count is <= 0)
            {
                continue;
            }
            var notes = WorldSourceData.Notes(row.Values, "LootDeliveryQuantity").ToList();
            notes.Add(
                "Each delivery attempt selects from the pool. Requires the constructed settlement building."
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
                        weight,
                        null,
                        [],
                        notes
                    )
                );
            }
        }
    }
}
