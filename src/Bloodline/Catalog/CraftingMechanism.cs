using System.Globalization;
using Bloodline.Code;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed class CraftingMechanism : ISourceMechanism
{
    /// <summary>
    /// Recipes whose component row names another item than its class makes. The forge spawns the row's class and
    /// names the item after the crafting window's text, the row's name (<c>BlacksmithingMinigame</c>'s ubergraph),
    /// so such a recipe makes an item that looks like the class's item but carries the row's name, such as an Angled
    /// Steel Blade built on the Angled Mystcopper Blade. It is no source of the class's item.
    /// </summary>
    public static IReadOnlyList<(string Recipe, string Name, string Item)> RenamedProducts(
        CatalogContext context
    )
    {
        var recipes = Recipes(context).ToArray();
        // A class that one row makes is that row's item under the row's spelling, so only a class that several
        // rows share can make an item that no page describes.
        var shared = recipes
            .GroupBy(recipe => recipe.Item, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return recipes
            .Where(recipe =>
                shared.Contains(recipe.Item)
                && recipe.Name is { } name
                && !SameName(name, ClassItemName(context, recipe.Item))
            )
            .Select(recipe => (recipe.Row.Name, recipe.Name!, recipe.Item))
            .ToArray();
    }

    /// <summary>
    /// Whether a row names the class's item, allowing two letter edits because rows and items misspell each other,
    /// such as the Silver Gothic Guard row and the Silver Gothic Gaurd item.
    /// </summary>
    internal static bool SameName(string name, string? item)
    {
        if (item is null)
        {
            return false;
        }
        var left = name.ToLowerInvariant();
        var right = item.ToLowerInvariant();
        // Optimal string alignment distance, where swapping two neighbouring letters is one edit.
        var distance = new int[left.Length + 1, right.Length + 1];
        for (var i = 0; i <= left.Length; i++)
        {
            distance[i, 0] = i;
        }
        for (var j = 0; j <= right.Length; j++)
        {
            distance[0, j] = j;
        }
        for (var i = 1; i <= left.Length; i++)
        {
            for (var j = 1; j <= right.Length; j++)
            {
                var cost = left[i - 1] == right[j - 1] ? 0 : 1;
                distance[i, j] = Math.Min(
                    Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                    distance[i - 1, j - 1] + cost
                );
                if (i > 1 && j > 1 && left[i - 1] == right[j - 2] && left[i - 2] == right[j - 1])
                {
                    distance[i, j] = Math.Min(distance[i, j], distance[i - 2, j - 2] + 1);
                }
            }
        }
        return distance[left.Length, right.Length] <= 2;
    }

    private static string? ClassItemName(CatalogContext context, string item) =>
        context.Classes.TryGetValue(item, out var type)
            ? RecordValues.Text(RecordValues.At(type.Properties, "ItemInfo.ItemName"))
            : null;

    /// <summary>The function whose class comparisons name the recipe row of each item the crafting window makes.</summary>
    private const string CorrectItem = "/Game/BloodlinesAlpha/Crafting:Get Correct Item";

    /// <summary>
    /// The recipe row of each item class that <c>Crafting:Get Correct Item</c> maps by hand, such as IronIngot for
    /// Iron Ingot, read from the build's code index.
    /// </summary>
    internal static IReadOnlyList<ClassName> HandMapped(CatalogContext context) =>
        context
            .Records.Code?.Functions.FirstOrDefault(function => function.Citation == CorrectItem)
            ?.ClassNames
        ?? [];

    /// <summary>
    /// A cooking row of the recipe table. The cooking fire opens <c>CookingWindow</c>, not the crafting window, so
    /// <see cref="CookingMechanism"/> publishes these rows.
    /// </summary>
    internal static bool IsCooking(TableRow row) =>
        (
            RecordValues.EnumDisplay(row.Values["CraftingType"])
            ?? RecordValues.EnumValue(row.Values["CraftingType"])
        )
            is "Cooking"
                or "CraftingTypes::NewEnumerator6";

    /// <summary>Hand-mapped items whose recipe row the recipe table lacks, which the crafting window cannot make.</summary>
    public static IReadOnlyList<ClassName> MissingRows(CatalogContext context)
    {
        var rows = RecipeRows(context)
            .Select(pair => pair.Row.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return HandMapped(context).Where(mapping => !rows.Contains(mapping.Name)).ToArray();
    }

    private static IEnumerable<(TableRecord Table, TableRow Row)> RecipeRows(
        CatalogContext context
    ) =>
        context
            .Records.Tables.Where(table => ItemIdentity.ClassName(table.Path) == "ItemRecipes")
            .SelectMany(table => table.Rows.Select(row => (table, row)));

    /// <summary>
    /// Every recipe with the item it makes: the class of the weapon component row of the same name, or else the class
    /// that <c>Crafting:Get Correct Item</c> maps to the row. <see cref="Output"/> is the component row, if any.
    /// </summary>
    private static IEnumerable<(
        TableRecord Table,
        TableRow Row,
        TableRow? Output,
        string Item,
        string? Name
    )> Recipes(CatalogContext context)
    {
        var outputs = context
            .Records.Tables.Where(table =>
                ItemIdentity.ClassName(table.Path) == "WeaponComponentTable"
            )
            .SelectMany(table => table.Rows)
            .ToDictionary(row => row.Name, StringComparer.OrdinalIgnoreCase);
        var mapped = HandMapped(context)
            .GroupBy(mapping => mapping.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First().Class,
                StringComparer.OrdinalIgnoreCase
            );
        foreach (var (table, row) in RecipeRows(context))
        {
            if (RecordValues.Integer(row.Values["CraftAmount"]) is not > 0 || IsCooking(row))
            {
                continue;
            }
            if (
                outputs.TryGetValue(row.Name, out var output)
                && RecordValues.AnyReference(output.Values["Class"]) is { } item
            )
            {
                yield return (
                    table,
                    row,
                    output,
                    item,
                    RecordValues.Text(output.Values["ItemName"])
                );
            }
            else if (mapped.TryGetValue(row.Name, out var handMapped))
            {
                yield return (table, row, null, handMapped, null);
            }
        }
    }

    /// <summary>
    /// The crafting window that the anvil, the engineer's workbench and the smelting oven open. Its class lists what
    /// each profession's window offers in seven tiers, which no recipe row holds.
    /// </summary>
    private const string CraftingWindow = "/Game/BloodlinesAlpha/Crafting.Crafting_C";

    /// <summary>
    /// The window's tier lists per profession, with the function that shows them. Each function opens tier N from the
    /// profession's level N, and tier 1 from the start (run tiers1).
    /// </summary>
    private static readonly (string Profession, string List)[] TierLists =
    [
        ("Weaponsmithing", "WeaponsmithingLevel{0}"),
        ("Engineering", "Engineer Level {0}"),
        ("Smelting", "Smelting Level {0}"),
    ];

    private const int Tiers = 7;

    /// <summary>
    /// The profession and level at which the crafting window offers each item it lists, or null without the window's
    /// record. An item in two tiers needs the lower one.
    /// </summary>
    private static Dictionary<string, (string Profession, int Level)>? WindowTiers(
        CatalogContext context
    )
    {
        if (!context.Classes.TryGetValue(CraftingWindow, out var window))
        {
            return null;
        }
        var tiers = new Dictionary<string, (string, int)>(StringComparer.OrdinalIgnoreCase);
        foreach (var (profession, list) in TierLists)
        {
            for (var tier = Tiers; tier >= 1; tier--)
            {
                var name = string.Format(CultureInfo.InvariantCulture, list, tier);
                foreach (var entry in RecordValues.Elements(window.Properties[name]))
                {
                    if (RecordValues.AnyReference(entry) is { } path)
                    {
                        tiers[path] = (profession, tier);
                    }
                }
            }
        }
        return tiers;
    }

    public IEnumerable<EntitySource> FindSources(CatalogContext context)
    {
        var window = WindowTiers(context);
        var renamed = RenamedProducts(context)
            .Select(recipe => recipe.Recipe)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        // Rows such as SteelRapierGuard and SteelJianGuard make one item from the same inputs under the same name, which
        // the player sees as one recipe.
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (table, row, output, item, name) in Recipes(context))
        {
            if (renamed.Contains(row.Name))
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
            var notes = new List<string>();
            var conditions = new List<RelationCondition>();
            var typeName =
                RecordValues.EnumDisplay(row.Values["CraftingType"])
                ?? (
                    type switch
                    {
                        "CraftingTypes::NewEnumerator0" => "Weaponsmithing",
                        "CraftingTypes::NewEnumerator1" => "Armorsmithing",
                        "CraftingTypes::NewEnumerator2" => "Bow Crafting",
                        "CraftingTypes::NewEnumerator3" => "Runecrafting",
                        "CraftingTypes::NewEnumerator4" => "Smelting",
                        "CraftingTypes::NewEnumerator5" => "Engineering",
                        "CraftingTypes::NewEnumerator6" => "Cooking",
                        "CraftingTypes::NewEnumerator7" => "Necromancy",
                        "CraftingTypes::NewEnumerator8" => "Alchemy",
                        _ => null,
                    }
                );
            // A whole item that the crafting window lists is made where its tier list says, at the tier's level, even
            // when its row names another type, as Rune-Tempered Gear's row names Weaponsmithing. One that the window
            // does not list cannot be made there.
            var listed =
                output is null && window?.TryGetValue(item, out var found) == true
                    ? found
                    : default;
            var unlisted =
                output is null
                && window is not null
                && listed.Profession is null
                && typeName is not null
                && TierLists.Any(entry => entry.Profession == typeName);
            if (unlisted && Stations.For(typeName) is { } station)
            {
                notes.Add($"The recipe exists, but the {station.Name} does not offer it.");
            }
            if (listed.Profession is not null)
            {
                typeName = listed.Profession;
            }
            var smithing = listed.Profession is { } profession
                ? profession == "Weaponsmithing"
                : type is "Weaponsmithing" or "Armorsmithing"
                    || type is "CraftingTypes::NewEnumerator0" or "CraftingTypes::NewEnumerator1";
            var part = output is null ? null : RecordValues.EnumValue(output.Values["Part"]);
            // Magical components use focus crafting despite the shared table field's name.
            var focus =
                part
                is "WeaponParts::NewEnumerator17"
                    or "WeaponParts::NewEnumerator18"
                    or "WeaponParts::NewEnumerator19";
            var requiredLevel =
                output is not null
                    ? RecordValues.Integer(output.Values["WeaponsmithingLevelRequired"])
                : listed.Profession is not null ? listed.Level
                : null;
            if (requiredLevel is { } level)
            {
                var skill =
                    output is null ? listed.Profession
                    : focus ? "Focus Crafting"
                    : part is null ? null
                    : "Weaponsmithing";
                conditions.Add(
                    new(
                        skill is null ? ConditionKind.Unknown : ConditionKind.ProficiencyLevel,
                        skill is null ? null : Professions.Reference(skill),
                        level,
                        skill is null
                            ? $"Requires skill level {level}"
                            : $"Requires {skill} level {level}"
                    )
                );
            }
            else if (output?.Values.ContainsKey("WeaponsmithingLevelRequired") == true)
            {
                conditions.Add(
                    new(
                        ConditionKind.Unknown,
                        null,
                        null,
                        "The required component skill level could not be read"
                    )
                );
            }
            var amount = checked((int)RecordValues.Integer(row.Values["CraftAmount"])!.Value);
            var identity = string.Join(
                "|",
                [
                    item,
                    name,
                    typeName,
                    requiredLevel?.ToString(CultureInfo.InvariantCulture),
                    amount.ToString(CultureInfo.InvariantCulture),
                    RecordValues
                        .Number(row.Values["EXPrewardforcrafting"])
                        ?.ToString(CultureInfo.InvariantCulture),
                    .. inputs
                        .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                        .Select(pair => pair.Key + "*" + pair.Value),
                ]
            );
            if (!seen.Add(identity))
            {
                continue;
            }
            yield return new EntitySource(
                item,
                new Source(
                    smithing ? SourceKind.Smithing : SourceKind.Crafting,
                    name ?? context.ItemLink(item).Name,
                    [],
                    null,
                    new Amount(amount, amount),
                    inputs
                        .Select(pair => new ItemAmount(context.ItemLink(pair.Key), pair.Value))
                        .ToArray(),
                    notes
                )
                {
                    Entity = WorldSourceData.Reference(
                        EntityKind.Recipe,
                        table.Path + ":" + row.Name,
                        name ?? context.ItemLink(item).Name
                    ),
                    Conditions = conditions,
                    Unconfirmed = unlisted,
                    Recipe = new Recipe(
                        Stations.For(focus ? "Focus crafting" : typeName),
                        focus ? Professions.Reference("Focus crafting")
                            : typeName is null ? null
                            : Professions.Reference(typeName),
                        requiredLevel is >= 0 and <= int.MaxValue ? (int)requiredLevel.Value : null,
                        RecordValues.Number(row.Values["EXPrewardforcrafting"]),
                        amount,
                        null
                    ),
                }
            );
        }
    }
}
