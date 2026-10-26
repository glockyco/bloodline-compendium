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
        JsonObject? reagent = null,
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
                ["ReagentInfo"] = reagent,
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
            "Food",
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
        var facts = Facts("Food", potion: Parse("""{"PotionEffects":[]}"""));
        foreach (var label in new[] { "Restoration amount", "Health damage", "Mana damage" })
        {
            Assert.Null(facts.Single(fact => fact.Label == label).Value);
        }
    }

    [Theory]
    [InlineData("Chopped")]
    [InlineData("Cooled")]
    [InlineData("Ground")]
    [InlineData("Heated")]
    public void ReagentEffectsOnlyAppearWithActivePower(string treatment)
    {
        JsonObject Reagent(int power, int duration) =>
            Parse(
                $$$"""
                {"{{{treatment}}}Effects":[{"$enum":"Effects::Default","display":"Enhance Speed"}],"{{{treatment}}}EffectPower":{{{power}}},"{{{treatment}}}EffectDuration":{{{duration}}}}
                """
            );
        Assert.DoesNotContain(
            Facts("Etc.", reagent: Reagent(0, 60)),
            fact => fact.Label.StartsWith(treatment, StringComparison.Ordinal)
        );
        Assert.Equal(
            "Enhance Speed 10 for 60 s",
            Facts("Etc.", reagent: Reagent(10, 60))
                .Single(fact => fact.Label == treatment + " effect")
                .Value
        );
        Assert.Equal(
            "Enhance Speed 10",
            Facts("Etc.", reagent: Reagent(10, 0))
                .Single(fact => fact.Label == treatment + " effect")
                .Value
        );
    }

    [Theory]
    [InlineData("ChoppedEffects")]
    [InlineData("ChoppedEffectPower")]
    [InlineData("ChoppedEffectDuration")]
    public void MissingReagentEffectFieldsRemainUnknown(string field)
    {
        var reagent = Parse(
            """
            {"ChoppedEffects":[{"$enum":"Effects::Speed","display":"Enhance Speed"}],"ChoppedEffectPower":10,"ChoppedEffectDuration":60}
            """
        );
        reagent.Remove(field);
        Assert.Null(
            Facts("Etc.", reagent: reagent).Single(fact => fact.Label == "Chopped effect").Value
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
                {"Range":350,"MagazineSize":5,"DamageType":{"$enum":"Damage::Pierce","display":"Pierce"}}
                """
            )
        );
        Assert.Equal("350", facts.Single(fact => fact.Label == "Range").Value);
        Assert.Equal("5", facts.Single(fact => fact.Label == "Magazine size").Value);
        Assert.Equal("Pierce", facts.Single(fact => fact.Label == "Damage type").Value);
    }

    [Fact]
    public void MissingWeaponOptionsRemainUnknown()
    {
        var facts = Facts("Weapon", weapon: new JsonObject());
        foreach (var label in new[] { "Range", "Magazine size", "Damage type" })
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

        Assert.Equal("Etc.", page.Type);
        Assert.DoesNotContain(page.Facts, fact => fact.Label == "Damage contribution");
    }

    [Theory]
    [InlineData("Iuba_Coin")]
    [InlineData("Tithes")]
    public void CurrencyConsumersDetermineTypeWithoutChangingTheRecordedCategory(string className)
    {
        var page = new ItemFacts([]).Resolve(Item(className: className), "invented");

        Assert.Equal("Currency", page.Type);
        Assert.Equal("Etc.", page.Category.Name);
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
    [InlineData("Food")]
    [InlineData("Potion")]
    [InlineData("Etc.")]
    public void OtherItemsUseTheirRecordedCategoryAsType(string category)
    {
        Assert.Equal(category, new ItemFacts([]).Resolve(Item(category), "invented").Type);
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
}
