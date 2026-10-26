using System.Text.Json.Nodes;
using Bloodline.Catalog;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Tests.Catalog;

public sealed class TradeMechanismTests
{
    private static JsonObject Properties(string json) => JsonNode.Parse(json)!.AsObject();

    private static ClassRecord Item(string name, bool defaultsNameClass = true) =>
        new(
            $"/Game/Invented/{name}.{name}_C",
            [ItemIdentity.BaseItem],
            Properties(
                $$$"""{"ItemInfo":{"ItemName":{"$text":"{{{name}}}"},"ItemValue":17,"ItemClass":{{{(
                    defaultsNameClass ? $$"""{"$ref":"/Game/Invented/{{name}}.{{name}}_C"}""" : "null"
                )}}}}}"""
            )
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
        var pages = CatalogBuilder.Build(records, [], []).Items.ToDictionary(item => item.Key);
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
                    $$$"""{"Class":{"$ref":"{{{output.Path}}}"},"WeaponsmithingLevelRequired":9,"Part":{"$enum":"WeaponParts::NewEnumerator3"}}"""
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
        Assert.Equal("OddOutput", source.Source.Name);
        Assert.Equal(new Amount(4, 4), source.Source.Amount);
        Assert.Equal(5, Assert.Single(source.Source.Inputs).Quantity);
        Assert.Contains(
            source.Source.Conditions,
            condition =>
                condition.Kind == ConditionKind.ProficiencyLevel
                && condition.Value == 9
                && condition.Label.Contains("weaponsmithing")
        );
        Assert.Contains(
            source.Source.Notes,
            note => note.Contains("experience") && note.Contains("12")
        );
        Assert.Empty(source.Source.Places);
        Assert.Equal(EntityKind.Recipe, source.Source.Entity.Kind);
        Assert.Equal(recipes.Path + ":SharedKey", source.Source.Entity.Key);
        Assert.Equal(input.Path, Assert.Single(source.Source.Inputs).Item.Key);
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
        Assert.Contains(source.Notes, note => note.Contains("Carving"));
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
        Assert.Equal(EntityKind.Recipe, found.Source.Entity.Kind);
        Assert.Equal(
            "/Game/Invented/AllFoodRecipes.AllFoodRecipes:Recipe",
            found.Source.Entity.Key
        );
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
        Assert.Equal("/Game/Invented/Town", Assert.Single(found.Source.Places).Key);
    }

