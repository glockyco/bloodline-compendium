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
                        "Some containers can contain this item. Not every container can drop it.",
                        "The chance depends on the container, player level, item level and item type, so a fixed chance is unknown.",
                    ]
                )
                {
                    Entity = WorldSourceData.Reference(
                        EntityKind.Activity,
                        table.Path,
                        "General random loot"
                    ),
                }
            );
        }
    }
}
