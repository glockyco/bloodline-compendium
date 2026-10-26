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

    private readonly HashSet<string> ignoredFoodEffects = new(StringComparer.Ordinal);

    /// <summary>Foods whose effect row lists an effect name that the food manager's switch does not apply.</summary>
    public IReadOnlySet<string> IgnoredFoodEffects => ignoredFoodEffects;

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

    // Null when the records hold no such table, so that its facts stay unknown instead of empty.
    private readonly RowLookup? armorRows = RowLookup.Find(tables, "ArmorDataTable");
    private readonly RowLookup? foodRows = RowLookup.Find(tables, "FoodEffectDataTable");

    // PlayerPassiveLevelStatComponent:Retrieve Player Armor adds up the armor of these item slots. Items of the
    // slot Back go into the character sheet's arms slot. Amulets and support items add no armor.
    private static readonly HashSet<string> ArmoredSlots = new(StringComparer.Ordinal)
    {
        "Head",
        "Torso",
        "Back",
        "Arms",
        "Legs",
        "Knees",
        "Feet",
        "Shoulders",
    };

    // Get Armor Slot gives these crafted armor classes their own ItemInfo.Damage and the class Custom.
    private static readonly HashSet<string> CraftedArmor = new(StringComparer.Ordinal)
    {
        "/Game/BloodlinesAlpha/Interactables/Armor/Arms/CustomArm_Armor_Base.CustomArm_Armor_Base_C",
        "/Game/BloodlinesAlpha/Interactables/Armor/Feet/Custom_Foot_Armor_Base.Custom_Foot_Armor_Base_C",
        "/Game/BloodlinesAlpha/Interactables/Armor/Head/CustomArmor_Base_Head.CustomArmor_Base_Head_C",
        "/Game/BloodlinesAlpha/Interactables/Armor/Legs/Base_Custom_Leg_Armor.Base_Custom_Leg_Armor_C",
        "/Game/BloodlinesAlpha/Interactables/Armor/Shoulders/CustomShoulderArmor.CustomShoulderArmor_C",
        "/Game/BloodlinesAlpha/Interactables/Armor/Torso/CustomArmor_Torso_Base.CustomArmor_Torso_Base_C",
    };

    private enum FoodValue
    {
        Heal,
        Amount,
        Seconds,
        Unused,
    }

    // FoodManager:Get Item's switch over effect names, with how the function it calls uses the row's value. The
    // two EXP boosts also pass a fixed multiplier of 1.25. Names are FNames, which compare without regard to case.
    private static readonly Dictionary<
        string,
        (string Name, FoodValue Value, double? Power)
    > FoodEffects = new[]
    {
        ("Heal Player", FoodValue.Heal, (double?)null),
        ("Boost Woodcutting EXP", FoodValue.Seconds, 1.25),
        ("Druidsight", FoodValue.Seconds, null),
        ("Boost Foraging EXP", FoodValue.Seconds, 1.25),
        ("Drunk", FoodValue.Seconds, null),
        ("Boost Health Regeneration", FoodValue.Seconds, null),
        ("Boost Mana Regeneration", FoodValue.Seconds, null),
        ("Quickness", FoodValue.Seconds, null),
        ("Restore Mana", FoodValue.Amount, null),
        ("Restore Stamina", FoodValue.Amount, null),
        ("Mouse Form", FoodValue.Seconds, null),
        ("Cure Minor Diseases", FoodValue.Unused, null),
        ("Sunshade", FoodValue.Seconds, null),
        ("Cure Sunburn", FoodValue.Unused, null),
        ("Bat Form", FoodValue.Seconds, null),
        ("Bloodfeed", FoodValue.Seconds, null),
        ("Boost Stamina Regeneration", FoodValue.Seconds, null),
    }.ToDictionary(effect => effect.Item1, StringComparer.OrdinalIgnoreCase);

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
        var name = RecordValues.Text(RecordValues.At(properties, "ItemInfo.ItemName"))!;
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
            // Equipping and the ranged weapon checks read this attack type. A shield's None is no weapon type.
            var weaponType = Display(RecordValues.At(properties, "ItemInfo.WeaponType"));
            if (weaponType != "None")
            {
                facts.Add(StatVocabulary.Text(Stat.WeaponType, weaponType));
            }
            // Equipped damage comes from WeaponInfo, not the inventory display's ItemInfo.Damage.
            Add(Stat.Damage, "WeaponInfo.Damage");
            // Tooltips show this speed word, and the equipped attack montage plays at the multiplier.
            Add(Stat.Speed, "ItemInfo.Speed");
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
            AddArmor(facts, item.Path, properties, name);
        }
        if (category == "Food")
        {
            AddFoodEffects(facts, item.Path, name);
        }
        else if (category == "Potion")
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
            name,
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

    /// <summary>
    /// The armor class and rating that equipping the item applies, as Get Armor Slot finds them: crafted armor
    /// keeps its own value, other armor takes the ArmorDataTable row named after the item, and armor without
    /// such a row counts as Unarmored with no armor.
    /// </summary>
    private void AddArmor(List<Fact> facts, string path, JsonObject properties, string name)
    {
        var slot = Display(RecordValues.At(properties, "ItemInfo.ItemSlot"));
        if (slot is null || armorRows is null)
        {
            facts.Add(StatVocabulary.Unknown(Stat.ArmorType));
            facts.Add(StatVocabulary.Unknown(Stat.ArmorRating));
        }
        else if (!ArmoredSlots.Contains(slot))
        {
            facts.Add(StatVocabulary.Number(Stat.ArmorRating, 0));
        }
        else if (CraftedArmor.Contains(path))
        {
            facts.Add(StatVocabulary.Text(Stat.ArmorType, "Custom"));
            Add(Stat.ArmorRating, RecordValues.At(properties, "ItemInfo.Damage"));
        }
        else if (armorRows.Named(name) is { } row)
        {
            facts.Add(StatVocabulary.Text(Stat.ArmorType, Display(row.Values["ArmorClass"])));
            Add(Stat.ArmorRating, row.Values["ArmorValue"]);
        }
        else
        {
            facts.Add(StatVocabulary.Text(Stat.ArmorType, "Unarmored"));
            facts.Add(StatVocabulary.Number(Stat.ArmorRating, 0));
        }

        void Add(Stat stat, JsonNode? value) =>
            facts.Add(StatVocabulary.Number(stat, RecordValues.Number(value)));
    }

    /// <summary>
    /// What eating the food applies, as FoodManager:Get Item finds it: the FoodEffectDataTable row named after
    /// the food, or else the first row whose localized food name is the food's name, with each of its effects
    /// that the manager's switch names. A food without such a row or effect has no effect.
    /// </summary>
    private void AddFoodEffects(List<Fact> facts, string path, string name)
    {
        if (foodRows is null)
        {
            facts.Add(StatVocabulary.Unknown(Stat.Effects));
            return;
        }
        var row =
            foodRows.Named(name)
            ?? foodRows.Rows.FirstOrDefault(row =>
                RecordValues.Text(row.Values["LocalizedFoodName"]) == name
            );
        var count = 0;
        var heals = false;
        foreach (var (key, value) in RecordValues.MapEntries(row?.Values["FoodEffects"]))
        {
            if (
                RecordValues.String(key) is not { } effectName
                || !FoodEffects.TryGetValue(effectName, out var effect)
            )
            {
                ignoredFoodEffects.Add(path);
                continue;
            }
            var amount = RecordValues.Number(value);
            if (effect.Value == FoodValue.Heal)
            {
                facts.Add(StatVocabulary.Number(Stat.RestorationAmount, amount));
                heals = true;
                continue;
            }
            count++;
            facts.Add(
                StatVocabulary.Effect(
                    Stat.Effect,
                    effect.Value switch
                    {
                        FoodValue.Unused => new Effect(effect.Name, null, 0),
                        _ when amount is null => null,
                        FoodValue.Amount => new Effect(effect.Name, amount, 0),
                        _ => new Effect(effect.Name, effect.Power, amount.Value),
                    },
                    "Effect " + count.ToString(CultureInfo.InvariantCulture)
                )
            );
        }
        if (count == 0 && !heals)
        {
            facts.Add(StatVocabulary.Text(Stat.Effects, "None"));
        }
    }

    /// <summary>
    /// A data table's rows in table order, found by row name the way the game finds them for an item: by its
    /// English name without spaces and apostrophes, compared as an FName without regard to case.
    /// </summary>
    private sealed class RowLookup(IReadOnlyList<TableRow> rows)
    {
        private readonly Dictionary<string, TableRow> named = rows.GroupBy(
                row => row.Name,
                StringComparer.OrdinalIgnoreCase
            )
            .ToDictionary(
                group => group.Key,
                group => group.First(),
                StringComparer.OrdinalIgnoreCase
            );

        public IReadOnlyList<TableRow> Rows => rows;

        public TableRow? Named(string itemName) =>
            named.GetValueOrDefault(
                itemName
                    .Replace(" ", "", StringComparison.Ordinal)
                    .Replace("'", "", StringComparison.Ordinal)
            );

        public static RowLookup? Find(IReadOnlyList<TableRecord>? tables, string table)
        {
            var records = (tables ?? [])
                .Where(record => ItemIdentity.ClassName(record.Path) == table)
                .ToArray();
            return records.Length == 0
                ? null
                : new(records.SelectMany(record => record.Rows).ToArray());
        }
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
