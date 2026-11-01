using System.Text.Json.Nodes;
using Bloodline.Catalog;
using Bloodline.Code;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Tests.Catalog;

public sealed class RangedFactsTests
{
    private const string Base = "/Game/Invented/Launcher.Launcher_C";
    private const string Bolt = "/Game/Invented/Bolt.Bolt_C";
    private const string Bullet = "/Game/BloodlinesAlpha/Weapons/Bullet.Bullet_C";

    private static ClassRecord Weapon(string type, double itemDamage, double weaponDamage) =>
        new(
            "/Game/Invented/Weapon.Weapon_C",
            [Base],
            new JsonObject
            {
                ["ItemInfo"] = new JsonObject
                {
                    ["ItemName"] = new JsonObject { ["$text"] = "Invented weapon" },
                    ["ItemType"] = new JsonObject
                    {
                        ["$enum"] = "Category::Weapon",
                        ["display"] = "Weapon",
                    },
                    ["WeaponType"] = new JsonObject
                    {
                        ["$enum"] = "Weapon::" + type,
                        ["display"] = type,
                    },
                    ["Damage"] = itemDamage,
                },
                ["WeaponInfo"] = new JsonObject { ["Damage"] = weaponDamage, ["Range"] = 0 },
            }
        );

    private static CodeFunction Fire(
        string type,
        IReadOnlyList<FiredProjectile>? projectiles,
        double? charge = null,
        CodeParseStatus status = CodeParseStatus.Complete,
        bool replaces = true
    ) =>
        new(
            Base.Split('.')[0],
            Base,
            type == "Crossbow" ? "Custom Fire Event"
                : type == "Bow" ? "Custom Shoot Bow Event"
                : "Swing Event",
            false,
            "invented",
            status,
            [],
            [],
            [],
            []
        )
        {
            RangedFire = new RangedFire(projectiles, replaces, charge),
        };

    private static ItemPage Resolve(ClassRecord item, CodeFunction fire, params CodeGap[] gaps) =>
        new ItemFacts(
            [],
            code: CodeIndex.Create(
                [
                    fire,
                    new CodeFunction(
                        "/Game/BloodlinesAlpha/Blueprints/FirstPerson2/FirstPersonCharacter2",
                        "/Game/Invented/Player.Player_C",
                        "Calculate Random Shotgun Projectiles",
                        false,
                        "invented",
                        CodeParseStatus.Complete,
                        [],
                        [],
                        [],
                        []
                    )
                    {
                        RangedFire = new RangedFire(
                            Enumerable
                                .Repeat(new FiredProjectile(Bullet, null, false), 5)
                                .ToArray(),
                            false,
                            null
                        ),
                    },
                ],
                gaps
            ),
            classes:
            [
                item,
                new ClassRecord(Base, [], new JsonObject()),
                new ClassRecord(
                    Bolt,
                    ["/Game/BloodlinesAlpha/Weapons/Arrow.Arrow_C"],
                    new JsonObject { ["Damage"] = 0 }
                ),
                new ClassRecord(Bullet, [], new JsonObject { ["Damage"] = 0 }),
            ]
        ).Resolve(item, "invented");

    [Fact]
    public void CrossbowChargeUsesItemDamageAndHeadAddsWeaponDamage()
    {
        var page = Resolve(
            Weapon("Crossbow", 85, 100),
            Fire("Crossbow", [new FiredProjectile(Bolt, new DamageFormula(0, 1, 0, 1), true)], 0.3)
        );
        Assert.Equal(85, Number(page, Stat.BodyHitDamage));
        Assert.Equal(185, Number(page, Stat.HeadHitDamage));
        Assert.Equal(110.5, Number(page, Stat.ChargedBodyHitDamage));
        Assert.Equal(1, Number(page, Stat.ProjectilesPerShot));
        Assert.DoesNotContain(page.Facts, fact => fact.Stat == Stat.Damage);
    }

    [Fact]
    public void AVolleyUsesProjectileDefaultsRatherThanTheTooltipDamage()
    {
        var page = Resolve(
            Weapon("Gun", 300, 20),
            Fire("Gun", Enumerable.Repeat(new FiredProjectile(Bolt, null, false), 15).ToArray())
        );
        Assert.Equal(0, Number(page, Stat.BodyHitDamage));
        Assert.Equal(20, Number(page, Stat.HeadHitDamage));
        Assert.Equal(15, Number(page, Stat.ProjectilesPerShot));
        Assert.Equal("300", page.Facts.Single(fact => fact.Stat == Stat.BodyHitDamage).GameShows);
    }

