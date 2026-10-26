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
                        $"Base spawn rate: {rate.ToString(CultureInfo.InvariantCulture)}. Player level, item level and item type modify this rate before selection.",
                        "Used by containers with Use Entire Data Table enabled. Container item types and required tags filter the pool. This is not an unconditional drop from every container.",
                    ]
                )
            );
        }
    }
}
