using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed class CurrencyShopMechanism : ISourceMechanism
{
    public IEnumerable<ItemSource> FindSources(CatalogContext context)
    {
        foreach (var (placement, dialogue, instance, properties) in ShopMechanism.Events(context))
        {
            if (ItemIdentity.ClassName(instance.Class) != "OpenCustomCurrencyShop")
            {
                continue;
            }
            // These switches replace the authored map with bytecode literals outside record coverage.
            if (
                RecordValues.Boolean(properties["Default Lionsmane Content?"]) == true
                || RecordValues.Boolean(properties["Default Church of Arosa Content?"]) == true
            )
            {
                continue;
            }
            var currency = RecordValues.AnyReference(properties["Currency Item Type"]);
            var notes = new List<string>();
            if (RecordValues.Boolean(properties["Currency Tracked Via Achievements?"]) == true)
            {
                var achievement = RecordValues.String(properties["Currency Achievement Name"]);
                var title = context
                    .Records.Tables.Where(table =>
                        WorldSourceData.IsTable(table, "AllAchievements_DataTable")
                    )
                    .SelectMany(table => table.Rows)
                    .Where(row =>
                        !string.IsNullOrWhiteSpace(achievement)
                        && (
                            string.Equals(row.Name, achievement, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(
                                RecordValues.String(row.Values["ID"]),
                                achievement,
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                    )
                    .Select(row =>
                        RecordValues.Text(row.Values["LocalizableDisplayName"])
                        ?? RecordValues.String(row.Values["DisplayName"])
                    )
                    .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name));
                notes.Add(
                    title is null
                        ? "Currency is tracked through achievements. The achievement name is unknown."
                        : $"Currency is tracked through the achievement \"{title}\"."
                );
            }
            foreach (
                var (key, value) in RecordValues.MapEntries(properties["Shop Contents (Items)"])
            )
            {
                if (
                    RecordValues.AnyReference(key) is not { } item
                    || RecordValues.Integer(value) is not { } price
                    || price < 0
                )
                {
                    continue;
                }
                yield return new ItemSource(
                    item,
                    new Source(
                        SourceKind.CurrencyShop,
                        ShopMechanism.VendorName(placement.Properties, placement.Actor.Class),
                        [context.Places.Get(placement.Level)],
                        new Price(
                            checked((int)price),
                            currency is null ? null : context.ItemLink(currency)
                        ),
                        new Amount(1, 1),
                        null,
                        null,
                        [],
                        notes
                    )
                    {
                        Entity = WorldSourceData.PlacementEntity(
                            context,
                            placement,
                            EntityKind.Merchant,
                            ShopMechanism.VendorName(placement.Properties, placement.Actor.Class)
                        ),
                        Conditions =
                        [
                            .. WorldSourceData.Conditions(
                                context,
                                placement.Properties,
                                placement.Level
                            ),
                            .. WorldSourceData.DialogueConditions(context, dialogue, instance),
                        ],
                    }
                );
            }
        }
    }
}
