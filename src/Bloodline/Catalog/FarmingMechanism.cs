using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed class FarmingMechanism : ISourceMechanism
{
    public IEnumerable<ItemSource> FindSources(CatalogContext context)
    {
        foreach (
            var (table, row) in context
                .Records.Tables.Where(table => WorldSourceData.IsTable(table, "SeedDataTable"))
                .SelectMany(table => table.Rows.Select(row => (table, row)))
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
                        seed is null ? [] : [new ItemAmount(context.ItemLink(seed), 1)],
                        notes
                    )
                    {
                        // FarmingPlot_Base:Harvest hands the pool to a harvestable whose every pick chooses by weight.
                        Odds = WeightTotals.Chance(weight) is { } chance
                            ? LootOdds.Of(OddsEvent.Pick, chance, PickCount.Fixed(1))
                            : null,
                        Entity = WorldSourceData.Reference(
                            EntityKind.Resource,
                            table.Path + ":" + row.Name,
                            RecordValues.Text(values["ItemName"]) ?? ItemIdentity.Readable(row.Name)
                        ),
                        Conditions =
                            RecordValues.Number(values["FarmingLevel"]) is { } level
                                ?
                                [
                                    new RelationCondition(
                                        ConditionKind.ProficiencyLevel,
                                        null,
                                        level,
                                        FormattableString.Invariant(
                                            $"Requires farming level {level}."
                                        )
                                    ),
                                ]
                            : values.ContainsKey("FarmingLevel")
                                ?
                                [
                                    new RelationCondition(
                                        ConditionKind.Unknown,
                                        null,
                                        null,
                                        "The required farming level could not be read."
                                    ),
                                ]
                            : [],
                    }
                );
            }
        }
    }
}
