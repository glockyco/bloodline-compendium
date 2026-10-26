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
}
