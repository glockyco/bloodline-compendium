using System.Globalization;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed class CookingMechanism : ISourceMechanism
{
    public IEnumerable<ItemSource> FindSources(CatalogContext context)
    {
        foreach (
            var row in context
                .Records.Tables.Where(table =>
                    ItemIdentity.ClassName(table.Path) == "AllFoodRecipes"
                )
                .SelectMany(table => table.Rows)
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
            IReadOnlyList<string> notes = RecordValues.Number(row.Values["EXPReward"])
                is { } experience
                ? [$"Cooking experience: {experience.ToString(CultureInfo.InvariantCulture)}."]
                : [];
            yield return new ItemSource(
                item,
                new Source(
                    SourceKind.Cooking,
                    RecordValues.Text(row.Values["LocalizedName"])
                        ?? RecordValues.String(row.Values["RecipeName"])
                        ?? row.Name,
                    [],
                    null,
                    new Amount(1, 1),
                    null,
                    null,
                    inputs,
                    notes
                )
            );
        }
    }
}