    [Fact]
    public void DelayedGenericPelletsRemainSeparateFromTheReplacementBullet()
    {
        var item = Weapon("Gun", 100, 50);
        item.Properties["WeaponInfo"]!["Range"] = 1;
        var page = Resolve(
            item,
            Fire("Gun", [new FiredProjectile(Bolt, new DamageFormula(100, 0, 0, 0), true)])
        );
        Assert.Equal(
            [new ProjectileGroup(1, 100, 150, "arrow"), new ProjectileGroup(4, 0, 50, "pellets")],
            page.ProjectileGroups
        );
        Assert.Equal(100, Number(page, Stat.BodyHitDamage));
        Assert.Equal(150, Number(page, Stat.HeadHitDamage));
        Assert.Equal(5, Number(page, Stat.ProjectilesPerShot));
    }

    [Fact]
    public void TheNormalBowArrowUsesWeaponDamageInsteadOfItsClassDefault()
    {
        var page = Resolve(Weapon("Bow", 75, 35), Fire("Bow", [], replaces: false));
        Assert.Equal(35, Number(page, Stat.BodyHitDamage));
        Assert.Equal(70, Number(page, Stat.HeadHitDamage));
        Assert.Equal(1, Number(page, Stat.ProjectilesPerShot));
    }

    [Theory]
    [InlineData("Gun", Bullet, 1, "bullet")]
    [InlineData("Gun", Bullet, 15, "pellets")]
    [InlineData("Crossbow", "/Game/BloodlinesAlpha/Weapons/CrossBowBolt.CrossBowBolt_C", 1, "bolt")]
    [InlineData(
        "Crossbow",
        "/Game/BloodlinesAlpha/Weapons/CrossBowBolt.CrossBowBolt_C",
        2,
        "bolts"
    )]
    [InlineData("Bow", "/Game/BloodlinesAlpha/Weapons/Arrow.Arrow_C", 1, "arrow")]
    [InlineData("Bow", "/Game/BloodlinesAlpha/Weapons/Arrow.Arrow_C", 2, "arrows")]
    [InlineData("Gun", "/Game/Invented/Unknown.Unknown_C", 1, "projectile")]
    [InlineData("Gun", "/Game/Invented/Unknown.Unknown_C", 2, "projectiles")]
    public void ProjectileNounsFollowTheSpawnedClass(
        string type,
        string projectile,
        int count,
        string noun
    )
    {
        var page = Resolve(
            Weapon(type, 100, 50),
            Fire(
                type,
                Enumerable
                    .Repeat(
                        new FiredProjectile(projectile, new DamageFormula(10, 0, 0, 0), true),
                        count
                    )
                    .ToArray()
            )
        );
        Assert.Equal(noun, page.ProjectileGroups![0].Projectile);
    }

    [Fact]
    public void AnUnchargedCrossbowDoesNotPublishAChargeFact()
    {
        var page = Resolve(
            Weapon("Crossbow", 85, 100),
            Fire("Crossbow", [new FiredProjectile(Bolt, new DamageFormula(0, 1, 0, 0), true)], 0)
        );
        Assert.DoesNotContain(page.Facts, fact => fact.Stat == Stat.ChargedBodyHitDamage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnreadCodeDoesNotFallBackToAuthoredDamage(bool gap)
    {
        var item = Weapon("Pistol", 100, 75);
        var fire = Fire(
            "Pistol",
            [new FiredProjectile(Bolt, new DamageFormula(100, 0, 0, 0), true)],
            status: gap ? CodeParseStatus.Complete : CodeParseStatus.Partial
        );
        var page = Resolve(
            item,
            fire,
            gap ? [new CodeGap(item.Path.Split('.')[0], "unread package")] : []
        );
        Assert.Null(Number(page, Stat.BodyHitDamage));
        Assert.Null(Number(page, Stat.HeadHitDamage));
        Assert.Null(Number(page, Stat.ProjectilesPerShot));
    }

    private static double? Number(ItemPage page, Stat stat) =>
        page.Facts.Single(fact => fact.Stat == stat).Number;
}
