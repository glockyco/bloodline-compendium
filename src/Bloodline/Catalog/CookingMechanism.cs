using System.Text.Json.Nodes;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

/// <summary>
/// Recipes of the cooking fire. <c>CookingWindow:Check Data Table for Recipe</c> matches the ingredients a player puts
/// in against the cooking rows of <c>ItemRecipes</c>, one ingredient per filled slot, and
/// <c>CookingWindow:Translate Data Table Row to Item Class</c> takes the meal and its experience from the
/// <c>AllFoodRecipes</c> row of the same name. A recipe that only one of the two tables holds cannot be cooked, so it
/// is published as unconfirmed.
/// </summary>
internal sealed class CookingMechanism : ISourceMechanism
{
    public IEnumerable<EntitySource> FindSources(CatalogContext context)
    {
        var cooking = context
            .Records.Tables.Where(table => ItemIdentity.ClassName(table.Path) == "ItemRecipes")
            .SelectMany(table => table.Rows)
            .Where(CraftingMechanism.IsCooking)
            .GroupBy(row => row.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First(),
                StringComparer.OrdinalIgnoreCase
            );
        var meals = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (
            var (table, row) in context
                .Records.Tables.Where(table =>
                    ItemIdentity.ClassName(table.Path) == "AllFoodRecipes"
                )
                .SelectMany(table => table.Rows.Select(row => (table, row)))
        )
        {
            if (RecordValues.AnyReference(row.Values["CompletedMeal"]) is not { } item)
            {
                continue;
            }
            meals.Add(row.Name);
            var matched = cooking.TryGetValue(row.Name, out var slots);
            var name =
                RecordValues.Text(row.Values["LocalizedName"])
                ?? RecordValues.String(row.Values["RecipeName"])
                ?? context.ItemLink(item).Name;
            yield return Recipe(
                context,
                item,
                table.Path + ":" + row.Name,
                name,
                matched ? Slots(slots!) : RecordValues.Elements(row.Values["RequiredItems"]),
                RecordValues.Number(row.Values["EXPReward"]),
                unconfirmed: !matched
            );
        }
        // A cooking row without a meal row matches the ingredients but names nothing to cook. Its item is the class
        // that the crafting window's mapping names for the row.
        var mapped = CraftingMechanism
            .HandMapped(context)
            .GroupBy(mapping => mapping.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First().Class,
                StringComparer.OrdinalIgnoreCase
            );
        foreach (var (rowName, row) in cooking)
        {
            if (meals.Contains(rowName) || !mapped.TryGetValue(rowName, out var item))
            {
                continue;
            }
            var path = context.Records.Tables.First(table => table.Rows.Contains(row)).Path;
            yield return Recipe(
                context,
                item,
                path + ":" + row.Name,
                context.ItemLink(item).Name,
                Slots(row),
                null,
                unconfirmed: true
            );
        }
    }

    /// <summary>The filled ingredient slots of a cooking row. The cooking window ignores the quantity fields.</summary>
    private static IEnumerable<JsonNode?> Slots(TableRow row) =>
        Enumerable.Range(1, 4).Select(slot => row.Values[$"Item{slot}"]);

    private static EntitySource Recipe(
        CatalogContext context,
        string item,
        string key,
        string name,
        IEnumerable<JsonNode?> ingredients,
        double? experience,
        bool unconfirmed
    )
    {
        var inputs = ingredients
            .Select(RecordValues.AnyReference)
            .OfType<string>()
            .GroupBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(group => new ItemAmount(context.ItemLink(group.Key), group.Count()))
            .ToArray();
        return new EntitySource(
            item,
            new Source(
                SourceKind.Cooking,
                name,
                [],
                null,
                new Amount(1, 1),
                inputs,
                unconfirmed
                    ? ["The recipe exists, but the Cooking Fire has no working way to make it."]
                    : []
            )
            {
                Entity = WorldSourceData.Reference(EntityKind.Recipe, key, name),
                Unconfirmed = unconfirmed,
                Recipe = new Recipe(
                    Stations.For("Cooking"),
                    Professions.Reference("Cooking"),
                    null,
                    experience,
                    1,
                    null
                ),
            }
        );
    }
}
