using System.Text.Json.Nodes;
using Bloodline.Catalog;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Tests.Catalog;

public sealed class ItemFactsTests
{
    private static JsonObject Parse(string json) => JsonNode.Parse(json)!.AsObject();

    private static IReadOnlyList<Bloodline.SiteData.Fact> Facts(
        string category,
        JsonObject? potion = null,
        JsonObject? weapon = null
    )
    {
        var properties = new JsonObject
        {
            ["ItemInfo"] = new JsonObject
            {
                ["ItemName"] = new JsonObject { ["$text"] = "Invented item" },
                ["ItemType"] = new JsonObject
                {
                    ["$enum"] = "Category::Invented",
                    ["display"] = category,
                },
                ["PotionInfo"] = potion,
            },
            ["WeaponInfo"] = weapon,
        };
        return new ItemFacts([])
            .Resolve(new ClassRecord("/Game/Invented.Invented_C", [], properties), "invented")
            .Facts;
    }

    [Fact]
    public void PotionEffectsCombineNamePowerAndNonzeroDuration()
    {
        var facts = Facts(
            "Potion",
            potion: Parse(
                """
                {"PotionEffects":[{"$enum":"Effects::Health","display":"Restore health"},{"$enum":"Effects::Drunk","display":"Drunk"}],
                 "Effect1Power":50,"DurationofEffect1":0,"Effect2Power":10,"DurationofEffect2":60,
                 "AmountofHealthorManatorestore":50,"Amountofdamagetodealtohealth":0,"Amountofdamagetodealtomana":0}
                """
            )
        );
        Assert.Equal("Restore health 50", facts.Single(fact => fact.Label == "Effect 1").Value);
        Assert.Equal("Drunk 10 for 60 s", facts.Single(fact => fact.Label == "Effect 2").Value);
        Assert.DoesNotContain(
            facts,
            fact =>
                fact.Label.EndsWith(" power", StringComparison.Ordinal)
                || fact.Label.EndsWith(" duration", StringComparison.Ordinal)
        );
        Assert.Equal("50", facts.Single(fact => fact.Label == "Restoration amount").Value);
        Assert.DoesNotContain(facts, fact => fact.Label is "Health damage" or "Mana damage");
    }

    [Fact]
    public void ZeroPowerPotionEffectsAndZeroEffectNumbersAreNotFacts()
    {
        var facts = Facts(
            "Potion",
            potion: Parse(
                """
                {"PotionEffects":[{"$enum":"Effects::Default","display":"Enhance Speed"}],"Effect1Power":0,"DurationofEffect1":60,
                 "AmountofHealthorManatorestore":0,"Amountofdamagetodealtohealth":0,"Amountofdamagetodealtomana":0}
                """
            )
        );
        Assert.DoesNotContain(
            facts,
            fact =>
                fact.Label.StartsWith("Effect", StringComparison.Ordinal)
                || fact.Label is "Restoration amount" or "Health damage" or "Mana damage"
        );
    }

    [Fact]
    public void NonzeroEffectDamageNumbersRemainVisibleEvenWithoutAnEffectArrayEntry()
    {
        var facts = Facts(
            "Potion",
            potion: Parse(
                """
                {"PotionEffects":[],"AmountofHealthorManatorestore":0,"Amountofdamagetodealtohealth":5,"Amountofdamagetodealtomana":-2}
                """
            )
        );
        Assert.Equal("5", facts.Single(fact => fact.Label == "Health damage").Value);
        Assert.Equal("-2", facts.Single(fact => fact.Label == "Mana damage").Value);
        Assert.DoesNotContain(facts, fact => fact.Label == "Effects");
    }

    [Theory]
    [InlineData("Effect1Power")]
    [InlineData("DurationofEffect1")]
    [InlineData("EffectName")]
    public void MissingPotionEffectFieldsKeepTheEffectUnknown(string field)
    {
        var potion = Parse(
            """
            {"PotionEffects":[{"$enum":"Effects::Speed","display":"Enhance Speed"}],"Effect1Power":10,"DurationofEffect1":60}
            """
        );
        if (field == "EffectName")
        {
            potion["PotionEffects"]![0] = null;
        }
        else
        {
            potion.Remove(field);
        }
        Assert.Null(Facts("Potion", potion: potion).Single(fact => fact.Label == "Effect 1").Value);
    }

