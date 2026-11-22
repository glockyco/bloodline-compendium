using System.Text.Json.Nodes;
using Bloodline.Catalog;
using Bloodline.Code;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Tests.Catalog;

public sealed class OnHitFactsTests
{
    private const string Root = "/Game/BloodlinesAlpha/Interactables/Weapons/";

    private static ClassRecord Weapon(string path) =>
        new(
            Root + path + "." + path.Split('/')[^1] + "_C",
            [],
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
                        ["$enum"] = "Weapon::Spear",
                        ["display"] = "Spear",
                    },
                },
                ["WeaponInfo"] = new JsonObject { ["Damage"] = 25 },
            }
        );

    private static JsonObject Debuff(string effect, double damage, double duration) =>
        new()
        {
            ["DebuffType"] = new JsonObject
            {
                ["$enum"] = "Debuff::" + effect,
                ["display"] = effect,
            },
            ["Damage"] = damage,
            ["Duration"] = duration,
            ["DelayofrepeatingDamage"] = 0.25,
            ["Numberoftimestoloop"] = 99,
        };

    /// <summary>A code index whose function makes one random roll with the given literal arguments.</summary>
    /// <summary>
    /// A weapon without its weapon damage, whose claim needs the damage functions in the code index, so a test of its
    /// rolls needs only the roll and the effect functions.
    /// </summary>
    private static ClassRecord WithoutDamageClaim(ClassRecord item)
    {
        item.Properties["WeaponInfo"]!.AsObject().Remove("Damage");
        return item;
    }

    /// <summary>The enemy and status functions every on-hit effect cites, which a code index must hold.</summary>
    private static readonly string[] EffectFunctions =
    [
        "/Game/AIBehaviorToolkit/Blueprints/Goblin_Base:Ignite",
        "/Game/AIBehaviorToolkit/Blueprints/Goblin_Base:Apply Projectile Debuff",
        "/Game/AIBehaviorToolkit/Blueprints/EnemyStatusEffectManager:Apply Burn Debuff",
        "/Game/AIBehaviorToolkit/Blueprints/EnemyStatusEffectManager:Begin Burn",
        "/Game/AIBehaviorToolkit/Blueprints/EnemyStatusEffectManager:Stop Burn",
        "/Game/AIBehaviorToolkit/Blueprints/EnemyStatusEffectManager:Apply Poison Debuff",
        "/Game/AIBehaviorToolkit/Blueprints/EnemyStatusEffectManager:Begin Poisoning",
        "/Game/AIBehaviorToolkit/Blueprints/EnemyStatusEffectManager:Stop Poison",
    ];

    /// <summary>A code index whose given function makes one random roll with the given literal arguments.</summary>
    private static CodeIndex Rolls(
        string package,
        string name,
        string function,
        params double?[] arguments
    ) =>
        CodeIndex.Create([
            new CodeFunction(
                package,
                package + "_C",
                name,
                false,
                "fingerprint",
                CodeParseStatus.Complete,
                [],
                [],
                [],
                []
            )
            {
                RandomCalls = [new RandomCall(function, arguments)],
            },
            .. EffectFunctions.Select(citation => new CodeFunction(
                citation.Split(':')[0],
                "/Game/Invented.Owner_C",
                citation.Split(':')[1],
                false,
                "fingerprint",
                CodeParseStatus.Complete,
                [],
                [],
                [],
                []
            )),
        ]);

    [Fact]
    public void PikeReadsConsumedDebuffFieldsAndNotTheOldLoopSettings()
    {
        var item = WithoutDamageClaim(Weapon("Spears/PoisonPike"));
        item.Properties["Debuff"] = Debuff("Poison", 17, 12);
        // Enemy Hit Event poisons when its RandomIntegerInRange(0, 4) returns zero, one value in five.
        var page = new ItemFacts(
            [],
            code: Rolls(Root + "Spears/PoisonPike", "Enemy Hit Event", "RandomIntegerInRange", 0, 4)
        ).Resolve(item, "invented");
        var effect = Assert.Single(page.OnHitEffects!);
        Assert.Equal("Poison", effect.Effect);
        Assert.Equal(0.2, effect.Chance);
        Assert.Equal(17, effect.Amount);
        Assert.Equal(1, effect.TickIntervalSeconds);
        Assert.Equal(12, effect.DurationSeconds);
    }

    [Fact]
    public void TorchUsesItsRollAndIgniteLiteralsRatherThanWeaponDamage()
    {
        var page = new ItemFacts(
            [],
            code: Rolls(
                Root + "1HandedWeapons/Torch",
                "Enemy Hit Event",
                "RandomIntegerInRange",
                0,
                5
            )
        ).Resolve(WithoutDamageClaim(Weapon("1HandedWeapons/Torch")), "invented");
        var effect = Assert.Single(page.OnHitEffects!);
        Assert.Equal("Burn", effect.Effect);
        Assert.Equal(1d / 6, effect.Chance);
        Assert.Equal(15, effect.Amount);
        Assert.Equal(1, effect.TickIntervalSeconds);
        Assert.Equal(5, effect.DurationSeconds);
    }

    [Theory]
    [InlineData("StaffOfMinorFlames", "BP_Projectile_Fireball01Orange_Weak", "Burn")]
    [InlineData("StaffOfMinorRot", "BP_Projectile_PoisonBall_Weak", "Poison")]
    public void MinorStavesReadProjectileClassDebuffs(
        string name,
        string projectileName,
        string effect
    )
    {
        var item = Weapon("Halberds/" + name);
        var projectile = new ClassRecord(
            "/Game/GabrielAguiarProd/ToonProjectilesVol1/Blueprints/"
                + projectileName
                + "."
                + projectileName
                + "_C",
            [],
            new JsonObject { ["Debuff Info"] = Debuff(effect, 9, 8) }
        );
        var page = new ItemFacts([], classes: [item, projectile]).Resolve(item, "invented");
        var onHit = Assert.Single(page.OnHitEffects!);
        Assert.Equal(effect, onHit.Effect);
        Assert.Equal(1, onHit.Chance);
        Assert.Equal(9, onHit.Amount);
        Assert.Equal(1, onHit.TickIntervalSeconds);
        Assert.Equal(8, onHit.DurationSeconds);
    }

    [Fact]
    public void HolyNooseIsAnAimedFireEffectNotAProjectileImpact()
    {
        var effects = new ItemFacts([])
            .Resolve(Weapon("Crossbows/HolyNooseCrossbow"), "invented")
            .OnHitEffects!;
        Assert.Equal(2, effects.Count);
        Assert.All(
            effects,
            effect => Assert.Contains("undead", effect.Trigger, StringComparison.OrdinalIgnoreCase)
        );
        Assert.Equal("Burn", effects[0].Effect);
        Assert.Equal(15, effects[0].Amount);
        Assert.Null(effects[1].DurationSeconds);
        Assert.Null(effects[1].Amount);
    }

    [Fact]
    public void VeiledSlicersRestoreManaInstantly()
    {
        var effect = Assert.Single(
            new ItemFacts([]).Resolve(Weapon("Daggers/VeiledSlicers"), "invented").OnHitEffects!
        );
        Assert.Equal(1, effect.Chance);
        Assert.Equal(5, effect.Amount);
        Assert.Null(effect.TickIntervalSeconds);
        Assert.Null(effect.DurationSeconds);
    }

    [Fact]
    public void EmbersnapBurnsAtItsRollsWeight()
    {
        // The Emberspark's overlap burns when its RandomBoolWithWeight succeeds, and without that roll the chance
        // stays unknown rather than a guess.
        var weapon = WithoutDamageClaim(Weapon("Guns/Embersnap"));
        var unknown = Assert.Single(new ItemFacts([]).Resolve(weapon, "invented").OnHitEffects!);
        Assert.Equal(
            ("Burn", 15d, 5d),
            (unknown.Effect, unknown.Amount!.Value, unknown.DurationSeconds!.Value)
        );
        Assert.Null(unknown.Chance);
        const string emberspark =
            "/Game/GabrielAguiarProd/ToonProjectilesVol1/Blueprints/Emberspark";
        var rolled = Assert.Single(
            new ItemFacts(
                [],
                code: Rolls(
                    emberspark,
                    "BndEvt__ProjectileCollider_K2Node_ComponentBoundEvent_1_ComponentBeginOverlapSignature__DelegateSignature",
                    "RandomBoolWithWeight",
                    0.25
                )
            )
                .Resolve(weapon, "invented")
                .OnHitEffects!
        );
        Assert.Equal(0.25, rolled.Chance);
    }

    [Theory]
    [InlineData("1HandedWeapons/1H_HAMMER_SturdyWarhammer")]
    [InlineData("2H_Weapons/2HWeapon_GreatMace")]
    [InlineData("2H_Weapons/Greathammer")]
    [InlineData("1HandedWeapons/UndeadMace")]
    public void UnappliedDefaultsAndUnsettledPathsDoNotBecomeEffects(string path)
    {
        var item = Weapon(path);
        item.Properties["Debuff"] = Debuff("Burn", 500, 500);
        Assert.Null(new ItemFacts([]).Resolve(item, "invented").OnHitEffects);
    }

    [Fact]
    public void OnHitClaimsCiteCompleteReachableEventsForChangeComparison()
    {
        var citations = new[]
        {
            Root + "1HandedWeapons/Torch:Enemy Hit Event",
            "/Game/AIBehaviorToolkit/Blueprints/Goblin_Base:Ignite",
            "/Game/AIBehaviorToolkit/Blueprints/EnemyStatusEffectManager:Apply Burn Debuff",
            "/Game/AIBehaviorToolkit/Blueprints/EnemyStatusEffectManager:Begin Burn",
            "/Game/AIBehaviorToolkit/Blueprints/EnemyStatusEffectManager:Stop Burn",
        };
        var code = CodeIndex.Create(
            citations
                .Select(citation => new CodeFunction(
                    citation.Split(':')[0],
                    "/Game/Invented.Owner_C",
                    citation.Split(':')[1],
                    false,
                    "invented-" + citation,
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
        var item = Weapon("1HandedWeapons/Torch");
        item.Properties["WeaponInfo"]!.AsObject().Remove("Damage");
        facts.Resolve(item, "invented");
        var claim = Assert.Single(facts.Claims, claim => claim.Fact == "On-Hit Effects");
        Assert.Equal(ClaimType.Formula, claim.Type);
        Assert.Equal(123, claim.Build);
        Assert.Equal(citations.Order(), claim.Citations.Select(citation => citation.Function));
        Assert.All(
            claim.Citations,
            citation => Assert.StartsWith("invented-", citation.Fingerprint)
        );
    }
}
