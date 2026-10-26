using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed class CookingMechanism : ISourceMechanism
{
    public IEnumerable<ItemSource> FindSources(CatalogContext context)
    {
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
            var inputs = RecordValues
                .Elements(row.Values["RequiredItems"])
                .Select(RecordValues.AnyReference)
                .OfType<string>()
                .GroupBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Select(group => new ItemAmount(context.ItemLink(group.Key), group.Count()))
                .ToArray();
            yield return new ItemSource(
                item,
                new Source(
                    SourceKind.Cooking,
                    RecordValues.Text(row.Values["LocalizedName"])
                        ?? RecordValues.String(row.Values["RecipeName"])
                        ?? context.ItemLink(item).Name,
                    [],
                    null,
                    new Amount(1, 1),
                    inputs,
                    []
                )
                {
                    Entity = WorldSourceData.Reference(
                        EntityKind.Recipe,
                        table.Path + ":" + row.Name,
                        RecordValues.Text(row.Values["LocalizedName"])
                            ?? RecordValues.String(row.Values["RecipeName"])
                            ?? context.ItemLink(item).Name
                    ),
                    Recipe = new Recipe(
                        Stations.For("Cooking"),
                        "Cooking",
                        null,
                        RecordValues.Number(row.Values["EXPReward"]),
                        1,
                        null
                    ),
                }
            );
        }
    }
}
