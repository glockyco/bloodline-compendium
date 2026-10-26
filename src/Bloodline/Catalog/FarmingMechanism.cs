using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed class FarmingMechanism : ISourceMechanism
{
    public IEnumerable<ItemSource> FindSources(CatalogContext context)
    {
        foreach (
            var row in context
                .Records.Tables.Where(table => WorldSourceData.IsTable(table, "SeedDataTable"))
                .SelectMany(table => table.Rows)
        )
        {
            var values = row.Values;
            if (WorldSourceData.Integer(values["Yield"]) is <= 0)
            {
                continue;
            }
            var seed = RecordValues.AnyReference(values["Item"]);
            var notes = new List<string>();
            if (RecordValues.Number(values["Yield"]) is { } attempts)
            {
                notes.Add(
                    FormattableString.Invariant(
                        $"Base harvest attempts before farming bonuses: {attempts}."
                    )
                );
            }
            if (RecordValues.Number(values["GrowDurationDays"]) is { } days)
            {
                notes.Add(FormattableString.Invariant($"Growing time in days: {days}."));
            }
            if (RecordValues.Number(values["FarmingLevel"]) is { } level)
            {
                notes.Add(FormattableString.Invariant($"Requires farming level {level}."));
            }
            foreach (var (item, weight) in WorldSourceData.Pool(values["HarvestItems"]))
            {
                yield return new ItemSource(
                    item,
                    new Source(
                        SourceKind.Farming,
                        RecordValues.Text(values["ItemName"]) ?? ItemIdentity.Readable(row.Name),
                        [],
                        null,
                        null,
                        weight,
                        null,
                        seed is null ? [] : [new ItemAmount(context.ItemLink(seed), 1)],
                        notes
                    )
                );
            }
        }
    }
}
