using System.Text.Json.Nodes;
using Bloodline.Catalog;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Tests.Catalog;

public sealed class TradeMechanismTests
{
    private static JsonObject Properties(string json) => JsonNode.Parse(json)!.AsObject();

    private static ClassRecord Item(string name) =>
        new(
            $"/Game/Invented/{name}.{name}_C",
            [ItemIdentity.BaseItem],
            Properties($$$"""{"ItemInfo":{"ItemName":{"$text":"{{{name}}}"},"ItemValue":17}}""")
        );

    private static CatalogContext Context(
        ClassRecord[] classes,
        TableRecord[]? tables = null,
        LevelRecord[]? levels = null,
        AssetRecord[]? dialogues = null
    )
    {
        var records = new CatalogRecords(
            new BuildRecord(1159290, 1, []),
            classes,
            tables ?? [],
            [],
            levels ?? [],
            dialogues ?? [],
            [],
            []
        );
        var pages = CatalogBuilder.Build(records, [], []).Items.ToDictionary(item => item.Id);
        return new CatalogContext(records, pages, []);
    }

    private static TableRecord Table(string name, params TableRow[] rows) =>
        new($"/Game/Invented/{name}.{name}", null, rows);

    [Fact]
    public void RecipesJoinOnlySharedRowsAndKeepConsumedInputsSeparate()
    {
        var output = Item("OddOutput");
        var input = Item("Ingredient");
        var recipes = Table(
            "ItemRecipes",
            new TableRow(
                "SharedKey",
                Properties(
                    $$"""{"Item1":{"$ref":"{{input.Path}}"},"QtyReqItem1":3,"Item2":{"$ref":"{{input.Path}}"},"QtyReqItem2":2,"CraftAmount":4,"CraftingType":{"$enum":"CraftingTypes::NewEnumerator0","display":"Weaponsmithing"},"EXPrewardforcrafting":12}"""
                )
            ),
            new TableRow("OddOutput", Properties("""{"CraftAmount":1}""")),
            new TableRow("Zero", Properties("""{"CraftAmount":0}"""))
        );
        var components = Table(
            "WeaponComponentTable",
            new TableRow(
                "SharedKey",
                Properties(
                    $$"""{"Class":{"$ref":"{{output.Path}}"},"WeaponsmithingLevelRequired":9}"""
                )
            ),
            new TableRow("Zero", Properties($$$"""{"Class":{"$ref":"{{{input.Path}}}"}}"""))
        );
        var found = new CraftingMechanism()
            .FindSources(Context([input, output], [recipes, components]))
            .ToArray();
        var source = Assert.Single(found);
        Assert.Equal(output.Path, source.ItemClass);
        Assert.Equal(SourceKind.Smithing, source.Source.Kind);
        Assert.Equal(new Amount(4, 4), source.Source.Amount);
        Assert.Equal(5, Assert.Single(source.Source.Inputs).Quantity);
        Assert.Contains("Requires weaponsmithing level 9.", source.Source.Notes);
        Assert.Contains("Crafting experience: 12.", source.Source.Notes);
        Assert.Empty(source.Source.Places);
    }

    [Fact]
    public void NonSmithingJoinedRecipesKeepTheirCraftingType()
    {
        var item = Item("CarvedObject");
        var context = Context(
            [item],
            [
                Table(
                    "ItemRecipes",
                    new TableRow(
                        "Exact",
                        Properties(
                            """{"CraftAmount":2,"CraftingType":{"$enum":"Craft::Carving","display":"Carving"}}"""
                        )
                    )
                ),
                Table(
                    "WeaponComponentTable",
                    new TableRow("Exact", Properties($$$"""{"Class":{"$ref":"{{{item.Path}}}"}}"""))
                ),
            ]
        );
        var source = Assert.Single(new CraftingMechanism().FindSources(context)).Source;
        Assert.Equal(SourceKind.Crafting, source.Kind);
        Assert.Contains("Crafting type: Carving.", source.Notes);
    }

    [Fact]
    public void CookingCountsRepeatedIngredientsWithoutGrantingThem()
    {
        var food = Item("Stew");
        var ingredient = Item("Root");
        var context = Context(
            [food, ingredient],
            [
                Table(
                    "AllFoodRecipes",
                    new TableRow(
                        "Recipe",
                        Properties(
                            $$"""{"CompletedMeal":{"$ref":"{{food.Path}}"},"RequiredItems":[{"$ref":"{{ingredient.Path}}"},null,{"$ref":"{{ingredient.Path}}"}],"RecipeName":"Root stew","EXPReward":3}"""
                        )
                    )
                ),
            ]
        );
        var found = Assert.Single(new CookingMechanism().FindSources(context));
        Assert.Equal(food.Path, found.ItemClass);
        Assert.Equal(2, Assert.Single(found.Source.Inputs).Quantity);
        Assert.Equal(new Amount(1, 1), found.Source.Amount);
        Assert.Equal("Root stew", found.Source.Name);
        Assert.Empty(found.Source.Places);
    }

