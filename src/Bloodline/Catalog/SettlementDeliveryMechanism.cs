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
            var notes = new List<string>();
            if (RecordValues.Number(row.Values["LootDeliveryQuantity"]) is { } attempts)
            {
                notes.Add(FormattableString.Invariant($"Delivery attempts: {attempts}."));
            }
            notes.Add(
                "Each delivery attempt randomly selects an item. Requires the completed settlement building."
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
