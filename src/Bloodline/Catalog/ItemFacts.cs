using System.Globalization;
using System.Text.Json.Nodes;
using Bloodline.Code;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed class ItemFacts(
    IReadOnlyList<EnumRecord> enums,
    IReadOnlyList<TableRecord>? tables = null,
    CodeIndex? code = null,
    long build = 0
)
{
    private readonly ItemClaims claims = new(code, build);
    public IReadOnlyList<FactClaim> Claims => claims.Claims;

    private readonly Dictionary<string, string?> enumDisplays = enums
        .SelectMany(record => record.Entries.Select(entry => (entry.Name, entry.Display)))
        .GroupBy(entry => entry.Name, StringComparer.Ordinal)
        .ToDictionary(group => group.Key, group => group.First().Display, StringComparer.Ordinal);

    private readonly Dictionary<string, TableRow[]> components = (tables ?? [])
        .Where(table => ItemIdentity.ClassName(table.Path) == "WeaponComponentTable")
        .SelectMany(table => table.Rows)
        .Select(row => (Item: RecordValues.AnyReference(row.Values["Class"]), Row: row))
        .Where(entry => entry.Item is not null)
        .GroupBy(entry => entry.Item!, entry => entry.Row, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(
            group => group.Key,
            group => group.ToArray(),
            StringComparer.OrdinalIgnoreCase
        );

    public string? Display(JsonNode? value) =>
        RecordValues.Text(value)
        ?? RecordValues.EnumDisplay(value)
        ?? (RecordValues.EnumValue(value) is { } name ? enumDisplays.GetValueOrDefault(name) : null)
        ?? RecordValues.String(value)
        ?? RecordValues.Number(value)?.ToString("G", CultureInfo.InvariantCulture)
        ?? (
            RecordValues.Boolean(value) is { } flag
                ? flag
                    ? "Yes"
                    : "No"
                : null
        );

    public ItemPage Resolve(ClassRecord item, string slug)
    {
        var properties = item.Properties;
        var category = Display(RecordValues.At(properties, "ItemInfo.ItemType"));
        var component = components.GetValueOrDefault(item.Path);
        var componentPart = component is null ? null : ComponentValue(component, "Part");
        // CustomCurrencyShop's default shop functions select these inventory classes as currency.
        var currency = ItemIdentity.ClassName(item.Path) is "Iuba_Coin" or "Tithes";
        var readerCategory = ItemCategories.For(category, component is not null, currency);
        var type =
            currency ? "Currency"
            : component is not null ? componentPart
            : category switch
            {
                "Weapon" => Display(RecordValues.At(properties, "ItemInfo.SpecificItemType")),
                "Armor" => Display(RecordValues.At(properties, "ItemInfo.ItemSlot")),
                _ => readerCategory?.Name,
            };
        var facts = new List<Fact>();
        void Add(Stat stat, string path)
        {
            var value = RecordValues.At(properties, path);
            facts.Add(
                RecordValues.Number(value) is { } number
                    ? StatVocabulary.Number(stat, number)
                    : StatVocabulary.Text(stat, Display(value))
            );
        }
        Add(Stat.Value, "ItemInfo.ItemValue");
        if (category is null)
        {
            facts.Add(StatVocabulary.Unknown(Stat.Category));
        }
        if (component is not null)
        {
            // Crafting's component selector and Retrieve Component Stats read these table fields.
            // They are contributions, not the item's inherited weapon defaults or final weapon stats.
            facts.Add(
                StatVocabulary.Number(Stat.DamageContribution, ComponentNumber(component, "Damage"))
            );
            facts.Add(
                StatVocabulary.Number(Stat.SpeedContribution, ComponentNumber(component, "Speed"))
            );
        }
        else if (category == "Weapon")
        {
            Add(Stat.WeaponType, "WeaponInfo.AttackType");
            // Equipped damage comes from WeaponInfo, not the inventory display's ItemInfo.Damage.
            Add(Stat.Damage, "WeaponInfo.Damage");
            // The equipped attack montage uses this multiplier.
            Add(Stat.AttackSpeed, "WeaponInfo.AttackSpeed");
            var range = RecordValues.Number(RecordValues.At(properties, "WeaponInfo.Range"));
            if (range is null || range > 0)
            {
                Add(Stat.Range, "WeaponInfo.Range");
            }
            var damageType = Display(RecordValues.At(properties, "WeaponInfo.DamageType"));
            if (damageType != "None")
            {
                facts.Add(StatVocabulary.Text(Stat.DamageType, damageType));
            }
            var magazine = RecordValues.Number(
                RecordValues.At(properties, "WeaponInfo.MagazineSize")
            );
            if (magazine is null || magazine > 0)
            {
                Add(Stat.MagazineSize, "WeaponInfo.MagazineSize");
            }
        }
        if (category == "Armor")
        {
            Add(Stat.ArmorType, "ItemInfo.ArmorType");
            // Keep this class default separate from ArmorDataTable's distinct gameplay rating.
            Add(Stat.ArmorRating, "ArmorInfo.DamageRating");
        }
        if (category is "Food" or "Potion")
        {
            AddEffects(facts, RecordValues.At(properties, "ItemInfo.PotionInfo"));
        }
        if (RecordValues.At(properties, "ItemInfo.ReagentInfo") is JsonObject reagent)
        {
            foreach (
                var (treatment, stat) in new[]
                {
                    ("Chopped", Stat.ChoppedEffect),
                    ("Cooled", Stat.CooledEffect),
                    ("Ground", Stat.GroundEffect),
                    ("Heated", Stat.HeatedEffect),
                }
            )
            {
                var effects = RecordValues.Elements(reagent[treatment + "Effects"]).ToArray();
                if (reagent[treatment + "Effects"] is JsonArray && effects.Length == 0)
                {
                    continue;
                }
                AddEffect(
                    facts,
                    stat,
                    null,
                    reagent[treatment + "Effects"] is JsonArray ? JoinEffects(effects) : null,
                    RecordValues.Number(reagent[treatment + "EffectPower"]),
                    RecordValues.Number(reagent[treatment + "EffectDuration"])
                );
            }
        }
        var page = new ItemPage(
            item.Path,
            slug,
            RecordValues.Text(RecordValues.At(properties, "ItemInfo.ItemName"))!,
            RecordValues.Text(RecordValues.At(properties, "ItemInfo.Description")),
            new CategoryLink(
                ItemIdentity.Slug(readerCategory?.Name ?? "Unknown"),
                readerCategory?.Name ?? "Unknown"
            ),
            null,
            facts,
            []
        )
        {
            Type = type ?? "Unknown",
        };
        claims.Record(item.Path, page);
        return page;
    }

    private string? ComponentValue(IReadOnlyList<TableRow> rows, string field)
    {
        var value = Display(rows[0].Values[field]);
        // A class can occur in several rows with conflicting stats. No row is authoritative.
        for (var index = 1; index < rows.Count; index++)
        {
            if (Display(rows[index].Values[field]) != value)
            {
                return null;
            }
        }
        return value;
    }

    private static double? ComponentNumber(IReadOnlyList<TableRow> rows, string field)
    {
        var value = RecordValues.Number(rows[0].Values[field]);
        // A class can occur in several rows with conflicting stats. No row is authoritative.
        for (var index = 1; index < rows.Count; index++)
        {
            if (RecordValues.Number(rows[index].Values[field]) != value)
            {
                return null;
            }
        }
        return value;
    }

    private void AddEffects(List<Fact> facts, JsonNode? potion)
    {
        var effects = RecordValues.At(potion, "PotionEffects");
        if (effects is not JsonArray array)
        {
            facts.Add(StatVocabulary.Unknown(Stat.Effects));
        }
        else
        {
            for (var index = 0; index < array.Count; index++)
            {
                var number = (index + 1).ToString(CultureInfo.InvariantCulture);
                AddEffect(
                    facts,
                    Stat.Effect,
                    "Effect " + number,
                    Display(array[index]),
                    RecordValues.Number(RecordValues.At(potion, "Effect" + number + "Power")),
                    RecordValues.Number(RecordValues.At(potion, "DurationofEffect" + number))
                );
            }
        }
        foreach (
            var (field, stat) in new[]
            {
                ("AmountofHealthorManatorestore", Stat.RestorationAmount),
                ("Amountofdamagetodealtohealth", Stat.HealthDamage),
                ("Amountofdamagetodealtomana", Stat.ManaDamage),
            }
        )
        {
            var value = RecordValues.Number(RecordValues.At(potion, field));
            if (value != 0)
            {
                facts.Add(StatVocabulary.Number(stat, value));
            }
        }
    }

    private static void AddEffect(
        List<Fact> facts,
        Stat stat,
        string? label,
        string? name,
        double? power,
        double? duration
    )
    {
        // Hide zero-power entries so inherited enum and duration defaults do not become facts.
        if (power == 0)
        {
            return;
        }
        facts.Add(
            StatVocabulary.Effect(
                stat,
                name is null || power is null || duration is null
                    ? null
                    : new Effect(name, power.Value, duration.Value),
                label
            )
        );
    }

    private string? JoinEffects(IEnumerable<JsonNode?> effects)
    {
        var names = effects.Select(Display).ToArray();
        return names.Any(name => name is null) ? null : string.Join(", ", names);
    }
}