    [Fact]
    public void MissingEffectNumbersRemainUnknownWhenTheEffectsArrayIsEmpty()
    {
        var facts = Facts("Potion", potion: Parse("""{"PotionEffects":[]}"""));
        foreach (var label in new[] { "Restoration amount", "Health damage", "Mana damage" })
        {
            Assert.Null(facts.Single(fact => fact.Label == label).Value);
        }
    }

    [Fact]
    public void TreatmentsFollowTheIngredientTableAndIgnoreReagentInfo()
    {
        var item = new ClassRecord(
            "/Game/Invented/Leaf.Leaf_C",
            [],
            Parse(
                """
                {"ItemInfo":{"ItemName":{"$text":"Leaf"},"ItemType":{"$enum":"E_ItemType::Etc","display":"Etc."},
                 "ReagentInfo":{"ChoppedEffects":[{"$enum":"Effects::Health","display":"Restore Health"}],"ChoppedEffectPower":75,"ChoppedEffectDuration":0}}}
                """
            )
        );
        var table = new TableRecord(
            "/Game/Invented/AllIngredientEffects.AllIngredientEffects",
            null,
            [
                new TableRow(
                    "Leaf",
                    Parse(
                        """
                        {"Item":{"$ref":"/Game/Invented/Leaf.Leaf_C"},
                         "ChoppedEffect":{"$map":[["Restore Health",35],["Druidsight",90]]},
                         "CooledEffect":{"$map":[["Druidsight",60]]},
                         "GroundEffect":{"$map":[]},
                         "HeatedEffect":{"$map":[["Restore Mana",15]]}}
                        """
                    )
                ),
            ]
        );

        var facts = new ItemFacts([], [table]).Resolve(item, "leaf").Facts;

        Assert.Equal("Restore Health 35", Value(facts, Stat.ChoppedEffect));
        Assert.Equal("Druidsight for 60 s", Value(facts, Stat.CooledEffect));
        Assert.Equal("Restore Mana 15", Value(facts, Stat.HeatedEffect));
        Assert.DoesNotContain(facts, fact => fact.Stat == Stat.GroundEffect);
        Assert.DoesNotContain(
            new ItemFacts([]).Resolve(item, "leaf").Facts,
            fact => fact.Stat == Stat.ChoppedEffect
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonpositiveWeaponRangeAndMagazineAndNoneDamageTypeAreHidden(int value)
    {
        var weapon = Parse(
            $$$"""
            {"Range":{{{value}}},"MagazineSize":{{{value}}},"DamageType":{"$enum":"Damage::Default","display":"None"}}
            """
        );
        Assert.DoesNotContain(
            Facts("Weapon", weapon: weapon),
            fact => fact.Label is "Range" or "Magazine size" or "Damage type"
        );
    }

    [Fact]
    public void PositiveWeaponOptionsRemainVisible()
    {
        var facts = Facts(
            "Weapon",
            weapon: Parse(
                """
                {"MagazineSize":5,"DamageType":{"$enum":"Damage::Pierce","display":"Pierce"}}
                """
            )
        );
        Assert.Equal("5", facts.Single(fact => fact.Label == "Magazine size").Value);
        Assert.Equal("Pierce", facts.Single(fact => fact.Label == "Damage type").Value);
    }

    [Fact]
    public void MissingWeaponOptionsRemainUnknown()
    {
        var facts = Facts("Weapon", weapon: new JsonObject());
        foreach (var label in new[] { "Magazine size", "Damage type" })
        {
            Assert.Null(facts.Single(fact => fact.Label == label).Value);
        }
    }

    private static ClassRecord Item(string? category = "Etc.", string className = "Invented") =>
        new(
            $"/Game/Invented/{className}.{className}_C",
            [],
            new JsonObject
            {
                ["ItemInfo"] = new JsonObject
                {
                    ["ItemName"] = new JsonObject { ["$text"] = "Invented item" },
                    ["ItemType"] = category is null
                        ? null
                        : new JsonObject
                        {
                            ["$enum"] = "InventedCategory::Item",
                            ["display"] = category,
                        },
                    ["ItemValue"] = 9,
                },
            }
        );

    private static TableRow ComponentRow(
        ClassRecord item,
        string fields,
        string name = "InventedPart",
        bool softReference = false
    )
    {
        var values = Parse(fields);
        values["Class"] = new JsonObject { [softReference ? "$soft" : "$ref"] = item.Path };
        return new TableRow(name, values);
    }

    private static TableRecord Components(params TableRow[] rows) =>
        new("/Game/Invented/WeaponComponentTable.WeaponComponentTable", null, rows);

    [Theory]
    [InlineData("Pommel")]
    [InlineData("Blade")]
    [InlineData("Wand Focus")]
    public void ComponentFactsUseTableContributionsInsteadOfInheritedWeaponStats(string part)
    {
        var item = Item("Weapon");
        item.Properties["WeaponInfo"] = Parse("""{"Damage":999,"AttackSpeed":999}""");
        item.Properties["ItemInfo"]!["Damage"] = 888;
        item.Properties["ItemInfo"]!["Speed"] = 888;
        var row = ComponentRow(
            item,
            $$$"""
            {"Part":{"$enum":"InventedParts::Part","display":"{{{part}}}"},"Damage":7.5,"Speed":-0.125}
            """
        );

        var page = new ItemFacts([], [Components(row)]).Resolve(item, "invented");

        Assert.Equal(part, page.Type);
        Assert.DoesNotContain(
            page.Facts,
            fact => fact.Label is "Type" or "Slot" or "Component part"
        );
        Assert.Equal("7.5", page.Facts.Single(fact => fact.Label == "Damage contribution").Value);
        Assert.Equal("-0.125", page.Facts.Single(fact => fact.Label == "Speed contribution").Value);
        Assert.Equal("9", page.Facts.Single(fact => fact.Label == "Value").Value);
        Assert.DoesNotContain(
            page.Facts,
            fact => fact.Label is "Damage" or "Attack speed" or "Weapon type"
        );
    }

    [Fact]
    public void ComponentPartUsesEnumRecordsWhenTheRowHasNoDisplay()
    {
        var item = Item();
        var row = ComponentRow(
            item,
            """{"Part":{"$enum":"InventedParts::EndCap"},"Damage":0,"Speed":0}""",
            softReference: true
        );
        var enums = new[]
        {
            new EnumRecord(
                "/Game/Invented/InventedParts",
                [new EnumEntry("InventedParts::EndCap", 0, "Pommel")]
            ),
        };

        var page = new ItemFacts(enums, [Components(row)]).Resolve(item, "invented");

        Assert.Equal("Pommel", page.Type);
        Assert.Equal("0", page.Facts.Single(fact => fact.Label == "Damage contribution").Value);
        Assert.Equal("0", page.Facts.Single(fact => fact.Label == "Speed contribution").Value);
    }

    [Theory]
    [InlineData("Part", "Component part")]
    [InlineData("Damage", "Damage contribution")]
    [InlineData("Speed", "Speed contribution")]
    public void MissingComponentFieldsRemainUnknown(string field, string label)
    {
        var item = Item();
        var row = ComponentRow(
            item,
            """{"Part":{"$enum":"InventedParts::EndCap","display":"Pommel"},"Damage":3,"Speed":0.25}"""
        );
        row.Values.Remove(field);

        var page = new ItemFacts([], [Components(row)]).Resolve(item, "invented");

        if (field != "Part")
            Assert.Null(page.Facts.Single(fact => fact.Label == label).Value);
        Assert.Equal(field == "Part" ? "Unknown" : "Pommel", page.Type);
    }

    [Fact]
    public void ComponentLookupMatchesClassReferencesWithoutDependingOnRowNames()
    {
        var item = Item();
        var row = ComponentRow(
            item,
            """{"Part":{"$enum":"InventedParts::Edge","display":"Blade"},"Damage":12,"Speed":0.5}""",
            "UnrelatedInventedRecipeName"
        );
        row.Values["Class"] = new JsonObject { ["$ref"] = item.Path.ToUpperInvariant() };
        var unrelated = ComponentRow(
            Item(className: "OtherInventedItem"),
            """{"Part":{"$enum":"InventedParts::EndCap","display":"Pommel"},"Damage":100,"Speed":100}"""
        );

        var page = new ItemFacts([], [Components(unrelated, row)]).Resolve(item, "invented");

        Assert.Equal("Blade", page.Type);
        Assert.Equal("12", page.Facts.Single(fact => fact.Label == "Damage contribution").Value);
    }

    [Fact]
    public void ConflictingComponentRowsKeepOnlyTheFieldsTheyAgreeOn()
    {
        var item = Item();
        var first = ComponentRow(
            item,
            """{"Part":{"$enum":"InventedParts::Edge","display":"Blade"},"Damage":12,"Speed":0.5}""",
            "FirstInventedRecipe"
        );
        var second = ComponentRow(
            item,
            """{"Part":{"$enum":"InventedParts::Edge","display":"Blade"},"Damage":18,"Speed":0.5}""",
            "SecondInventedRecipe"
        );

        foreach (var rows in new[] { new[] { first, second }, new[] { second, first } })
        {
            var page = new ItemFacts([], [Components(rows)]).Resolve(item, "invented");
            Assert.Equal("Blade", page.Type);
            Assert.Null(page.Facts.Single(fact => fact.Label == "Damage contribution").Value);
            Assert.Equal(
                "0.5",
                page.Facts.Single(fact => fact.Label == "Speed contribution").Value
            );
        }
    }

    [Fact]
    public void IdenticalComponentRowsDoNotDuplicateFacts()
    {
        var item = Item();
        const string fields =
            """{"Part":{"$enum":"InventedParts::EndCap","display":"Pommel"},"Damage":2,"Speed":0.1}""";
        var page = new ItemFacts(
            [],
            [Components(ComponentRow(item, fields, "First"), ComponentRow(item, fields, "Second"))]
        ).Resolve(item, "invented");

        Assert.Equal("Pommel", page.Type);
        Assert.DoesNotContain(page.Facts, fact => fact.Label == "Component part");
        Assert.Equal("2", page.Facts.Single(fact => fact.Label == "Damage contribution").Value);
    }

    [Fact]
    public void OtherTablesDoNotDefineWeaponComponentFacts()
    {
        var item = Item();
        var row = ComponentRow(
            item,
            """{"Part":{"$enum":"InventedParts::Edge","display":"Blade"},"Damage":12,"Speed":0.5}"""
        );
        var unrelated = new TableRecord("/Game/Invented/OtherTable.OtherTable", null, [row]);
        var page = new ItemFacts([], [unrelated]).Resolve(item, "invented");

        Assert.Equal("Material", page.Type);
        Assert.DoesNotContain(page.Facts, fact => fact.Label == "Damage contribution");
    }

    [Theory]
    [InlineData("Iuba_Coin")]
    [InlineData("Tithes")]
    public void CurrenciesFormTheirOwnCategory(string className)
    {
        var page = new ItemFacts([]).Resolve(Item(className: className), "invented");

        Assert.Equal("Currency", page.Type);
        Assert.Equal("Currency", page.Category.Name);
    }

    [Fact]
    public void WeaponComponentsFormTheWeaponPartCategory()
    {
        var item = Item();
        var row = ComponentRow(
            item,
            """{"Part":{"$enum":"InventedParts::Edge","display":"Blade"},"Damage":12,"Speed":0.5}"""
        );
        var table = new TableRecord(
            "/Game/BloodlinesAlpha/WeaponComponentTable.WeaponComponentTable",
            null,
            [row]
        );
        var page = new ItemFacts([], [table]).Resolve(item, "invented");

        Assert.Equal("Weapon Part", page.Category.Name);
        Assert.Equal("weapon-part", page.Category.Slug);
    }

    [Theory]
    [InlineData("Weapon", "SpecificItemType", "Invented sword")]
    [InlineData("Armor", "ItemSlot", "Invented head slot")]
    public void EquippedItemsUseTheirRelevantSubtype(string category, string field, string type)
    {
        var item = Item(category);
        item.Properties["ItemInfo"]![field] = new JsonObject
        {
            ["$enum"] = "InventedTypes::Subtype",
            ["display"] = type,
        };

        Assert.Equal(type, new ItemFacts([]).Resolve(item, "invented").Type);
    }

    [Theory]
    [InlineData("Food", "Food")]
    [InlineData("Potion", "Potion")]
    [InlineData("Quest", "Quest Item")]
    [InlineData("Etc.", "Material")]
    [InlineData("Invented Kind", "Invented Kind")]
    public void OtherItemsUseTheirCategoryAsType(string gameType, string category)
    {
        var page = new ItemFacts([]).Resolve(Item(gameType), "invented");

        Assert.Equal(category, page.Type);
        Assert.Equal(category, page.Category.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Weapon")]
    [InlineData("Armor")]
    public void UndeterminedItemTypesRemainUnknown(string? category)
    {
        var page = new ItemFacts([]).Resolve(Item(category), "invented");

        Assert.Equal("Unknown", page.Type);
        Assert.Null(page.Qualifier);
        Assert.Empty(page.Uses);
    }

    private static TableRecord Table(string name, params TableRow[] rows) =>
        new($"/Game/Invented/{name}.{name}", null, rows);

    private static TableRow Row(string name, string fields) => new(name, Parse(fields));

    private static readonly TableRecord ArmorTable = Table(
        "ArmorDataTable",
        Row("WardensCuirass", """{"ArmorClass":{"$text":"Heavy"},"ArmorValue":13}"""),
        Row("VelosianPlumehelm", """{"ArmorClass":{"$text":"Medium"},"ArmorValue":9}""")
    );

    private static IReadOnlyList<Bloodline.SiteData.Fact> Armor(
        string name,
        string slot,
        string path = "/Game/Invented/Armor.Armor_C",
        params TableRecord[] tables
    ) =>
        new ItemFacts([], tables)
            .Resolve(
                new ClassRecord(
                    path,
                    [],
                    Parse(
                        $$$"""
                        {"ItemInfo":{"ItemName":{"$text":"{{{name}}}"},"ItemType":{"$enum":"E_ItemType::Armor","display":"Armor"},
                         "ItemSlot":{"$enum":"Slot::Invented","display":"{{{slot}}}"},"ArmorType":{"$enum":"Armor::Light","display":"Light"},"Damage":4},
                         "ArmorInfo":{"DamageRating":2}}
                        """
                    )
                ),
                "invented"
            )
            .Facts;

    private static string? Value(IReadOnlyList<Bloodline.SiteData.Fact> facts, Stat stat) =>
        facts.Single(fact => fact.Stat == stat).Value;

    [Theory]
    [InlineData("Warden's Cuirass", "Torso", "Heavy", "13")]
    [InlineData("warden's cuirass", "Back", "Heavy", "13")]
    [InlineData("Velosian Plume-helm", "Head", "Unarmored", "0")]
    public void ArmorFollowsTheTableRowThatEquippingFinds(
        string name,
        string slot,
        string armorType,
        string rating
    )
    {
        var facts = Armor(name, slot, tables: ArmorTable);
        Assert.Equal(armorType, Value(facts, Stat.ArmorType));
        Assert.Equal(rating, Value(facts, Stat.ArmorRating));
    }

    [Fact]
    public void CraftedArmorKeepsItsOwnValueOverATableRow()
    {
        var facts = Armor(
            "Warden's Cuirass",
            "Torso",
            "/Game/BloodlinesAlpha/Interactables/Armor/Torso/CustomArmor_Torso_Base.CustomArmor_Torso_Base_C",
            ArmorTable
        );
        Assert.Equal("Custom", Value(facts, Stat.ArmorType));
        Assert.Equal("4", Value(facts, Stat.ArmorRating));
    }

    [Theory]
    [InlineData("Amulet")]
    [InlineData("Support")]
    public void AmuletsAndSupportItemsAddNoArmorEvenWithATableRow(string slot)
    {
        var facts = Armor("Warden's Cuirass", slot, tables: ArmorTable);
        Assert.Equal("0", Value(facts, Stat.ArmorRating));
        Assert.DoesNotContain(facts, fact => fact.Stat == Stat.ArmorType);
    }

    [Fact]
    public void ArmorStaysUnknownWithoutTheArmorTable()
    {
        var facts = Armor("Warden's Cuirass", "Torso");
        Assert.Null(Value(facts, Stat.ArmorType));
        Assert.Null(Value(facts, Stat.ArmorRating));
    }

    private static IReadOnlyList<Bloodline.SiteData.Fact> Food(
        string name,
        params TableRow[] rows
    ) =>
        new ItemFacts([], rows.Length == 0 ? [] : [Table("FoodEffectDataTable", rows)])
            .Resolve(
                new ClassRecord(
                    "/Game/Invented/Food.Food_C",
                    [],
                    Parse(
                        $$$"""
                        {"ItemInfo":{"ItemName":{"$text":"{{{name}}}"},"ItemType":{"$enum":"E_ItemType::Food","display":"Food"},
                         "PotionInfo":{"PotionEffects":[{"$enum":"Effects::Health","display":"Restore Health"}],"Effect1Power":75,"DurationofEffect1":0,
                          "AmountofHealthorManatorestore":80,"Amountofdamagetodealtohealth":5,"Amountofdamagetodealtomana":0}}
                        }
                        """
                    )
                ),
                "invented"
            )
            .Facts;

    private static TableRow FoodRow(string row, string localized, string effects) =>
        Row(
            row,
            $$$"""{"FoodName":"{{{row}}}","LocalizedFoodName":{"$text":"{{{localized}}}"},"FoodEffects":{"$map":{{{effects}}}}}"""
        );

    [Fact]
    public void FoodAppliesItsEffectRowInsteadOfItsPotionInfo()
    {
        var facts = Food(
            "Apple Pie",
            FoodRow(
                "ApplePie",
                "Apple Pie",
                """[["Heal Player",200],["Boost Health Regeneration",120],["Restore Mana",15],["Cure Sunburn",0]]"""
            )
        );
        Assert.Equal("200", Value(facts, Stat.RestorationAmount));
        Assert.Equal(
            ["Boost Health Regeneration for 120 s", "Restore Mana 15", "Cure Sunburn"],
            facts.Where(fact => fact.Stat == Stat.Effect).Select(fact => fact.Value)
        );
        Assert.DoesNotContain(
            facts,
            fact => fact.Stat is Stat.HealthDamage or Stat.ManaDamage or Stat.Effects
        );
    }

    [Fact]
    public void FoodFallsBackToTheFirstRowWithItsLocalizedName()
    {
        var facts = Food(
            "Apple Pie",
            FoodRow("Unrelated", "Other Food", """[["Heal Player",5]]"""),
            FoodRow("RenamedPie", "Apple Pie", """[["Heal Player",200]]"""),
            FoodRow("LaterPie", "Apple Pie", """[["Heal Player",1]]""")
        );
        Assert.Equal("200", Value(facts, Stat.RestorationAmount));
    }

    [Fact]
    public void OnlyEffectNamesThatTheGameKnowsApplyWithoutRegardToCase()
    {
        var facts = Food(
            "Druid's Stew",
            FoodRow(
                "DruidsStew",
                "Druid's Stew",
                """[["Heal PLayer",50],["Boost Foraging EXP ",900],["boost foraging exp",60]]"""
            )
        );
        Assert.Equal("50", Value(facts, Stat.RestorationAmount));
        Assert.Equal(
            "Boost Foraging EXP 1.25 for 60 s",
            Assert.Single(facts, fact => fact.Stat == Stat.Effect).Value
        );
    }

    [Fact]
    public void FoodWithoutAnEffectRowHasNoEffectDespiteItsPotionInfo()
    {
        var facts = Food("Steak", FoodRow("ApplePie", "Apple Pie", """[["Heal Player",200]]"""));
        Assert.Equal("None", Value(facts, Stat.Effects));
        Assert.DoesNotContain(
            facts,
            fact => fact.Stat is Stat.RestorationAmount or Stat.Effect or Stat.HealthDamage
        );
    }

    [Fact]
    public void FoodEffectsStayUnknownWithoutTheFoodTable()
    {
        var facts = Food("Steak");
        Assert.Null(Value(facts, Stat.Effects));
        Assert.DoesNotContain(facts, fact => fact.Stat is Stat.RestorationAmount or Stat.Effect);
    }

    [Theory]
    [InlineData("Pistol", "Pistol")]
    [InlineData("None", "Rifle")]
    [InlineData(null, "Rifle")]
    public void AWeaponsTypeIsItsAttackTypeAndElseItsItemType(string? attack, string type)
    {
        var item = Item("Weapon");
        item.Properties["ItemInfo"]!["SpecificItemType"] = Parse(
            """{"$enum":"E_SpecificItemType::Rifle","display":"Rifle"}"""
        );
        if (attack is not null)
        {
            item.Properties["ItemInfo"]!["WeaponType"] = Parse(
                $$"""{"$enum":"E_AttackType::{{attack}}","display":"{{attack}}"}"""
            );
        }
        Assert.Equal(type, new ItemFacts([]).Resolve(item, "invented").Type);
    }

    [Fact]
    public void SpeedWordFollowsTheItemInfoThatTooltipsRead()
    {
        var item = Item("Weapon");
        item.Properties["ItemInfo"]!["Speed"] = Parse(
            """{"$enum":"AttackSpeeds::Slow","display":"Slow"}"""
        );
        item.Properties["WeaponInfo"] = Parse("""{"AttackSpeed":1.5}""");
        var facts = new ItemFacts([]).Resolve(item, "invented").Facts;

        var speed = facts.Single(fact => fact.Stat == Stat.Speed);
        Assert.Equal(("Slow", "Speed"), (speed.Value, speed.TooltipLabel));
        var multiplier = facts.Single(fact => fact.Stat == Stat.AttackSpeed);
        Assert.Equal(("1.5", (string?)null), (multiplier.Value, multiplier.TooltipLabel));
    }
}
