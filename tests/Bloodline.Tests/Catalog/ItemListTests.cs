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

    [Fact]
    public void AcquisitionFiltersAndPlaceSummariesOmitFileOnlySources()
    {
        var place = new EntityReference(EntityKind.Place, "/Invented/Coast", "Coast", null, null);
        var pickup = new Source(SourceKind.Pickup, "World pickup", [place], null, null, [], []);
        var page = Page("Apple", "food", "Fruit") with
        {
            Sources =
            [
                pickup,
                pickup with
                {
                    Kind = SourceKind.Gathering,
                    Unconfirmed = true,
                },
                pickup with
                {
                    Kind = SourceKind.Dialogue,
                    Places = [place with { Key = "/Invented/Hidden", Name = "Hidden" }],
                    Unconfirmed = true,
                },
            ],
        };
        var facets = ItemList.FacetsOf(page);
        Assert.Equal(["pickup"], facets["source"]);
        Assert.Equal(["Coast"], facets["place"]);
        Assert.Equal(["pickup"], Assert.Single(ItemList.PlaceSourcesOf(page)));
    }

    [Fact]
    public void SpeedWordsSortBySpeedAndAnUnknownFactIsAnUnknownCell()
    {
        ListCell Speed(string word) =>
            ItemList.CellsOf(
                Page("Sword", "weapon", "Sword") with
                {
                    Facts = [StatVocabulary.Text(Stat.Speed, word)],
                }
            )["speed"];
        string[] words = ["Fast", "Slow", "Above Average", "Standard", "Below Average"];
        Assert.Equal(
            ["Slow", "Below Average", "Standard", "Above Average", "Fast"],
            words.OrderBy(word => Speed(word).Number).ToArray()
        );
        Assert.Equal(Speed("Average").Number, Speed("Standard").Number);
        Assert.Equal("Slow", Speed("Slow").Text);
        Assert.DoesNotContain(
            "attackSpeed",
            ItemList.CellsOf(Page("Sword", "weapon", "Sword")).Keys
        );

        var part = Page("Blade", "weapon-part", "Blade") with
        {
            Facts = [StatVocabulary.Unknown(Stat.DamageContribution)],
        };
        Assert.Equal(new ListCell(null, null), ItemList.CellsOf(part)["damageContribution"]);
    }
}
