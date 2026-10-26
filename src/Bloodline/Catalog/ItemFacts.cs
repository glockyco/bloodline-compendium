using System.Globalization;
using System.Text.Json.Nodes;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed class ItemFacts(IReadOnlyList<EnumRecord> enums)
{
    private readonly Dictionary<string, string?> enumDisplays = enums
        .SelectMany(record => record.Entries.Select(entry => (entry.Name, entry.Display)))
        .GroupBy(entry => entry.Name, StringComparer.Ordinal)
        .ToDictionary(group => group.Key, group => group.First().Display, StringComparer.Ordinal);

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
        var facts = new List<Fact>();
        void Add(string label, string path) =>
            facts.Add(new Fact(label, Display(RecordValues.At(properties, path))));
        Add("Value", "ItemInfo.ItemValue");
        if (category is null)
        {
            facts.Add(new Fact("Category", null));
        }
        if (category == "Weapon")
        {
            Add("Type", "ItemInfo.SpecificItemType");
            Add("Weapon type", "WeaponInfo.AttackType");
            // BP_BaseWeapon ExecuteUbergraph L13640 passes WeaponInfo.Damage to ApplyDamage.
            // FirstPersonCharacter2 also reads it into Arrow/Bullet Damage at L188281 and L196673.
            // ItemInfo.Damage is inventory display data, not the equipped weapon's base damage.
            Add("Damage", "WeaponInfo.Damage");
            // FirstPersonCharacter2 L165197 multiplies WeaponInfo.AttackSpeed for the attack montage.
            Add("Attack speed", "WeaponInfo.AttackSpeed");
            Add("Range", "WeaponInfo.Range");
            Add("Damage type", "WeaponInfo.DamageType");
            var magazine = RecordValues.Number(
                RecordValues.At(properties, "WeaponInfo.MagazineSize")
            );
            if (magazine is null || magazine > 0)
            {
                Add("Magazine size", "WeaponInfo.MagazineSize");
            }
        }
        if (category == "Armor")
        {
            // SpecificItemType retains the weapon default on most armour. ItemSlot governs equipment placement.
            Add("Slot", "ItemInfo.ItemSlot");
            Add("Armor type", "ItemInfo.ArmorType");
            // FirstPersonCharacter2's equipment function assigns Head Armor at L3650 and Torso Armor
            // at L6506 from ArmorInfo.DamageRating. TakeDamage uses Total Armor, not ItemInfo.Damage.
            Add("Armor rating", "ArmorInfo.DamageRating");
        }
        if (category is "Food" or "Potion")
        {
            AddEffects(facts, RecordValues.At(properties, "ItemInfo.PotionInfo"));
        }
        if (RecordValues.At(properties, "ItemInfo.ReagentInfo") is JsonObject reagent)
        {
            foreach (var treatment in new[] { "Chopped", "Cooled", "Ground", "Heated" })
            {
                var effects = RecordValues.Elements(reagent[treatment + "Effects"]).ToArray();
                if (effects.Length == 0)
                {
                    continue;
                }
                facts.Add(new Fact(treatment + " effects", JoinEffects(effects)));
                facts.Add(
                    new Fact(
                        treatment + " effect power",
                        Display(reagent[treatment + "EffectPower"])
                    )
                );
                facts.Add(
                    new Fact(
                        treatment + " effect duration",
                        Display(reagent[treatment + "EffectDuration"])
                    )
                );
            }
        }
        return new ItemPage(
            item.Path,
            slug,
            RecordValues.Text(RecordValues.At(properties, "ItemInfo.ItemName"))!,
            RecordValues.Text(RecordValues.At(properties, "ItemInfo.Description")),
            new CategoryLink(ItemIdentity.Slug(category ?? "Unknown"), category ?? "Unknown"),
            null,
            facts,
            []
        );
    }

    private void AddEffects(List<Fact> facts, JsonNode? potion)
    {
        var effects = RecordValues.At(potion, "PotionEffects");
        if (effects is not JsonArray array)
        {
            facts.Add(new Fact("Effects", null));
            return;
        }
        if (array.Count == 0)
        {
            facts.Add(new Fact("Effects", "None"));
            return;
        }
        for (var index = 0; index < array.Count; index++)
        {
            var number = (index + 1).ToString(CultureInfo.InvariantCulture);
            facts.Add(new Fact("Effect " + number, Display(array[index])));
            facts.Add(
                new Fact(
                    "Effect " + number + " power",
                    Display(RecordValues.At(potion, "Effect" + number + "Power"))
                )
            );
            facts.Add(
                new Fact(
                    "Effect " + number + " duration",
                    Display(RecordValues.At(potion, "DurationofEffect" + number))
                )
            );
        }
        foreach (
            var (field, label) in new[]
            {
                ("AmountofHealthorManatorestore", "Restoration amount"),
                ("Amountofdamagetodealtohealth", "Health damage"),
                ("Amountofdamagetodealtomana", "Mana damage"),
            }
        )
        {
            facts.Add(new Fact(label, Display(RecordValues.At(potion, field))));
        }
    }

    private string? JoinEffects(IEnumerable<JsonNode?> effects)
    {
        var names = effects.Select(Display).ToArray();
        return names.Any(name => name is null) ? null : string.Join(", ", names);
    }
}
