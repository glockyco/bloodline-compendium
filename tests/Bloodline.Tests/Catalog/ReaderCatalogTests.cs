using System.Text.Json.Nodes;
using Bloodline.Catalog;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Tests.Catalog;

public sealed class ReaderCatalogTests
{
    [Fact]
    public void SameNameItemsUseReadableDistinctTypesInRoutesAndLinks()
    {
        var blade = Item("Blade", "Iron Part", "Blade");
        var pommel = Item("Pommel", "Iron Part", "Pommel");
        var pages = new[] { blade, pommel }.Select(item => new ItemFacts([]).Resolve(item, ""));
        var identities = ItemIdentity.Identities(pages);
        Assert.Equal(("iron-part-blade", "Blade"), identities[blade.Path]);
        Assert.Equal(("iron-part-pommel", "Pommel"), identities[pommel.Path]);
    }

    [Fact]
    public void RecipesAndCurrencyPaymentsProduceReverseRelations()
    {
        var ingredient = Item("Ore", "Copper Ore", "Ore");
        var currency = Item("Coin", "Test Coin", "Coin");
        var output = Item("Sword", "Copper Sword", "Sword");
        var records = new CatalogRecords(
            new BuildRecord(1159290, 1, []),
            [ingredient, currency, output],
            [],
            [],
            [],
            [],
            [],
            []
        );
        var result = CatalogBuilder.Build(
            records,
            [],
            [new Sources(ingredient.Path, currency.Path, output.Path)]
        );
        var ore = result.Items.Single(item => item.Key == ingredient.Path);
        var coin = result.Items.Single(item => item.Key == currency.Path);
        var craft = Assert.Single(ore.Uses);
        Assert.Equal(SourceKind.Crafting, craft.Kind);
        Assert.Equal(3, craft.Quantity);
        Assert.Equal("copper-sword", craft.Item.Slug);
        var payment = Assert.Single(coin.Uses);
        Assert.Equal(SourceKind.CurrencyShop, payment.Kind);
        Assert.Equal(7, payment.Quantity);
        Assert.Equal("copper-sword", payment.Item.Slug);
    }

    [Fact]
    public void ExcludedItemsKeepReadableNamesWithoutLinksToMissingPages()
    {
        var item = Item("Hidden", "Hidden Token", "Token");
        var page = new ItemFacts([]).Resolve(item, "hidden-token");
        var records = new CatalogRecords(
            new BuildRecord(1159290, 1, []),
            [item],
            [],
            [],
            [],
            [],
            [],
            []
        );
        var context = new CatalogContext(
            records,
            new Dictionary<string, ItemPage> { [item.Path] = page },
            [
                new Exclusion(
                    ExclusionKind.Class,
                    item.Path,
                    "Invented exclusion",
                    "Invented evidence"
                ),
            ]
        );
        Assert.Equal(
            new EntityReference(EntityKind.Item, null, "Hidden Token", null, null),
            context.ItemLink(item.Path)
        );
    }

    private static ClassRecord Item(string id, string name, string type) =>
        new(
            $"/Game/Items/{id}.{id}_C",
            [ItemIdentity.BaseItem],
            JsonNode
                .Parse(
                    $$$"""
                    {"ItemInfo":{"ItemName":{"$text":"{{{name}}}"},"ItemType":{"$enum":"Category::Weapon","display":"Weapon"},"SpecificItemType":{"$enum":"Specific::Type","display":"{{{type}}}"},"ItemValue":1}}
                    """
                )!
                .AsObject()
        );

    private sealed class Sources(string ingredient, string currency, string output)
        : ISourceMechanism
    {
        public IEnumerable<ItemSource> FindSources(CatalogContext context)
        {
            yield return new ItemSource(
                output,
                new Source(
                    SourceKind.Crafting,
                    "Copper Recipe",
                    [],
                    null,
                    new Amount(1, 1),
                    null,
                    null,
                    [new ItemAmount(context.ItemLink(ingredient), 3)],
                    []
                )
            );
            yield return new ItemSource(
                output,
                new Source(
                    SourceKind.CurrencyShop,
                    "Test Merchant",
                    [],
                    new Price(7, context.ItemLink(currency)),
                    new Amount(1, 1),
                    null,
                    null,
                    [],
                    []
                )
            );
        }
    }
}
