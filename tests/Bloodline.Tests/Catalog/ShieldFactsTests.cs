using System.Text.Json.Nodes;
using Bloodline.Catalog;
using Bloodline.Code;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Tests.Catalog;

public sealed class ShieldFactsTests
{
    private static ClassRecord Shield(string name = "InventedShield") =>
        new(
            "/Game/Invented/" + name + "." + name + "_C",
            [],
            new JsonObject
            {
                ["ItemInfo"] = new JsonObject
                {
                    ["ItemName"] = new JsonObject { ["$text"] = "Invented shield" },
                    ["ItemType"] = new JsonObject
                    {
                        ["$enum"] = "Category::Weapon",
                        ["display"] = "Weapon",
                    },
                    ["ItemSlot"] = new JsonObject
                    {
                        ["$enum"] = "Slot::Shield",
                        ["display"] = "Shield",
                    },
                    ["Damage"] = 17,
                },
                ["WeaponInfo"] = new JsonObject { ["Damage"] = 999 },
            }
        );

    [Fact]
    public void ShieldPagesPublishBlockFactsAndNeitherStoredDamage()
    {
        var page = new ItemFacts([]).Resolve(Shield(), "invented");
        Assert.Equal("Shield", page.Type);
        Assert.DoesNotContain(
            page.Facts,
            fact => fact.Stat is Stat.Damage or Stat.BodyHitDamage or Stat.HeadHitDamage
        );
        var stamina = Assert.Single(page.Facts, fact => fact.Stat == Stat.BlockStaminaCost);
        Assert.Equal(50, stamina.Number);
        var blocks = Assert.Single(page.Facts, fact => fact.Stat == Stat.ExtraFullBlocks);
        Assert.Equal(6, blocks.Number);
    }

    [Fact]
    public void BlockClaimsTrackTheConsumingPlayerAndWidgetEvents()
    {
        var citations = new[]
        {
            "/Game/BloodlinesAlpha/Blueprints/FirstPerson2/FirstPersonCharacter2:Block Attack",
            "/Game/Widgets/AbsoluteBlockWidget:Check if player has a shield equipped",
        };
        var code = CodeIndex.Create(
            citations
                .Select(citation => new CodeFunction(
                    citation.Split(':')[0],
                    "/Game/Invented.Owner_C",
                    citation.Split(':')[1],
                    false,
                    "fingerprint-" + citation,
                    CodeParseStatus.Complete,
                    [],
                    [],
                    [],
                    []
                ))
                .ToArray(),
            []
        );
        var facts = new ItemFacts([], code: code, build: 123);
        facts.Resolve(Shield(), "invented");
        var claims = facts
            .Claims.Where(claim =>
                claim.Citations.Any(citation => citations.Contains(citation.Function))
            )
            .ToArray();
        Assert.Equal(2, claims.Length);
        Assert.All(claims, claim => Assert.Equal(ClaimType.Formula, claim.Type));
        Assert.Equal(
            citations.Order(),
            claims
                .SelectMany(claim => claim.Citations)
                .Select(citation => citation.Function)
                .Order()
        );
        Assert.DoesNotContain(facts.Claims, claim => claim.Fact == "Damage");
    }

    [Theory]
    [InlineData("Half-colorKiteshield")]
    [InlineData("ShieldOfBurnsley")]
    public void OnlyChargeContactConsumersReadItemDamage(string name)
    {
        var shield = Shield(name) with
        {
            Path =
                "/Game/BloodlinesAlpha/Interactables/Weapons/Shields/" + name + "." + name + "_C",
        };
        var page = new ItemFacts([]).Resolve(shield, "invented");
        var charge = Assert.Single(page.Facts, fact => fact.Stat == Stat.ShieldChargeDamage);
        Assert.Equal(17, charge.Number);
        Assert.DoesNotContain(
            new ItemFacts([]).Resolve(Shield(), "other").Facts,
            fact => fact.Stat == Stat.ShieldChargeDamage
        );
    }
}
