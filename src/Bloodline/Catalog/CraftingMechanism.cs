using System.Globalization;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed class CraftingMechanism : ISourceMechanism
{
    public IEnumerable<ItemSource> FindSources(CatalogContext context)
    {
        var outputs = context
            .Records.Tables.Where(table =>
                ItemIdentity.ClassName(table.Path) == "WeaponComponentTable"
            )
            .SelectMany(table => table.Rows)
            .ToDictionary(row => row.Name, StringComparer.OrdinalIgnoreCase);
        foreach (
            var row in context
                .Records.Tables.Where(table => ItemIdentity.ClassName(table.Path) == "ItemRecipes")
                .SelectMany(table => table.Rows)
        )
        {
            if (
                !outputs.TryGetValue(row.Name, out var output)
                || RecordValues.AnyReference(output.Values["Class"]) is not { } item
                || RecordValues.Integer(row.Values["CraftAmount"]) is not > 0
            )
            {
                continue;
            }
            var inputs = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var slot = 1; slot <= 4; slot++)
            {
                if (
                    RecordValues.AnyReference(row.Values[$"Item{slot}"]) is { } ingredient
                    && RecordValues.Integer(row.Values[$"QtyReqItem{slot}"]) is > 0 and var quantity
                )
                {
                    inputs[ingredient] =
                        inputs.GetValueOrDefault(ingredient) + checked((int)quantity);
                }
            }
            var type =
                RecordValues.EnumDisplay(row.Values["CraftingType"])
                ?? RecordValues.EnumValue(row.Values["CraftingType"]);
            var smithing =
                type is "Weaponsmithing" or "Armorsmithing"
                || type is "CraftingTypes::NewEnumerator0" or "CraftingTypes::NewEnumerator1";
            var notes = new List<string>();
            if (type is not null)
            {
                notes.Add($"Crafting type: {type}.");
            }
            if (RecordValues.Number(row.Values["EXPrewardforcrafting"]) is { } experience)
            {
                notes.Add(
                    $"Crafting experience: {experience.ToString(CultureInfo.InvariantCulture)}."
                );
            }
            if (RecordValues.Integer(output.Values["WeaponsmithingLevelRequired"]) is { } level)
            {
                var part = RecordValues.EnumValue(output.Values["Part"]);
                // Magical components use focus crafting despite the shared table field's name.
                var skill = part switch
                {
                    "WeaponParts::NewEnumerator17"
                    or "WeaponParts::NewEnumerator18"
                    or "WeaponParts::NewEnumerator19" => "focus crafting",
                    null => null,
                    _ => "weaponsmithing",
                };
                notes.Add(
                    skill is null
                        ? $"Required component skill level: {level}. The skill is not recorded."
                        : $"Requires {skill} level {level}."
                );
            }
            var amount = checked((int)RecordValues.Integer(row.Values["CraftAmount"])!.Value);
            yield return new ItemSource(
                item,
                new Source(
                    smithing ? SourceKind.Smithing : SourceKind.Crafting,
                    RecordValues.Text(output.Values["ItemName"]) ?? row.Name,
                    [],
                    null,
                    new Amount(amount, amount),
                    null,
                    null,
                    inputs
                        .Select(pair => new ItemAmount(context.ItemLink(pair.Key), pair.Value))
                        .ToArray(),
                    notes
                )
            );
        }
    }
}