    [Fact]
    public void SharedMerchantClassesUseDialogueIdentityNotTheirDisplayName()
    {
        var stock = Item("Stock");
        var vendor = new ClassRecord(
            "/Game/Invented/ShopKeeper_Base.ShopKeeper_Base_C",
            [],
            Properties(
                $$"""{"Full Name":{"$text":"Not a seasonal name"},"Dialogue":{"$ref":"/Game/Invented/ShopTalk.ShopTalk"},"Shop Contents":[{"$ref":"{{stock.Path}}"}]}"""
            )
        );
        var level = new LevelRecord(
            "/Game/Invented/Town",
            [],
            [new ActorRecord("Vendor1", vendor.Path, new JsonObject())]
        );
        var source = Assert
            .Single(new ShopMechanism().FindSources(Context([stock, vendor], levels: [level])))
            .Source;
        Assert.Equal(EntityKind.Merchant, source.Entity.Kind);
        Assert.Equal("/Game/Invented/ShopTalk.ShopTalk", source.Entity.Key);
        Assert.Null(source.Entity.Slug);
        Assert.Empty(source.Conditions);
        vendor.Properties["Full Name"] = Properties("""{"$text":"Hallowed Eve Merchant"}""");
        var renamed = Assert
            .Single(new ShopMechanism().FindSources(Context([stock, vendor], levels: [level])))
            .Source;
        Assert.Equal(source.Entity.Key, renamed.Entity.Key);
        Assert.Empty(renamed.Conditions);
        vendor.Properties["Tags"] = new JsonArray("Halloween Decor");
        var tagged = Assert
            .Single(new ShopMechanism().FindSources(Context([stock, vendor], levels: [level])))
            .Source;
        Assert.Contains(
            tagged.Conditions,
            condition =>
                condition.Kind == ConditionKind.Unknown
                && condition.Label.Contains("Halloween Decor", StringComparison.Ordinal)
        );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WoodhurstTaggedMerchantCarriesTheActualCalendarWindows(bool customCurrency)
    {
        var stock = Item("SeasonalStock");
        var vendor = new ClassRecord(
            "/Game/Invented/ShopKeeper_Base.ShopKeeper_Base_C",
            [],
            Properties(
                """
                {"Full Name":{"$text":"A different display name"},"Dialogue":{"$ref":"/Game/Invented/SeasonalTalk.SeasonalTalk"}}
                """
            )
        );
        if (!customCurrency)
            vendor.Properties["Shop Contents"] = new JsonArray(
                new JsonObject { ["$ref"] = stock.Path }
            );
        var shop = new InnerObjectRecord(
            "/Game/Invented/SeasonalTalk.SeasonalTalk:Open",
            "/Game/Invented/OpenCustomCurrencyShop.OpenCustomCurrencyShop_C",
            Properties(
                $$$"""{"Shop Contents (Items)":{"$map":[[{"$ref":"{{{stock.Path}}}"},10]]}}"""
            )
        );
        var dialogue = new AssetRecord(
            "/Game/Invented/SeasonalTalk.SeasonalTalk",
            "Dialogue",
            Properties($$"""{"Data":[{"Events":[{"$ref":"{{shop.Path}}"}]}]}"""),
            [shop]
        );
        var level = new LevelRecord(
            "/Game/Cities/Woodhurst/Woodhurst",
            [],
            [
                new ActorRecord(
                    "SeasonalVendor",
                    vendor.Path,
                    Properties("""{"Tags":["Halloween Decor"]}""")
                ),
            ]
        );
        var context = Context([stock, vendor], levels: [level], dialogues: [dialogue]);
        var source = Assert
            .Single(
                customCurrency
                    ? new CurrencyShopMechanism().FindSources(context)
                    : new ShopMechanism().FindSources(context)
            )
            .Source;
        var condition = Assert.Single(source.Conditions);
        Assert.Equal(ConditionKind.CalendarEvent, condition.Kind);
        Assert.Equal("Hallowed Eve", condition.Entity!.Name);
        Assert.Equal(EntityKind.CalendarEvent, condition.Entity.Kind);
        Assert.Null(condition.Entity.Key);
        Assert.Null(condition.Entity.Slug);
        Assert.Contains("Truill", condition.Label);
        Assert.Contains("October", condition.Label);
    }

    [Theory]
    [InlineData("InternalCurrencyCounter", true)]
    [InlineData("MissingCounter", false)]
    [InlineData(null, false)]
    public void CurrencyPricesAreNotQuantitiesAndEventDefaultsAreMerged(
        string? achievement,
        bool named
    )
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
                $$$"""{"Currency Item Type":{"$ref":"{{{currency.Path}}}"},"Currency Tracked Via Achievements?":true,"Currency Achievement Name":"InternalCurrencyCounter","Shop Contents (Items)":{"$map":[[{"$ref":"{{{stock.Path}}}"},8],[null,4]]}}"""
            )
        );
        eventClass.Properties["Currency Achievement Name"] = achievement;
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
            tables:
            [
                Table(
                    "AllAchievements_DataTable",
                    new TableRow(
                        "UnrelatedAchievement",
                        Properties("""{"LocalizableDisplayName":{"$text":"Unrelated title"}}""")
                    ),
                    new TableRow(
                        "CurrencyAchievement",
                        Properties(
                            """{"ID":"InternalCurrencyCounter","LocalizableDisplayName":{"$text":"Earned tokens"}}"""
                        )
                    )
                ),
            ],
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
        Assert.Equal(named, found.Source.Notes.Any(note => note.Contains("Earned tokens")));
        Assert.Equal(!named, found.Source.Notes.Any(note => note.Contains("unknown")));
        Assert.DoesNotContain(
            found.Source.Notes,
            note =>
                note.Contains("InternalCurrencyCounter")
                || note.Contains("MissingCounter")
                || note.Contains("Unrelated title")
        );
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
    }

    [Theory]
    [InlineData("/Game/AdvancedLocomotionV3/Elderglen_Improved", true)]
    [InlineData("/Game/Cities/Woodhurst/WoodhurstUnderground", false)]
    [InlineData("/Game/Overworld/OpenWorldAreas/Cortona", false)]
    public void MerchantPricesTradeByTheRespectOfTheirCity(string level, bool respect)
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
                    level,
                    [],
                    [new ActorRecord("Keeper", vendor.Path, new JsonObject())]
                ),
            ]
        );

        var price = Assert.Single(new ShopMechanism().FindSources(context)).Source.Price!;

        Assert.Equal((17, PriceRule.Trading), (price.Amount, price.Rule));
        Assert.Equal(respect ? level : null, price.City?.Key);
    }

    [Theory]
    [InlineData("WizardTower_Finished", "Wizard Tower (settlement)")]
    [InlineData("Fletcher_Finished", "Fletcher (settlement)")]
    [InlineData("Residence_02", "Residence (settlement)")]
    public void ConstructedSettlementStockKeepsUnknownPlacementAndDropsUnnamedItems(
        string buildingClass,
        string name
    )
    {
        var item = Item("Wand");
        var unnamed = Item("Bracers", defaultsNameClass: false);
        var building = new ClassRecord(
            $"/Game/Invented/Construction_Buildings/Buildings_Finished/{buildingClass}.{buildingClass}_C",
            [],
            Properties(
                $$"""{"Vendor Contents":[{"$ref":"{{item.Path}}"},{"$ref":"{{unnamed.Path}}"}]}"""
            )
        );
        // The shopkeeper never offers an item whose default item info names no class.
        var found = Assert.Single(
            new ShopMechanism().FindSources(Context([item, unnamed, building]))
        );
        Assert.Equal(item.Path, found.ItemClass);
        Assert.Equal(name, found.Source.Name);
        Assert.Equal(new Amount(1, 1), found.Source.Amount);
        Assert.Equal(
            (17, PriceRule.Trading, null),
            (found.Source.Price!.Amount, found.Source.Price.Rule, found.Source.Price.City)
        );
        Assert.Empty(found.Source.Places);
        var condition = Assert.Single(found.Source.Conditions);
        Assert.Equal(ConditionKind.Building, condition.Kind);
        Assert.Equal(building.Path, condition.Entity!.Key);
    }

    [Theory]
    [InlineData("WeaponParts::NewEnumerator17")]
    [InlineData("WeaponParts::NewEnumerator18")]
    [InlineData("WeaponParts::NewEnumerator19")]
    public void MagicalComponentsUseFocusCraftingSkill(string part)
    {
        var item = Item("ArcanePart");
        var context = Context(
            [item],
            [
                Table(
                    "ItemRecipes",
                    new TableRow(
                        "Shared",
                        Properties(
                            """{"CraftAmount":1,"CraftingType":{"$enum":"CraftingTypes::NewEnumerator0"}}"""
                        )
                    )
                ),
                Table(
                    "WeaponComponentTable",
                    new TableRow(
                        "Shared",
                        Properties(
                            $$$"""{"Class":{"$ref":"{{{item.Path}}}"},"WeaponsmithingLevelRequired":15,"Part":{"$enum":"{{{part}}}"}}"""
                        )
                    )
                ),
            ]
        );
        var source = Assert.Single(new CraftingMechanism().FindSources(context)).Source;
        Assert.Contains(
            source.Conditions,
            condition =>
                condition.Kind == ConditionKind.ProficiencyLevel
                && condition.Value == 15
                && condition.Label.Contains("focus crafting")
        );
        Assert.DoesNotContain(source.Notes, note => note.Contains("NewEnumerator"));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public void DialogueGrantsKeepAmountsAndOnlyQuoteRecordedText(bool linked, bool player)
    {
        var item = Item("Gift");
        var vendor = new ClassRecord(
            "/Game/Invented/Speaker.Speaker_C",
            [],
            Properties(
                """{"Full Name":{"$text":"Speaker"},"Dialogue":{"$ref":"/Game/Invented/Talk.Talk"}}"""
            )
        );
        var grant = new InnerObjectRecord(
            "/Game/Invented/Talk.Talk:Gift",
            "/Game/Invented/AddItemDialogue.AddItemDialogue_C",
            Properties($$"""{"Item To Add":{"$ref":"{{item.Path}}"},"Amount to Give":3}""")
        );
        var secondGrant = grant with { Path = "/Game/Invented/Talk.Talk:SecondGift" };
        var money = new InnerObjectRecord(
            "/Game/Invented/Talk.Talk:Money",
            "/Game/Invented/PlayerHasEnoughMoney.PlayerHasEnoughMoney_C",
            Properties("""{"Required Money":5}""")
        );
        var node = Properties(
            """{"Text":{"$text":"Take this gift."},"Conditions":[{"$ref":"/Game/Invented/Talk.Talk:Money"}]}"""
        );
        node["isPlayer"] = player;
        node["Events"] = linked
            ? JsonNode.Parse(
                """[{"$ref":"/Game/Invented/Talk.Talk:Gift"},{"$ref":"/Game/Invented/Talk.Talk:SecondGift"}]"""
            )
            : new JsonArray();
        var dialogue = new AssetRecord(
            "/Game/Invented/Talk.Talk",
            "Dialogue",
            new JsonObject { ["Data"] = new JsonArray(node) },
            [grant, secondGrant, money]
        );
        var context = Context(
            [item, vendor],
            levels:
            [
                new LevelRecord(
                    "/Game/Invented/Village",
                    [],
                    [new ActorRecord("Speaker", vendor.Path, new JsonObject())]
                ),
            ],
            dialogues: [dialogue]
        );
        var sources = new DialogueGrantMechanism().FindSources(context).ToArray();
        Assert.Equal(2, sources.Length);
        Assert.Single(CatalogContext.MergeSources(sources, context.ExcludedLevels));
        Assert.All(
            sources,
            source =>
                Assert.Contains(
                    source.Source.Notes,
                    note => note.Contains(linked ? "same line gives it 2 times" : "2 lines")
                )
        );
        var found = sources[0];
        Assert.Equal(item.Path, found.ItemClass);
        Assert.Equal(SourceKind.Dialogue, found.Source.Kind);
        Assert.Equal(new Amount(3, 3), found.Source.Amount);
        Assert.Equal("Speaker", found.Source.Name);
        Assert.Equal("/Game/Invented/Village", Assert.Single(found.Source.Places).Key);
        Assert.Null(found.Source.Price);
        Assert.Empty(found.Source.Inputs);
        Assert.DoesNotContain(
            found.Source.Notes,
            note => note.Contains("/Game/") || note.Contains(":Gift")
        );
        Assert.Equal(linked ? [new Quote("Take this gift.", player)] : [], found.Source.Quotes);
        Assert.Equal(
            linked,
            found.Source.Conditions.Any(condition =>
                condition.Kind == ConditionKind.GameState
                && condition.Value == 5
                && condition.Label.Contains("gold")
            )
        );
    }
}
