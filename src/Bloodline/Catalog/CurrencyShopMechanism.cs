using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed class CurrencyShopMechanism : ISourceMechanism
{
    public IEnumerable<EntitySource> FindSources(CatalogContext context)
    {
        foreach (var (placement, dialogue, instance, properties) in ShopMechanism.Events(context))
        {
            if (ItemIdentity.ClassName(instance.Class) != "OpenCustomCurrencyShop")
            {
                continue;
            }
            var requirements = WorldSourceData.DialogueConditions(context, dialogue, instance);
            // These switches replace the authored map, currency and title with literals in the shop window's own
            // script, read with `bloodline script` and held here by the functions' fingerprints.
            var defaults =
                RecordValues.Boolean(properties["Default Lionsmane Content?"]) == true
                || RecordValues.Boolean(properties["Default Church of Arosa Content?"]) == true;
            var stock = DefaultStock(context, properties);
            // A build without the shop window has no default stock to read.
            if (defaults && stock is null)
                continue;
            var currency =
                stock?.Currency ?? RecordValues.AnyReference(properties["Currency Item Type"]);
            var entries = stock is { } preset
                ? preset.Items.Select(entry =>
                    (Item: (string?)entry.Item, Price: (long?)entry.Price)
                )
                : RecordValues
                    .MapEntries(properties["Shop Contents (Items)"])
                    .Select(pair =>
                        (RecordValues.AnyReference(pair.Key), RecordValues.Integer(pair.Value))
                    );
            var notes = new List<string>();
            if (
                stock is null
                && RecordValues.Boolean(properties["Currency Tracked Via Achievements?"]) == true
            )
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
            foreach (var (key, value) in entries)
            {
                if (key is not { } item || value is not { } price || price < 0)
                {
                    continue;
                }
                yield return new EntitySource(
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
                            .. requirements.Shown,
                        ],
                    }
                )
                {
                    Unresolved = requirements.Unresolved,
                };
            }
        }
    }

    private const string Window = "/Game/fantasy_gui_4/widgets/sample_windows/CustomCurrencyShop";

    /// <summary>The stock the window sets for a shop event that asks for the Lionsmane or the church default.</summary>
    private static (string Currency, (string Item, long Price)[] Items)? DefaultStock(
        CatalogContext context,
        System.Text.Json.Nodes.JsonObject properties
    )
    {
        if (RecordValues.Boolean(properties["Default Lionsmane Content?"]) == true)
        {
            if (
                ReadFunction.Find(
                    context,
                    Window + ":Set Default Lionsmane Content",
                    "a99afbeeaf10",
                    "CurrencyShopMechanism"
                )
                is null
            )
                return null;
            return (
                Path(context, "Iuba_Coin"),
                [
                    (Path(context, "Torso_Armor_LionsMane"), 350),
                    (Path(context, "LionsmaneMace"), 250),
                    (Path(context, "LionsmaneDaggers"), 200),
                    (Path(context, "LionsmaneLongsword"), 300),
                    (Path(context, "LionsmaneAxe"), 300),
                    (Path(context, "LionsmaneHammer_Fixed"), 350),
                ]
            );
        }
        if (RecordValues.Boolean(properties["Default Church of Arosa Content?"]) == true)
        {
            if (
                ReadFunction.Find(
                    context,
                    Window + ":Set Default Church of Arosa Content",
                    "29116a9ebb7c",
                    "CurrencyShopMechanism"
                )
                is null
            )
                return null;
            return (
                Path(context, "Tithes"),
                [
                    (Path(context, "HeadArmor_PriestsHat"), 150),
                    (Path(context, "HeadArmor_DarkPriestHat"), 250),
                ]
            );
        }
        return null;
    }

    /// <summary>The class path of an item the window names by its class name alone.</summary>
    private static string Path(CatalogContext context, string name) =>
        context.Classes.Keys.Single(path =>
            path.EndsWith("/" + name + "." + name + "_C", StringComparison.Ordinal)
        );
}
