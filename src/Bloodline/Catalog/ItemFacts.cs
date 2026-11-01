using System.Globalization;
using System.Text.Json.Nodes;
using Bloodline.Code;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed class ItemFacts(
    IReadOnlyList<EnumRecord> enums,
    IReadOnlyList<TableRecord>? tables = null,
    CodeIndex? code = null,
    long build = 0,
    IReadOnlyList<ClassRecord>? classes = null
)
{
    private readonly ItemClaims claims = new(code, build);
    public IReadOnlyList<FactClaim> Claims => claims.Claims;

    private readonly HashSet<string> ignoredFoodEffects = new(StringComparer.Ordinal);

    /// <summary>Foods whose effect row lists an effect name that the food manager's switch does not apply.</summary>
    public IReadOnlySet<string> IgnoredFoodEffects => ignoredFoodEffects;

    private readonly HashSet<string> untrainedWeapons = new(StringComparer.Ordinal);

    /// <summary>
    /// Weapons whose hits award no proficiency experience. A hit passes the weapon's attack type to the game's
    /// proficiency award, which gives a Halberd attack's experience only to a weapon of the Staff class.
    /// </summary>
    public IReadOnlySet<string> UntrainedWeapons => untrainedWeapons;

    private readonly Dictionary<string, string?> enumDisplays = enums
        .SelectMany(record => record.Entries.Select(entry => (entry.Name, entry.Display)))
        .GroupBy(entry => entry.Name, StringComparer.Ordinal)
        .ToDictionary(group => group.Key, group => group.First().Display, StringComparer.Ordinal);

    private readonly Dictionary<string, TableRow[]> components = (tables ?? [])
        .Where(table => ItemIdentity.ClassName(table.Path) == "WeaponComponentTable")
        .SelectMany(table => table.Rows)
        .Select(row => (Item: RecordValues.AnyReference(row.Values["Class"]), Row: row))
        .Where(entry => entry.Item is not null)
        .GroupBy(entry => entry.Item!, entry => entry.Row, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(
            group => group.Key,
            group => group.ToArray(),
            StringComparer.OrdinalIgnoreCase
        );

    // Null when the records hold no such table, so that its facts stay unknown instead of empty.
    private readonly RowLookup? armorRows = RowLookup.Find(tables, "ArmorDataTable");
    private readonly RowLookup? foodRows = RowLookup.Find(tables, "FoodEffectDataTable");
    private readonly RowLookup? potionEffectNames = RowLookup.Find(
        tables,
        "PotionEffectTranslatorTable"
    );

    /// <summary>
    /// The effect numbers that FoodManager:Find Any Apply Potion Effects applies, with how it uses the amount it is
    /// given. Its other numbers, such as Relaxed and Poison, apply nothing. The two EXP boosts pass a fixed
    /// multiplier of 1.25.
    /// </summary>
    private static readonly Dictionary<int, (FoodValue Value, double? Power)> PotionDispatch = new()
    {
        [1] = (FoodValue.Amount, null),
        [2] = (FoodValue.Amount, null),
        [3] = (FoodValue.Seconds, null),
        [4] = (FoodValue.Seconds, null),
        [5] = (FoodValue.Amount, null),
        [6] = (FoodValue.Seconds, null),
        [7] = (FoodValue.Seconds, null),
        [8] = (FoodValue.Seconds, null),
        [9] = (FoodValue.Seconds, null),
        [11] = (FoodValue.Seconds, null),
        [15] = (FoodValue.Seconds, 1.25),
        [18] = (FoodValue.Seconds, null),
        [19] = (FoodValue.Seconds, null),
        [20] = (FoodValue.Seconds, null),
        [22] = (FoodValue.Seconds, null),
        [23] = (FoodValue.Seconds, null),
        [24] = (FoodValue.Seconds, null),
        [25] = (FoodValue.Seconds, 1.25),
    };

    // Potioncrafting_Ingredient_Slot:Retrieve Prep Effects finds an ingredient's row by its Item class.
    private readonly Dictionary<string, TableRow> ingredientRows = (tables ?? [])
        .Where(table => ItemIdentity.ClassName(table.Path) == "AllIngredientEffects")
        .SelectMany(table => table.Rows)
        .Where(row => RecordValues.AnyReference(row.Values["Item"]) is not null)
        .GroupBy(
            row => RecordValues.AnyReference(row.Values["Item"])!,
            StringComparer.OrdinalIgnoreCase
        )
        .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

    // PotionCrafting:Get_Ing_1_Effect words these effects as points and every other effect as seconds.
    private static readonly HashSet<string> PointEffects = new(StringComparer.Ordinal)
    {
        "Restore Health",
        "Restore Stamina",
        "Restore Mana",
    };

    // PlayerPassiveLevelStatComponent:Retrieve Player Armor adds up the armor of these item slots. Items of the
    // slot Back go into the character sheet's arms slot. Amulets and support items add no armor.
    private static readonly HashSet<string> ArmoredSlots = new(StringComparer.Ordinal)
    {
        "Head",
        "Torso",
        "Back",
        "Arms",
        "Legs",
        "Knees",
        "Feet",
        "Shoulders",
    };

    // Get Armor Slot gives these crafted armor classes their own ItemInfo.Damage and the class Custom.
    private static readonly HashSet<string> CraftedArmor = new(StringComparer.Ordinal)
    {
        "/Game/BloodlinesAlpha/Interactables/Armor/Arms/CustomArm_Armor_Base.CustomArm_Armor_Base_C",
        "/Game/BloodlinesAlpha/Interactables/Armor/Feet/Custom_Foot_Armor_Base.Custom_Foot_Armor_Base_C",
        "/Game/BloodlinesAlpha/Interactables/Armor/Head/CustomArmor_Base_Head.CustomArmor_Base_Head_C",
        "/Game/BloodlinesAlpha/Interactables/Armor/Legs/Base_Custom_Leg_Armor.Base_Custom_Leg_Armor_C",
        "/Game/BloodlinesAlpha/Interactables/Armor/Shoulders/CustomShoulderArmor.CustomShoulderArmor_C",
        "/Game/BloodlinesAlpha/Interactables/Armor/Torso/CustomArmor_Torso_Base.CustomArmor_Torso_Base_C",
    };

    private enum FoodValue
    {
        Heal,
        Amount,
        Seconds,
        Unused,
    }

    // FoodManager:Get Item's switch over effect names, with how the function it calls uses the row's value. The
    // two EXP boosts also pass a fixed multiplier of 1.25. Names are FNames, which compare without regard to case.
    private static readonly Dictionary<
        string,
        (string Name, FoodValue Value, double? Power)
    > FoodEffects = new[]
    {
        ("Heal Player", FoodValue.Heal, (double?)null),
        ("Boost Woodcutting EXP", FoodValue.Seconds, 1.25),
        ("Druidsight", FoodValue.Seconds, null),
        ("Boost Foraging EXP", FoodValue.Seconds, 1.25),
        ("Drunk", FoodValue.Seconds, null),
        ("Boost Health Regeneration", FoodValue.Seconds, null),
        ("Boost Mana Regeneration", FoodValue.Seconds, null),
        ("Quickness", FoodValue.Seconds, null),
        ("Restore Mana", FoodValue.Amount, null),
        ("Restore Stamina", FoodValue.Amount, null),
        ("Mouse Form", FoodValue.Seconds, null),
        ("Cure Minor Diseases", FoodValue.Unused, null),
        ("Sunshade", FoodValue.Seconds, null),
        ("Cure Sunburn", FoodValue.Unused, null),
        ("Bat Form", FoodValue.Seconds, null),
        ("Bloodfeed", FoodValue.Seconds, null),
        ("Boost Stamina Regeneration", FoodValue.Seconds, null),
    }.ToDictionary(effect => effect.Item1, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Items that fill the shield slot are Shields, as most of them are typed already: lab run hands-1101 equipped
    /// Metal Pot, Torch, Thurstons Grimoire and Sturdy Wood Kiteshield, typed One Handed Weapon, each in the slot
    /// Lonland Shield fills, beside a sword. Staves retain their own type within the shared Halberd attack family.
    /// Other weapons use their attack type, or their item type when no attack type is set.
    /// </summary>
    private string? WeaponType(ClassRecord item) =>
        Display(RecordValues.At(item.Properties, "ItemInfo.ItemSlot")) == "Shield" ? "Shield"
        : IsStaff(item) ? "Staff"
        : Display(RecordValues.At(item.Properties, "ItemInfo.WeaponType"))
            is { } attack
                and not "None"
            ? attack
        : Display(RecordValues.At(item.Properties, "ItemInfo.SpecificItemType"));

    private bool IsStaff(ClassRecord item) =>
        Display(item.Properties["Weapon Class"]) == "Staff"
        || (
            Display(RecordValues.At(item.Properties, "ItemInfo.WeaponType")) == "Halberd"
            && (
                item.Path.Contains("/Weapons/Staves/", StringComparison.Ordinal)
                || (
                    RecordValues
                        .Text(RecordValues.At(item.Properties, "ItemInfo.ItemName"))
                        ?.Contains("staff", StringComparison.OrdinalIgnoreCase) == true
                )
            )
        );

    /// <summary>
    /// E_SlotType calls the arms slot Back: gauntlets, bracers and sleeves of that slot fill the character sheet's
    /// ArmsSlot, which the armor-slots lab run read with Archer's Straps equipped.
    /// </summary>
    private static string? ArmorSlot(string? slot) => slot == "Back" ? "Arms" : slot;

    public string? Display(JsonNode? value) =>
        RecordValues.Text(value)
        ?? RecordValues.EnumDisplay(value)
        ?? (RecordValues.EnumValue(value) is { } name ? enumDisplays.GetValueOrDefault(name) : null)
        ?? RecordValues.String(value)
        ?? RecordValues.Number(value)?.ToString("G", CultureInfo.InvariantCulture)
        ?? (
            RecordValues.Boolean(value) is { } flag
                ? flag
                    ? "Yes"
                    : "No"
                : null
        );

    public ItemPage Resolve(ClassRecord item, string slug)
    {
        var properties = item.Properties;
        var name = RecordValues.Text(RecordValues.At(properties, "ItemInfo.ItemName"))!;
        var category = Display(RecordValues.At(properties, "ItemInfo.ItemType"));
        var component = components.GetValueOrDefault(item.Path);
        var componentPart = component is null ? null : ComponentValue(component, "Part");
        // CustomCurrencyShop's default shop functions select these inventory classes as currency.
        var currency = ItemIdentity.ClassName(item.Path) is "Iuba_Coin" or "Tithes";
        var readerCategory = ItemCategories.For(category, component is not null, currency);
        var type =
            currency ? "Currency"
            : component is not null ? componentPart
            : category switch
            {
                "Weapon" => WeaponType(item),
                "Armor" => ArmorSlot(Display(RecordValues.At(properties, "ItemInfo.ItemSlot"))),
                _ => readerCategory?.Name,
            };
        var facts = new List<Fact>();
        CodeFunction? projectileFunction = null;
        var projectileDamage = false;
        var ranged = type is "Gun" or "Pistol" or "Crossbow" or "Bow";
        IReadOnlyList<ProjectileGroup>? projectileGroups = null;
        void Add(Stat stat, string path)
        {
            var value = RecordValues.At(properties, path);
            facts.Add(
                RecordValues.Number(value) is { } number
                    ? StatVocabulary.Number(stat, number)
                    : StatVocabulary.Text(stat, Display(value))
            );
        }
        Add(Stat.Value, "ItemInfo.ItemValue");
        if (category is null)
        {
            facts.Add(StatVocabulary.Unknown(Stat.Category));
        }
        if (component is not null)
        {
            // Crafting's component selector and Retrieve Component Stats read these table fields.
            // They are contributions, not the item's inherited weapon defaults or final weapon stats.
            facts.Add(
                StatVocabulary.Number(Stat.DamageContribution, ComponentNumber(component, "Damage"))
            );
            facts.Add(
                StatVocabulary.Number(Stat.SpeedContribution, ComponentNumber(component, "Speed"))
            );
        }
        else if (category == "Weapon")
        {
            projectileDamage = type == "Staff" && Display(properties["Weapon Class"]) == "Staff";
            var hasSeparateProjectile =
                type == "Staff"
                && ItemIdentity.ClassName(item.Path) is "StaffOfMinorFlames" or "StaffOfMinorRot";
            if (
                (projectileDamage || hasSeparateProjectile)
                && code is not null
                && code.Functions.Any(function => function.Package == item.Path.Split('.')[0])
                && !code.Gaps.Any(gap => gap.Package == item.Path.Split('.')[0])
            )
            {
                projectileFunction = new[] { item.Path }
                    .Concat(item.Ancestors)
                    .Select(path =>
                        code?.Functions.SingleOrDefault(function =>
                            function.Package == path.Split('.')[0]
                            && function.Name == (projectileDamage ? "Staff Attack" : "Swing Event")
                        )
                    )
                    .FirstOrDefault(function => function is not null);
            }
            if (ranged)
            {
                projectileFunction = AddRangedFacts(facts, item, type!, out projectileGroups);
            }
            else if (projectileDamage)
            {
                facts.Add(
                    StatVocabulary.Number(Stat.Damage, NormalProjectileDamage(projectileFunction))
                );
                // A staff hits only through the projectile its Staff Attack spawns. One that spawns none deals no
                // damage, which the staff-damage-1 lab run saw for both Soulstaffs.
                if (
                    facts[^1].Number is null
                    && projectileFunction
                        is { ParseStatus: CodeParseStatus.Complete, SpawnedDamage.Count: 0 }
                )
                {
                    facts[^1] = StatVocabulary.Number(Stat.Damage, 0);
                }
                else if (facts[^1].Number is null)
                {
                    facts.Add(
                        StatVocabulary.Text(
                            Stat.WeaponBehavior,
                            "Normal-hit damage is unknown because its normal projectile's damage could not be read."
                        )
                    );
                }
            }
            else
            {
                Add(Stat.Damage, "WeaponInfo.Damage");
            }
            var damageIndex = facts.FindIndex(fact =>
                fact.Stat == (ranged ? Stat.BodyHitDamage : Stat.Damage)
            );
            var separateDamage = hasSeparateProjectile
                ? NormalProjectileDamage(projectileFunction)
                : null;
            var shownDamage = RecordValues.Number(RecordValues.At(properties, "ItemInfo.Damage"));
            // The tooltip contradicts the game only where its damage matches no damage that a normal attack applies,
            // so an unknown applied damage and a separate projectile's damage are no difference.
            if (
                (type == "Staff" || ranged)
                && shownDamage is { } shown
                && facts[damageIndex].Number is { } applied
                && applied != shown
                && separateDamage != shown
            )
            {
                facts[damageIndex] = facts[damageIndex] with
                {
                    GameShows = shown.ToString("G", CultureInfo.InvariantCulture),
                };
            }
            if (hasSeparateProjectile)
            {
                facts.Add(StatVocabulary.Number(Stat.ProjectileDamage, separateDamage));
            }
            // Tooltips show the authored speed word, not a universal attack-rate number.
            Add(Stat.Speed, "ItemInfo.Speed");
            var damageType = Display(RecordValues.At(properties, "WeaponInfo.DamageType"));
            if (damageType != "None")
            {
                facts.Add(StatVocabulary.Text(Stat.DamageType, damageType));
            }
            var magazine = RecordValues.Number(
                RecordValues.At(properties, "WeaponInfo.MagazineSize")
            );
            if (magazine is null || magazine > 0)
            {
                Add(Stat.MagazineSize, "WeaponInfo.MagazineSize");
            }
            if (
                Display(RecordValues.At(properties, "WeaponInfo.AttackType")) == "Halberd"
                && Display(properties["Weapon Class"]) != "Staff"
            )
            {
                untrainedWeapons.Add(item.Path);
            }
        }
        if (category == "Armor")
        {
            AddArmor(facts, item.Path, properties, name);
        }
        if (category == "Food")
        {
            AddFoodEffects(facts, item.Path, name);
        }
        else if (category == "Potion")
        {
            AddEffects(facts, RecordValues.At(properties, "ItemInfo.PotionInfo"));
        }
        if (ingredientRows.GetValueOrDefault(item.Path) is { } ingredient)
        {
            foreach (
                var (treatment, stat) in new[]
                {
                    ("Chopped", Stat.ChoppedEffect),
                    ("Cooled", Stat.CooledEffect),
                    ("Ground", Stat.GroundEffect),
                    ("Heated", Stat.HeatedEffect),
                }
            )
            {
                // Retrieve Prep Effects reads only the first effect of a treatment.
                var (key, value) = RecordValues
                    .MapEntries(ingredient.Values[treatment + "Effect"])
                    .FirstOrDefault();
                if (RecordValues.String(key) is not { } effectName)
                {
                    continue;
                }
                var amount = RecordValues.Number(value);
                facts.Add(
                    StatVocabulary.Effect(
                        stat,
                        amount is null ? null
                            : PointEffects.Contains(effectName) ? new Effect(effectName, amount, 0)
                            : new Effect(effectName, null, amount.Value)
                    )
                );
            }
        }
        var page = new ItemPage(
            item.Path,
            slug,
            name,
            RecordValues.Text(RecordValues.At(properties, "ItemInfo.Description")),
            new CategoryLink(
                ItemIdentity.Slug(readerCategory?.Name ?? "Unknown"),
                readerCategory?.Name ?? "Unknown"
            ),
            null,
            facts,
            []
        )
        {
            Type = type ?? "Unknown",
            ProjectileGroups = projectileGroups,
            OnHitEffects = category == "Weapon" ? ResolveOnHitEffects(item) : null,
        };
        claims.Record(item.Path, page, projectileFunction, projectileDamage);
        return page;
    }

    private const string WeaponRoot = "/Game/BloodlinesAlpha/Interactables/Weapons/";
    private const string ProjectileRoot = "/Game/GabrielAguiarProd/ToonProjectilesVol1/Blueprints/";
    private const string Enemy = "/Game/AIBehaviorToolkit/Blueprints/Goblin_Base";
    private const string StatusManager =
        "/Game/AIBehaviorToolkit/Blueprints/EnemyStatusEffectManager";

    private IReadOnlyList<OnHitEffect>? ResolveOnHitEffects(ClassRecord item)
    {
        var package = item.Path.Split('.')[0];
        var effects = new List<OnHitEffect>();
        var citations = new List<string>();
        var inputs = new List<string>();
        void Cite(string owner, params string[] names) =>
            citations.AddRange(names.Select(name => owner + ":" + name));
        void Manager(string effect)
        {
            Cite(Enemy, "Apply Projectile Debuff");
            Cite(
                StatusManager,
                "Apply " + effect + " Debuff",
                effect == "Poison" ? "Begin Poisoning" : "Begin Burn",
                "Stop " + effect
            );
        }
        void Debuff(ClassRecord? owner, string field, string trigger, double chance)
        {
            var debuff = owner?.Properties[field];
            if (debuff is null)
                throw new InvalidDataException($"Missing on-hit debuff for {item.Path}.");
            var effect = Display(debuff["DebuffType"]);
            if (effect is not ("Poison" or "Burn"))
                throw new InvalidDataException(
                    $"Unsupported on-hit debuff for {item.Path}: {effect}."
                );
            // Apply Projectile Debuff passes Damage and Duration to the manager. Its timer uses
            // a literal one-second interval, not DelayofrepeatingDamage or Numberoftimestoloop.
            effects.Add(
                new(
                    effect,
                    trigger,
                    chance,
                    RecordValues.Number(debuff["Damage"]),
                    1,
                    RecordValues.Number(debuff["Duration"]),
                    "Refreshes Without Stacking"
                )
            );
            inputs.AddRange(
                new[] { field + ".DebuffType", field + ".Damage", field + ".Duration" }.Select(
                    path => owner!.Path + ":" + path
                )
            );
            Manager(effect);
        }
        void Ignite(string trigger, double chance)
        {
            // Goblin_Base:Ignite passes literal damage 15 and duration 5 to the burn manager.
            effects.Add(
                new(
                    "Burn",
                    trigger,
                    chance,
                    15,
                    1,
                    5,
                    "Does Not Refresh While Burning. Fire-Resistant Targets Are Immune."
                )
            );
            Cite(Enemy, "Ignite");
            Cite(StatusManager, "Apply Burn Debuff", "Begin Burn", "Stop Burn");
        }
        switch (package)
        {
            case WeaponRoot + "Spears/PoisonPike":
                // Enemy Hit Event succeeds only when RandomIntegerInRange(0, 4) returns zero.
                Debuff(item, "Debuff", "Melee Hit", 1d / 5);
                Cite(package, "Enemy Hit Event");
                break;
            case WeaponRoot + "1HandedWeapons/Torch":
                // Enemy Hit Event succeeds only when RandomIntegerInRange(0, 5) returns zero.
                Ignite("Melee Hit", 1d / 6);
                Cite(package, "Enemy Hit Event");
                break;
            case WeaponRoot + "Halberds/StaffOfMinorFlames":
            case WeaponRoot + "Halberds/StaffOfMinorRot":
                var projectileName = package.EndsWith(
                    "StaffOfMinorFlames",
                    StringComparison.Ordinal
                )
                    ? "BP_Projectile_Fireball01Orange_Weak"
                    : "BP_Projectile_PoisonBall_Weak";
                var projectile = ProjectileRoot + projectileName;
                Debuff(
                    classes?.SingleOrDefault(record =>
                        record.Path == projectile + "." + projectileName + "_C"
                    ),
                    "Debuff Info",
                    "Projectile Impact",
                    1
                );
                Cite(package, "Swing Event");
                Cite(projectile, "Effect on Enemy Upon Impact");
                break;
            case WeaponRoot + "Crossbows/HolyNooseCrossbow":
                Ignite("Firing While Aiming At An Undead Enemy", 1);
                effects.Add(
                    new(
                        "Fire Ground",
                        "Firing While Aiming At An Undead Enemy",
                        1,
                        null,
                        null,
                        3,
                        null
                    )
                );
                Cite(package, "Custom Fire Event");
                break;
            case WeaponRoot + "Daggers/VeiledSlicers":
                // Regen Mana passes the literal five to FoodManager:Replenish Mana.
                effects.Add(new("Restore Mana", "Melee Hit", 1, 5, null, null, null));
                Cite(package, "ReceiveBeginPlay", "Regen Mana");
                Cite("/Game/BloodlinesAlpha/UI/CharacterSheet/FoodManager", "Replenish Mana");
                break;
            default:
                return null;
        }
        claims.RecordOnHit(item.Path, inputs, citations);
        return effects;
    }

    private IReadOnlyList<FiredProjectile>? RangedProjectiles(RangedFire? analysis)
    {
        bool Projectile(FiredProjectile actor)
        {
            var record = classes?.FirstOrDefault(record => record.Path == actor.Class);
            // HolyNooseCrossbow:Custom Fire Event can also create FireGround, which is an effect, not a projectile.
            return record is null
                || record.Properties.ContainsKey("Damage")
                || record.Properties.ContainsKey("ProjectileMovement");
        }
        IReadOnlyList<FiredProjectile> Filter(IReadOnlyList<FiredProjectile> actors) =>
            actors.All(Projectile) ? actors : actors.Where(Projectile).ToArray();
        if (analysis?.Projectiles is { } projectiles)
            return Filter(projectiles);
        if (analysis?.Alternatives is not { Count: > 0 } alternatives)
            return null;
        var filtered = alternatives.Select(Filter).ToArray();
        var first = filtered[0];
        if (filtered.Skip(1).All(actors => actors.SequenceEqual(first)))
            return first;
        if (
            filtered.Any(actors =>
                actors.Count != first.Count
                || !actors
                    .Select(actor => actor.Class)
                    .SequenceEqual(first.Select(actor => actor.Class))
            )
        )
            return null;
        return first
            .Select(
                (actor, index) =>
                    filtered.All(actors => actors[index] == actor)
                        ? actor
                        : actor with
                        {
                            Assigned = true,
                            Damage = null,
                        }
            )
            .ToArray();
    }

    private CodeFunction? AddRangedFacts(
        List<Fact> facts,
        ClassRecord item,
        string type,
        out IReadOnlyList<ProjectileGroup>? projectileGroups
    )
    {
        var properties = item.Properties;
        var damage = RecordValues.Number(RecordValues.At(properties, "WeaponInfo.Damage"));
        var itemDamage = RecordValues.Number(RecordValues.At(properties, "ItemInfo.Damage"));
        var chain = new[] { item.Path }.Concat(item.Ancestors).ToArray();
        CodeFunction? Entry(string name) =>
            chain
                .Select(path =>
                    code?.Functions.SingleOrDefault(function =>
                        function.Package == path.Split('.')[0] && function.Name == name
                    )
                )
                .FirstOrDefault(function => function is not null);
        var fire = Entry(
            type == "Crossbow" ? "Custom Fire Event"
            : type == "Bow" ? "Custom Shoot Bow Event"
            : "Swing Event"
        );
        var resolved = RangedProjectiles(fire?.RangedFire);
        var complete =
            code is not null
            && chain
                .Where(path => !path.StartsWith("/Script/", StringComparison.Ordinal))
                .All(path =>
                    classes?.Any(record => record.Path == path) == true
                    && !code.Gaps.Any(gap => gap.Package == path.Split('.')[0])
                )
            && fire is { ParseStatus: CodeParseStatus.Complete, RangedFire: not null }
            && resolved is not null
            && !fire.RangedFire.DeferredCleanup;
        projectileGroups = null;
        double? charged = null;
        if (complete)
        {
            var analysis = fire!.RangedFire!;
            var projectiles = resolved!;
            var groups = new List<ProjectileGroup>();
            double? Applied(FiredProjectile projectile, double charge)
            {
                static bool PlayerProjectile(string path) =>
                    path
                        is "/Game/BloodlinesAlpha/Weapons/Bullet.Bullet_C"
                            or "/Game/BloodlinesAlpha/Weapons/Arrow.Arrow_C"
                            or "/Game/BloodlinesAlpha/Weapons/CrossBowBolt.CrossBowBolt_C";
                var actor = classes?.FirstOrDefault(record => record.Path == projectile.Class);
                if (
                    actor is null
                    || (!PlayerProjectile(actor.Path) && !actor.Ancestors.Any(PlayerProjectile))
                )
                    return null;
                return projectile.Assigned
                    ? projectile.Damage?.At(itemDamage, damage, charge)
                    : RecordValues.Number(actor.Properties["Damage"]);
            }
            ProjectileGroup Group(int count, double? body) =>
                new(
                    count,
                    body,
                    body is { } applied && damage is { } bonus ? applied + bonus : null
                );
            foreach (var group in projectiles.GroupBy(projectile => Applied(projectile, 0)))
                groups.Add(Group(group.Count(), group.Key));
            if (type is "Gun" or "Pistol")
            {
                var range = RecordValues.Number(RecordValues.At(properties, "WeaponInfo.Range"));
                var shotgun = code!.Functions.SingleOrDefault(function =>
                    function.Package
                        == "/Game/BloodlinesAlpha/Blueprints/FirstPerson2/FirstPersonCharacter2"
                    && function.Name == "Calculate Random Shotgun Projectiles"
                );
                var genericCount =
                    range is null ? (int?)null
                    : range == 0 ? 1
                    : shotgun
                        is {
                            ParseStatus: CodeParseStatus.Complete,
                            RangedFire.Projectiles: not null
                        }
                        ? shotgun.RangedFire.Projectiles.Count
                    : null;
                // FirstPersonCharacter2:Calculate Random Shotgun Projectiles emits one immediately, then delayed pellets.
                // Swing Event's synchronous cleanup removes only the immediate bullet, not those emitted afterwards.
                if (genericCount is { } count)
                {
                    var remaining = count - (analysis.ReplacesGeneric ? 1 : 0);
                    if (remaining > 0)
                        groups.Add(
                            Group(
                                remaining,
                                RecordValues.Number(
                                    classes
                                        ?.FirstOrDefault(record =>
                                            record.Path
                                            == "/Game/BloodlinesAlpha/Weapons/Bullet.Bullet_C"
                                        )
                                        ?.Properties["Damage"]
                                )
                            )
                        );
                    projectileGroups = groups;
                }
            }
            else
            {
                // FirstPersonCharacterBP_Modular1:AnimNotify_Spawn Arrow assigns WeaponInfo.Damage before the bow override.
                if (type == "Bow" && !analysis.ReplacesGeneric)
                    groups.Add(Group(1, damage));
                projectileGroups = groups;
                if (type == "Crossbow" && analysis.ReplacesGeneric)
                    groups.Add(Group(1, damage) with { Condition = "At close range, also" });
            }
            if (
                type == "Crossbow"
                && analysis.MaximumCharge is > 0 and var maximum
                && projectiles.Count > 0
            )
                charged = Applied(projectiles[0], maximum);
        }
        facts.Add(
            StatVocabulary.Number(
                Stat.BodyHitDamage,
                projectileGroups?.FirstOrDefault()?.BodyHitDamage
            )
        );
        facts.Add(
            StatVocabulary.Number(
                Stat.HeadHitDamage,
                projectileGroups?.FirstOrDefault()?.HeadHitDamage
            )
        );
        facts.Add(
            StatVocabulary.Number(
                Stat.ProjectilesPerShot,
                projectileGroups?.Where(group => group.Condition is null).Sum(group => group.Count)
            )
        );
        if (fire?.RangedFire?.MaximumCharge is > 0 && type == "Crossbow")
            facts.Add(StatVocabulary.Number(Stat.ChargedBodyHitDamage, charged));
        return fire;
    }

    private double? NormalProjectileDamage(CodeFunction? function)
    {
        if (
            function?.ParseStatus != CodeParseStatus.Complete
            || function.SpawnedDamage is not { Count: > 0 } spawned
        )
            return null;
        var values = spawned
            .Select(actor =>
                actor.Assigned
                    ? actor.Damage
                    : RecordValues.Number(
                        classes
                            ?.FirstOrDefault(type => type.Path == actor.Class)
                            ?.Properties["Damage"]
                    )
            )
            .Distinct()
            .ToArray();
        return values is [{ } damage] ? damage : null;
    }

    /// <summary>
    /// The armor class and rating that equipping the item applies, as Get Armor Slot finds them: crafted armor
    /// keeps its own value, other armor takes the ArmorDataTable row named after the item, and armor without
    /// such a row counts as Unarmored with no armor.
    /// </summary>
    private void AddArmor(List<Fact> facts, string path, JsonObject properties, string name)
    {
        var slot = Display(RecordValues.At(properties, "ItemInfo.ItemSlot"));
        if (slot is null || armorRows is null)
        {
            facts.Add(StatVocabulary.Unknown(Stat.ArmorType));
            facts.Add(StatVocabulary.Unknown(Stat.ArmorRating));
        }
        else if (!ArmoredSlots.Contains(slot))
        {
            facts.Add(StatVocabulary.Number(Stat.ArmorRating, 0));
        }
        else if (CraftedArmor.Contains(path))
        {
            facts.Add(StatVocabulary.Text(Stat.ArmorType, "Custom"));
            Add(Stat.ArmorRating, RecordValues.At(properties, "ItemInfo.Damage"));
        }
        else if (armorRows.Named(name) is { } row)
        {
            facts.Add(StatVocabulary.Text(Stat.ArmorType, Display(row.Values["ArmorClass"])));
            Add(Stat.ArmorRating, row.Values["ArmorValue"]);
        }
        else
        {
            facts.Add(StatVocabulary.Text(Stat.ArmorType, "Unarmored"));
            facts.Add(StatVocabulary.Number(Stat.ArmorRating, 0));
        }

        void Add(Stat stat, JsonNode? value) =>
            facts.Add(StatVocabulary.Number(stat, RecordValues.Number(value)));
    }

    /// <summary>
    /// What eating the food applies, as FoodManager:Get Item finds it: the FoodEffectDataTable row named after
    /// the food, or else the first row whose localized food name is the food's name, with each of its effects
    /// that the manager's switch names. A food without such a row or effect has no effect.
    /// </summary>
    private void AddFoodEffects(List<Fact> facts, string path, string name)
    {
        if (foodRows is null)
        {
            facts.Add(StatVocabulary.Unknown(Stat.Effects));
            return;
        }
        var row =
            foodRows.Named(name)
            ?? foodRows.Rows.FirstOrDefault(row =>
                RecordValues.Text(row.Values["LocalizedFoodName"]) == name
            );
        var count = 0;
        foreach (var (key, value) in RecordValues.MapEntries(row?.Values["FoodEffects"]))
        {
            if (
                RecordValues.String(key) is not { } effectName
                || !FoodEffects.TryGetValue(effectName, out var effect)
            )
            {
                ignoredFoodEffects.Add(path);
                continue;
            }
            var amount = RecordValues.Number(value);
            count++;
            // Healing keeps its restoration amount for the list's Heals column, and reads as Restore Health among
            // the effects, as potions and ingredients name it.
            if (effect.Value == FoodValue.Heal)
            {
                facts.Add(StatVocabulary.Number(Stat.RestorationAmount, amount));
            }
            facts.Add(
                StatVocabulary.Effect(
                    Stat.Effect,
                    effect.Value switch
                    {
                        FoodValue.Unused => new Effect(effect.Name, null, 0),
                        _ when amount is null => null,
                        FoodValue.Heal => new Effect("Restore Health", amount, 0),
                        FoodValue.Amount => new Effect(effect.Name, amount, 0),
                        _ => new Effect(effect.Name, effect.Power, amount.Value),
                    },
                    "Effect " + count.ToString(CultureInfo.InvariantCulture)
                )
            );
        }
        if (count == 0)
        {
            facts.Add(StatVocabulary.Text(Stat.Effects, "None"));
        }
    }

    /// <summary>
    /// A data table's rows in table order, found by row name the way the game finds them for an item: by its
    /// English name without spaces and apostrophes, compared as an FName without regard to case.
    /// </summary>
    private sealed class RowLookup(IReadOnlyList<TableRow> rows)
    {
        private readonly Dictionary<string, TableRow> named = rows.GroupBy(
                row => row.Name,
                StringComparer.OrdinalIgnoreCase
            )
            .ToDictionary(
                group => group.Key,
                group => group.First(),
                StringComparer.OrdinalIgnoreCase
            );

        public IReadOnlyList<TableRow> Rows => rows;

        public TableRow? Named(string itemName) =>
            named.GetValueOrDefault(
                itemName
                    .Replace(" ", "", StringComparison.Ordinal)
                    .Replace("'", "", StringComparison.Ordinal)
            );

        public static RowLookup? Find(IReadOnlyList<TableRecord>? tables, string table)
        {
            var records = (tables ?? [])
                .Where(record => ItemIdentity.ClassName(record.Path) == table)
                .ToArray();
            return records.Length == 0
                ? null
                : new(records.SelectMany(record => record.Rows).ToArray());
        }
    }

    private string? ComponentValue(IReadOnlyList<TableRow> rows, string field)
    {
        var value = Display(rows[0].Values[field]);
        // A class can occur in several rows with conflicting stats. No row is authoritative.
        for (var index = 1; index < rows.Count; index++)
        {
            if (Display(rows[index].Values[field]) != value)
            {
                return null;
            }
        }
        return value;
    }

    private static double? ComponentNumber(IReadOnlyList<TableRow> rows, string field)
    {
        var value = RecordValues.Number(rows[0].Values[field]);
        // A class can occur in several rows with conflicting stats. No row is authoritative.
        for (var index = 1; index < rows.Count; index++)
        {
            if (RecordValues.Number(rows[index].Values[field]) != value)
            {
                return null;
            }
        }
        return value;
    }

    /// <summary>
    /// Drinking a potion runs FoodManager:Drink Potion, which passes each of the five effect slots to Find Any Apply
    /// Potion Effects as its EffectNPower, truncated, for the effect number of PotionEffectTranslatorTable, and its
    /// DurationofEffectN as the amount. The potion crafting window writes potions that way. Neither function reads
    /// PotionEffects, the restoration amount or the damage fields, so they are no facts of the potion. Lab runs
    /// potions-1101 to potions-1101c drank the four bought potions: only Dwarfs Flaggon's Improved Jumping 5 for 60 s,
    /// which is effect number 5, did anything, and restored 60 stamina.
    /// </summary>
    private void AddEffects(List<Fact> facts, JsonNode? potion)
    {
        if (potionEffectNames is null || potion is null)
        {
            facts.Add(StatVocabulary.Unknown(Stat.Effects));
            return;
        }
        var count = 0;
        for (var slot = 1; slot <= 5; slot++)
        {
            var number = slot.ToString(CultureInfo.InvariantCulture);
            var power = RecordValues.Number(RecordValues.At(potion, "Effect" + number + "Power"));
            var amount = RecordValues.Number(RecordValues.At(potion, "DurationofEffect" + number));
            if (
                power is not { } id
                || !PotionDispatch.TryGetValue((int)Math.Truncate(id), out var dispatch)
            )
            {
                continue;
            }
            var name = potionEffectNames.Rows.FirstOrDefault(row =>
                RecordValues.Number(row.Values["EffectInt"]) == Math.Truncate(id)
            );
            count++;
            facts.Add(
                StatVocabulary.Effect(
                    Stat.Effect,
                    name is null || amount is null
                        ? null
                        : RecordValues.Text(name.Values["LocalizedEffectName"]) switch
                        {
                            null => null,
                            var text when dispatch.Value == FoodValue.Amount => new Effect(
                                text,
                                amount,
                                0
                            ),
                            var text => new Effect(text, dispatch.Power, amount.Value),
                        },
                    "Effect " + number
                )
            );
        }
        if (count == 0)
        {
            facts.Add(StatVocabulary.Text(Stat.Effects, "None"));
        }
    }

    private string? JoinEffects(IEnumerable<JsonNode?> effects)
    {
        var names = effects.Select(Display).ToArray();
        return names.Any(name => name is null) ? null : string.Join(", ", names);
    }
}
