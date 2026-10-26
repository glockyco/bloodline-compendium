using Bloodline.Catalog;
using Bloodline.SiteData;

namespace Bloodline.Tests.Catalog;

public sealed class ItemListTests
{
    private static ItemPage Page(string name, string category, string type) =>
        new(
            $"/Game/Invented/{name}.{name}_C",
            name.ToLowerInvariant(),
            name,
            null,
            new CategoryLink(category, category),
            null,
            [],
            []
        )
        {
            Type = type,
        };

    [Fact]
    public void FiltersForSomeCategoriesHoldOnlyTheirItemsValues()
    {
        var helm = Page("Helm", "armor", "Head");
        var blade = Page("Blade", "weapon-part", "Blade");
        var apple = Page("Apple", "food", "Fruit");
        var facets = ItemList.FacetDefinitions(
            [helm, blade, apple],
            [
                new CategorySummary("armor", "Armor", "Armor", 1),
                new CategorySummary("food", "Food", "Food", 1),
                new CategorySummary("weapon-part", "Weapon Part", "Weapon Parts", 1),
            ]
        );

        Assert.Equal(["Head"], facets.Single(facet => facet.Id == "armorSlot").Values);
        Assert.Equal(["Blade"], facets.Single(facet => facet.Id == "weaponPart").Values);
        Assert.Equal(["Head"], ItemList.FacetsOf(helm)["armorSlot"]);
        Assert.False(ItemList.FacetsOf(apple).ContainsKey("armorSlot"));
        Assert.False(ItemList.FacetsOf(helm).ContainsKey("weaponPart"));
    }
}