    [Fact]
    public void ShopsUsePlacedVendorNamesAndTableSelectionInsteadOfPreviewStock()
    {
        var stock = Item("Stock");
        var preview = Item("Preview");
        var vendor = new ClassRecord(
            "/Game/Invented/Vendor.Vendor_C",
            [],
            Properties(
                """{"Full Name":{"$text":"Default name"},"Dialogue":{"$ref":"/Game/Invented/Talk.Talk"}}"""
            )
        );
        var evt = new InnerObjectRecord(
            "/Game/Invented/Talk.Talk:Open",
            "/Game/Invented/OPENdialogueSHOP_New.OPENdialogueSHOP_New_C",
            Properties($$"""{"Shop Name":"Selected","Shop Items":[{"$ref":"{{preview.Path}}"}]}""")
        );
        var dialogue = new AssetRecord(
            "/Game/Invented/Talk.Talk",
            "Dialogue",
            Properties("""{"Data":[{"Events":[{"$ref":"/Game/Invented/Talk.Talk:Open"}]}]}"""),
            [evt]
        );
        var context = Context(
            [stock, preview, vendor],
            [
                Table(
                    "DT_AllShops",
                    new TableRow(
                        "Selected",
                        Properties($$"""{"ShopContents":[{"$ref":"{{stock.Path}}"}]}""")
                    )
                ),
            ],
            [
                new LevelRecord(
                    "/Game/Invented/Town",
                    [],
                    [
                        new ActorRecord(
                            "Vendor1",
                            vendor.Path,
                            Properties("""{"Full Name":{"$text":"Placed name"}}""")
                        ),
                    ]
                ),
            ],
            [dialogue]
        );
        var found = Assert.Single(new ShopMechanism().FindSources(context));
        Assert.Equal(stock.Path, found.ItemClass);
        Assert.Equal("Placed name", found.Source.Name);
        Assert.Equal(17, found.Source.Price!.Amount);
        Assert.Equal("/Game/Invented/Town", Assert.Single(found.Source.Places).Id);
        Assert.Null(found.Source.Weight);
    }

    [Fact]
    public void CurrencyPricesAreNotQuantitiesAndEventDefaultsAreMerged()
    {
        var stock = Item("Traded");
        var currency = Item("Token");
        var vendor = new ClassRecord(
            "/Game/Invented/Trader.Trader_C",
            [],
            Properties(
                """{"Full Name":{"$text":"Trader"},"Dialogue":{"$ref":"/Game/Invented/Exchange.Exchange"}}"""
            )
        );
        var eventClass = new ClassRecord(
            "/Game/Invented/OpenCustomCurrencyShop.OpenCustomCurrencyShop_C",
            [],
            Properties(
                $$$"""{"Currency Item Type":{"$ref":"{{{currency.Path}}}"},"Shop Contents (Items)":{"$map":[[{"$ref":"{{{stock.Path}}}"},8],[null,4]]}}"""
            )
        );
        var evt = new InnerObjectRecord(
            "/Game/Invented/Exchange.Exchange:Open",
            eventClass.Path,
            new JsonObject()
        );
        var dialogue = new AssetRecord(
            "/Game/Invented/Exchange.Exchange",
            "Dialogue",
            Properties(
                """{"Data":[{"Events":[{"$ref":"/Game/Invented/Exchange.Exchange:Open"}]}]}"""
            ),
            [evt]
        );
        var context = Context(
            [stock, currency, vendor, eventClass],
            levels:
            [
                new LevelRecord(
                    "/Game/Invented/Market",
                    [],
                    [new ActorRecord("Trader1", vendor.Path, new JsonObject())]
                ),
            ],
            dialogues: [dialogue]
        );
        var found = Assert.Single(new CurrencyShopMechanism().FindSources(context));
        Assert.Equal(stock.Path, found.ItemClass);
        Assert.Equal(8, found.Source.Price!.Amount);
        Assert.Equal("Token", found.Source.Price.Currency!.Name);
        Assert.Equal(new Amount(1, 1), found.Source.Amount);
        Assert.Empty(found.Source.Inputs);
        evt.Properties["Default Lionsmane Content?"] = true;
        Assert.Empty(new CurrencyShopMechanism().FindSources(context));
    }

    [Fact]
    public void DirectStockMergesPlacedOverridesAndKeepsPlainArraysUnweighted()
    {
        var item = Item("Goods");
        var vendor = new ClassRecord(
            "/Game/Invented/Keeper.Keeper_C",
            [],
            Properties(
                $$$"""{"Full Name":{"$text":"Keeper"},"Shop Contents":[{"ItemClass":{"$ref":"{{{item.Path}}}"}}]}"""
            )
        );
        var context = Context(
            [item, vendor],
            levels:
            [
                new LevelRecord(
                    "/Game/Invented/Village",
                    [],
                    [
                        new ActorRecord("Inherited", vendor.Path, new JsonObject()),
                        new ActorRecord(
                            "Empty",
                            vendor.Path,
                            Properties("""{"Shop Contents":[]}""")
                        ),
                    ]
                ),
            ]
        );
        var found = Assert.Single(new ShopMechanism().FindSources(context));
        Assert.Equal(item.Path, found.ItemClass);
        Assert.Equal("Keeper", found.Source.Name);
        Assert.Null(found.Source.Weight);
        Assert.Null(found.Source.Chance);
    }

    [Fact]
    public void ConstructedSettlementStockKeepsUnknownPlacementVisible()
    {
        var item = Item("Wand");
        var building = new ClassRecord(
            "/Game/Invented/Construction_Buildings/Buildings_Finished/ArcaneHouse.ArcaneHouse_C",
            [],
            Properties($$"""{"Vendor Contents":[{"$ref":"{{item.Path}}"}]}""")
        );
        var found = Assert.Single(new ShopMechanism().FindSources(Context([item, building])));
        Assert.Equal(item.Path, found.ItemClass);
        Assert.Empty(found.Source.Places);
        Assert.Contains("Stock from a constructed settlement building.", found.Source.Notes);
    }
}
