using System.Globalization;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed class RandomLootMechanism : ISourceMechanism
{
    public IEnumerable<ItemSource> FindSources(CatalogContext context)
    {
        foreach (
            var table in context.Records.Tables.Where(table =>
                ItemIdentity.ClassName(table.Path) == "AllLootSpawnRates"
            )
        )
        foreach (var row in table.Rows)
        {
            if (
                RecordValues.AnyReference(row.Values["ItemClass"]) is not { } item
                || RecordValues.Number(row.Values["BaseSpawnRate"]) is not > 0
            )
                continue;
            var rate = RecordValues.Number(row.Values["BaseSpawnRate"])!.Value;
            yield return new ItemSource(
                item,
                new Source(
                    SourceKind.RandomLoot,
                    "General random loot",
                    [],
                    null,
                    LootRecords.Quantity(context, item),
                    null,
                    null,
                    [],
                    [
                        $"Base loot rate: {rate.ToString(CultureInfo.InvariantCulture)}. Player level, item level and item type modify this rate before an item is chosen.",
                        "Available from containers that draw from general loot. Each container allows only certain item types and requires matching item tags. Not every container can drop this item.",
                    ]
                )
            );
        }
    }
}
