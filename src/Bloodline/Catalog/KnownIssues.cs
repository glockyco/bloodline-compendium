using System.Text.RegularExpressions;
using Bloodline.Output;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

/// <summary>
/// Reads <c>issues/known-issues.json</c> and notes its issues on the item pages whose shown values they affect.
/// A note or tooltip cause that names an issue the registry lacks fails the catalog.
/// An item-specific sentence for an item that does not note its issue also fails the catalog.
/// </summary>
internal static partial class KnownIssues
{
    /// <summary>Knee armor counts only after another armor piece is put on or taken off.</summary>
    public const string KneeArmor = "knee-armor-not-counted";

    /// <summary>A food lists an effect that the food manager's switch does not name.</summary>
    public const string IgnoredFoodEffect = "food-effect-not-applied";

    /// <summary>A food that eating ignores entirely, because the food effect table has no row for it.</summary>
    public const string FoodWithoutRow = "food-without-row";

    /// <summary>Foods whose rows restore other amounts than their descriptions say.</summary>
    public const string MisdescribedFood = "food-description-mismatch";

    /// <summary>Bought potions whose effect slots name no effect that using them applies.</summary>
    public const string PotionsWithoutEffect = "potions-without-effect";

    /// <summary>
    /// The potions that lab runs potions-1101 to potions-1101c used, with the one effect each applied. Only Dwarfs
    /// Flaggon did anything, by restoring stamina.
    /// </summary>
    private static readonly Dictionary<string, string?> IneffectivePotions = new(
        StringComparer.Ordinal
    )
    {
        ["/Game/BloodlinesAlpha/Interactables/HealthPotion.HealthPotion_C"] = null,
        ["/Game/ManaPotion.ManaPotion_C"] = null,
        ["/Game/BloodlinesAlpha/Interactables/CastlesideCider.CastlesideCider_C"] = null,
        ["/Game/BloodlinesAlpha/Interactables/TheDwarfsFlaggonBeer.TheDwarfsFlaggonBeer_C"] =
            "Restore Stamina",
    };

    /// <summary>Staves whose description promises fireballs that their swings never launch.</summary>
    public const string NoFireballs = "staves-without-fireballs";

    /// <summary>Reproduced staves can deal default projectile damage at point-blank range.</summary>
    public const string StaffPointBlankDamage = "staff-point-blank-damage";

    /// <summary>Weapons whose tooltip damage differs from their applied ordinary-hit damage.</summary>
    public const string WeaponTooltipDamage = "weapon-tooltip-damage";

    /// <summary>Halberd-type weapons, staves of other classes among them, whose hits train no proficiency.</summary>
    public const string NoProficiency = "halberds-train-no-proficiency";

    // Only staves whose point-blank damage was reproduced receive this note, not every late damage write.
    private static readonly HashSet<string> PointBlankStaves = new(StringComparer.Ordinal)
    {
        "/Game/BloodlinesAlpha/Interactables/Weapons/Staves/AcolyteGeomancerStaff.AcolyteGeomancerStaff_C",
        "/Game/BloodlinesAlpha/Interactables/Weapons/Halberds/WalkingStick.WalkingStick_C",
        "/Game/BloodlinesAlpha/Interactables/Weapons/Staves/MoonstoneStaff.MoonstoneStaff_C",
    };

    /// <summary>
    /// The staves with Staff of Minor Flames' fireball description whose normal swings lab runs showed launching
    /// no projectile and spending no mana.
    /// </summary>
    private static readonly HashSet<string> FirelessStaves = new(StringComparer.Ordinal)
    {
        "/Game/BloodlinesAlpha/Interactables/Weapons/Halberds/FairyStaff.FairyStaff_C",
        "/Game/BloodlinesAlpha/Interactables/Weapons/Halberds/NecroStaff.NecroStaff_C",
        "/Game/BloodlinesAlpha/Interactables/Weapons/Halberds/DeathsGrasp_Soulstaff.DeathsGrasp_Soulstaff_C",
        "/Game/BloodlinesAlpha/Interactables/Weapons/Halberds/PriestsSoulStaff.PriestsSoulStaff_C",
    };

    /// <summary>
    /// The foods whose descriptions promise healing and that lab runs showed doing nothing when eaten, with
    /// <c>tools/runtime/checks/issues.sh</c>. Other foods without an effect promise none, or promise something the
    /// runs could not observe.
    /// </summary>
    private static readonly HashSet<string> UneatenFoods = new(StringComparer.Ordinal)
    {
        "/Game/BloodlinesAlpha/Interactables/Steak.Steak_C",
        "/Game/BloodlinesAlpha/Interactables/Soup.Soup_C",
        "/Game/BloodlinesAlpha/Interactables/TukeyLeg.TukeyLeg_C",
        "/Game/BloodlinesAlpha/Interactables/Crafting_Items/CookedWrangler.CookedWrangler_C",
        "/Game/BloodlinesAlpha/Interactables/WranglerSandwich.WranglerSandwich_C",
        "/Game/BloodlinesAlpha/Interactables/Crafting_Items/PotatoSeeds.PotatoSeeds_C",
    };

    /// <summary>
    /// The foods that lab run food-desc-1101 ate, with the health their rows restore. Buttered Bread's description
    /// swaps its health and mana, and Spud Colossus' promises stamina it does not restore.
    /// </summary>
    private static readonly Dictionary<string, double> MisdescribedFoods = new(
        StringComparer.Ordinal
    )
    {
        [
            "/Game/BloodlinesAlpha/Interactables/Crafting_Items/GeneralFoodItems/ButteredBread.ButteredBread_C"
        ] = 15,
        ["/Game/BloodlinesAlpha/Interactables/Crafting_Items/SpudColossus.SpudColossus_C"] = 200,
    };

    /// <summary>Items whose tooltips show another item's name above a description of something else.</summary>
    public const string CopiedName = "copied-item-names";

    /// <summary>The items whose copied names the tooltip sweep read on screen.</summary>
    private static readonly HashSet<string> CopiedNames = new(StringComparer.Ordinal)
    {
        "/Game/BloodlinesAlpha/Interactables/Crafting_Items/Ore_And_Ingots/EarthOrb.EarthOrb_C",
        "/Game/BloodlinesAlpha/Interactables/Crafting_Items/Ore_And_Ingots/Whisperquartz.Whisperquartz_C",
        "/Game/BloodlinesAlpha/Interactables/Crafting_Items/WeaponComponents/WC_TerrashardWandCore.WC_TerrashardWandCore_C",
        "/Game/BloodlinesAlpha/Interactables/Weapons/1HandedWeapons/1HAXE_Woodaxe.1HAXE_Woodaxe_C",
        "/Game/BloodlinesAlpha/Interactables/Crafting_Items/WeaponComponents/WC_BonemetalBlade.WC_BonemetalBlade_C",
    };

    /// <summary>A crossbow whose class is no crossbow, so firing it does nothing.</summary>
    public const string SilentCrossbow = "rusty-crossbow-fires-nothing";

    /// <summary>
    /// Issues that runs of both waitresses' orders observed, whose registry notes name each item they concern: a
    /// seller whose line names one price while the purchase takes another, and the Rum order that gives Ale.
    /// </summary>
    private static readonly string[] OrderIssues =
    [
        "sellers-name-another-price",
        "rum-order-gives-ale",
    ];

    /// <summary>
    /// The Rusty Crossbow that merchants sell and Squalls End holds, which runs ranged1 to ranged3 saw fire nothing and
    /// run shops1 found in the stock of the shopkeepers of Squalls End and Elderglen.
    /// </summary>
    private const string RustyCrossbow =
        "/Game/BloodlinesAlpha/Interactables/Weapons/Crossbows/RustyCrossbow.RustyCrossbow_C";

    /// <summary>Placed items that disappear once another placed item with their Tag is taken.</summary>
    public const string SharedTag = "shared-save-tags";

    /// <summary>
    /// The items that lab runs placed1 and twins1 to twins3 saw disappear after another placed item with the same Tag
    /// was taken. The Briarwatch debris of Whitebrook Haven came back and stay out.
    /// </summary>
    private static readonly HashSet<string> TwinItems = new(StringComparer.Ordinal)
    {
        "/Game/BloodlinesAlpha/Interactables/Armor/Arms/WebspinnerGauntlets.WebspinnerGauntlets_C",
        "/Game/BloodlinesAlpha/Interactables/Armor/ETC/LifebinderRing.LifebinderRing_C",
        "/Game/BloodlinesAlpha/Interactables/Armor/Legs/LegArmor_Sparkbottoms.LegArmor_Sparkbottoms_C",
        "/Game/BloodlinesAlpha/Interactables/BagOfGold100gp.BagOfGold100gp_C",
        "/Game/BloodlinesAlpha/Interactables/BoneDust_Item.BoneDust_Item_C",
        "/Game/BloodlinesAlpha/Interactables/Crafting_Items/FineCloth.FineCloth_C",
        "/Game/BloodlinesAlpha/Interactables/Crafting_Items/GenericPotions/MediumHealthPotion.MediumHealthPotion_C",
        "/Game/BloodlinesAlpha/Interactables/Crafting_Items/GenericPotions/MediumStaminaPotion.MediumStaminaPotion_C",
        "/Game/BloodlinesAlpha/Interactables/Crafting_Items/Ore_And_Ingots/CryoliteBloom.CryoliteBloom_C",
        "/Game/BloodlinesAlpha/Interactables/Crafting_Items/Ore_And_Ingots/EarthshardHeart.EarthshardHeart_C",
        "/Game/BloodlinesAlpha/Interactables/Crafting_Items/Ore_And_Ingots/Emberglass.Emberglass_C",
        "/Game/BloodlinesAlpha/Interactables/Crafting_Items/Ore_And_Ingots/GoldIngot.GoldIngot_C",
        "/Game/BloodlinesAlpha/Interactables/Crafting_Items/Ore_And_Ingots/IronIngot.IronIngot_C",
        "/Game/BloodlinesAlpha/Interactables/Crafting_Items/Ore_And_Ingots/MystcopperIngot.MystcopperIngot_C",
        "/Game/BloodlinesAlpha/Interactables/Crafting_Items/Ore_And_Ingots/SilverIngot.SilverIngot_C",
        "/Game/BloodlinesAlpha/Interactables/Crafting_Items/Ore_And_Ingots/Stone.Stone_C",
        "/Game/BloodlinesAlpha/Interactables/Crafting_Items/Ore_And_Ingots/StonegemCore.StonegemCore_C",
        "/Game/BloodlinesAlpha/Interactables/Crafting_Items/Ore_And_Ingots/Whisperquartz.Whisperquartz_C",
        "/Game/BloodlinesAlpha/Interactables/Weapons/1HandedWeapons/1HSWORD_LonlandSword.1HSWORD_LonlandSword_C",
        "/Game/BloodlinesAlpha/Interactables/Weapons/2H_Weapons/TwoHanded_Tool_Weapons/MiningPickaxe.MiningPickaxe_C",
    };

    public static KnownIssueRegistry Load(string path)
    {
        var registry = CanonicalJson.Read<KnownIssueRegistry>(path);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var issue in registry.Issues)
        {
            if (!Id().IsMatch(issue.Id) || !ids.Add(issue.Id))
            {
                throw new InvalidDataException(
                    $"Known issue ID {issue.Id} is not unique lowercase words joined by hyphens."
                );
            }
            if (issue.Steps.Count == 0 || issue.Builds.Count == 0 || issue.Citations.Count == 0)
            {
                throw new InvalidDataException(
                    $"Known issue {issue.Id} needs steps, code citations and a confirmed build."
                );
            }
            if (issue.Effect is null && issue.EffectDetails is not { Count: > 0 })
            {
                throw new InvalidDataException(
                    $"Known issue {issue.Id} needs a player-facing effect."
                );
            }
            if ((issue.Report == ReportStatus.Reported) != (issue.ReportUrl is not null))
            {
                throw new InvalidDataException(
                    $"Known issue {issue.Id} needs a report link exactly when it is reported."
                );
            }
        }
        return registry;
    }

    /// <summary>
    /// Adds notes for tooltip differences concerning shown values, knee armor, ignored food effects,
    /// foods that eating ignores, foods whose descriptions misstate what they restore, potions without their effects, staves without their promised
    /// fireballs, reproduced point-blank staff damage and
    /// weapons whose hits train no proficiency.
    /// </summary>
    public static IReadOnlyList<ItemPage> Attach(
        IReadOnlyList<ItemPage> items,
        KnownIssueRegistry registry,
        TooltipReport? tooltips,
        IReadOnlySet<string> ignoredFoodEffects,
        IReadOnlySet<string> untrainedWeapons
    )
    {
        var issues = registry.Issues.ToDictionary(issue => issue.Id, StringComparer.Ordinal);
        var notes = items.ToDictionary(
            item => item.Key,
            _ => new SortedSet<string>(StringComparer.Ordinal),
            StringComparer.Ordinal
        );
        var causes = TooltipCheck.Reasons.ToDictionary(reason => reason.Id, StringComparer.Ordinal);
        var tooltipValues = (tooltips?.Differences ?? [])
            .Where(difference =>
                difference.Catalog is null
                && difference.Reason is not null
                && difference.Tooltip is not null
            )
            .GroupBy(difference => (difference.Item, difference.Reason))
            .ToDictionary(
                group => group.Key,
                group =>
                    (IReadOnlyDictionary<string, string>)
                        group.ToDictionary(
                            difference => difference.Field,
                            difference => difference.Tooltip!
                        )
            );
        KnownIssue Find(string id, string user) =>
            issues.TryGetValue(id, out var issue)
                ? issue
                : throw new InvalidDataException(
                    $"{user} names known issue {id}, which issues/known-issues.json does not contain."
                );
        foreach (var difference in tooltips?.Differences ?? [])
        {
            if (difference.Reason is not { } reason || !causes[reason].Issue)
            {
                continue;
            }
            Find(reason, $"The tooltip difference of {difference.Name} in {difference.Field}");
            // Shields need a damage note even though no ordinary-hit damage fact exists.
            if (difference.Catalog is not null || reason == WeaponTooltipDamage)
            {
                notes[difference.Item].Add(reason);
            }
        }
        foreach (var item in items)
        {
            if (item.Category.Name == "Armor" && item.Type == "Knees")
            {
                notes[item.Key].Add(KneeArmor);
            }
            if (ignoredFoodEffects.Contains(item.Key))
            {
                notes[item.Key].Add(IgnoredFoodEffect);
            }
            // A later build that gives such a food an effect drops the note, because Effects is then not None.
            if (
                UneatenFoods.Contains(item.Key)
                && item.Facts.Any(fact => fact.Stat == Stat.Effects && fact.Text == "None")
            )
            {
                notes[item.Key].Add(FoodWithoutRow);
            }
            // A later build that changes such a food's healing drops the note, since its row no longer matches.
            if (
                MisdescribedFoods.TryGetValue(item.Key, out var health)
                && item.Facts.Any(fact =>
                    fact.Effect is { Name: "Restore Health" } effect && effect.Power == health
                )
            )
            {
                notes[item.Key].Add(MisdescribedFood);
            }
            // A later build that gives such a potion other effects drops the note, since its effects then differ.
            if (
                IneffectivePotions.TryGetValue(item.Key, out var applied)
                && (
                    applied is null
                        ? item.Facts.Any(fact => fact.Stat == Stat.Effects && fact.Text == "None")
                        : item
                            .Facts.Where(fact => fact.Stat == Stat.Effect)
                            .Select(fact => fact.Effect?.Name)
                            .SequenceEqual([applied])
                )
            )
            {
                notes[item.Key].Add(PotionsWithoutEffect);
            }
            if (FirelessStaves.Contains(item.Key))
            {
                notes[item.Key].Add(NoFireballs);
            }
            if (PointBlankStaves.Contains(item.Key))
            {
                notes[item.Key].Add(StaffPointBlankDamage);
            }
            if (untrainedWeapons.Contains(item.Key))
            {
                notes[item.Key].Add(NoProficiency);
            }
            if (CopiedNames.Contains(item.Key))
            {
                notes[item.Key].Add(CopiedName);
            }
            if (TwinItems.Contains(item.Key))
            {
                notes[item.Key].Add(SharedTag);
            }
            if (item.Key == RustyCrossbow)
            {
                notes[item.Key].Add(SilentCrossbow);
            }
            if (
                item.Key
                == "/Game/BloodlinesAlpha/Interactables/Crafting_Items/Herbs_And_Roots/LoveMeNotFlower.LoveMeNotFlower_C"
            )
            {
                notes[item.Key].Add("foragers-feast-gives-flower");
            }
            // Runs quoted-prices and rum-gives-ale bought every order of the two waitresses, and each entry's notes
            // name the items it concerns.
            foreach (
                var issue in registry.Issues.Where(issue =>
                    OrderIssues.Contains(issue.Id) && issue.Notes?.ContainsKey(item.Key) == true
                )
            )
            {
                notes[item.Key].Add(issue.Id);
            }
        }
        // An item's Bugs rows follow the order of the Known Issues page, which is the registry's.
        var order = registry
            .Issues.Select((issue, index) => (issue.Id, index))
            .ToDictionary(entry => entry.Id, entry => entry.index, StringComparer.Ordinal);
        return items
            .Select(item =>
                item with
                {
                    Notes = notes[item.Key]
                        .Select(id => Find(id, "The page of " + item.Name))
                        .OrderBy(issue => order[issue.Id])
                        .Select(issue =>
                        {
                            var note = issue.Notes?.GetValueOrDefault(item.Key);
                            var effect = issue.Id switch
                            {
                                "armor-without-row" or "armor-slot-adds-none" =>
                                    $"Wearing {item.Name} adds no armor.",
                                NoProficiency => $"{item.Name} trains no weapon proficiency.",
                                WeaponTooltipDamage => WeaponDamageNote(item),
                                _ => note?.Effect
                                    ?? issue.Effect
                                    ?? throw new InvalidDataException(
                                        $"Known issue {issue.Id} needs an effect for {item.Name}."
                                    ),
                            };
                            return new IssueNote(issue.Id, effect)
                            {
                                Workaround = note is null ? issue.Workaround : note.Workaround,
                                TooltipValues = tooltipValues.GetValueOrDefault(
                                    (item.Key, issue.Id)
                                ),
                            };
                        })
                        .ToArray(),
                }
            )
            .ToArray();
    }

    /// <summary>
    /// The notes the registry gives an entity of a kind without derived notes, such as a discipline or a skill: each
    /// issue whose notes name its key, in the registry's order.
    /// </summary>
    public static IReadOnlyList<IssueNote> RegistryNotes(KnownIssueRegistry registry, string key) =>
        registry
            .Issues.Where(issue => issue.Notes?.ContainsKey(key) == true)
            .Select(issue =>
            {
                var note = issue.Notes![key];
                return new IssueNote(issue.Id, note.Effect) { Workaround = note.Workaround };
            })
            .ToArray();

    /// <summary>
    /// Every issue with the pages of every kind that note it, by name. A registry notes entry for a key that no page
    /// notes fails the catalog, so a sentence cannot go unpublished. <paramref name="versions"/> maps the key of an
    /// entity that merged into a version to that version, so its entry finds the page that carries it.
    /// </summary>
    public static IReadOnlyList<PublishedIssue> Publish(
        KnownIssueRegistry registry,
        IEnumerable<(EntityReference Reference, IReadOnlyList<IssueNote> Notes)> pages,
        IReadOnlyDictionary<string, EntityReference>? versions = null
    )
    {
        var noted = pages.ToArray();
        foreach (var issue in registry.Issues)
        foreach (var key in issue.Notes?.Keys ?? [])
        {
            var pageKey = versions?.GetValueOrDefault(key)?.Key ?? key;
            if (
                !noted.Any(page =>
                    page.Reference.Key == pageKey && page.Notes.Any(note => note.Issue == issue.Id)
                )
            )
                throw new InvalidDataException(
                    $"Known issue {issue.Id} has a notes entry for {key}, which does not note this issue."
                );
        }
        return registry
            .Issues.Select(issue => new PublishedIssue(
                issue,
                noted
                    .Where(page => page.Notes.Any(note => note.Issue == issue.Id))
                    .Select(page => page.Reference)
                    .Distinct()
                    // Versions of one item share its page, so the list names the page once. When more than one
                    // version is affected, it links the page rather than one version of it.
                    .GroupBy(reference => (reference.Kind, reference.Slug ?? reference.Key))
                    .Select(versions =>
                        versions.Count() == 1
                            ? versions.First()
                            : versions.First() with
                            {
                                Fragment = null,
                            }
                    )
                    .OrderBy(reference => reference.Name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(reference => reference.Key, StringComparer.Ordinal)
                    .ToArray()
            ))
            .ToArray();
    }

    /// <summary>
    /// The link targets that issue text uses, each resolved to its reference. Every sentence a player or a
    /// maintainer reads is checked, and the notes that pages show come from the registry, so this covers them too.
    /// </summary>
    public static IReadOnlyDictionary<string, EntityReference> Links(
        KnownIssueRegistry registry,
        IReadOnlyDictionary<string, EntityReference?> targets
    )
    {
        var used = new SortedDictionary<string, EntityReference>(StringComparer.Ordinal);
        foreach (var issue in registry.Issues)
        {
            // A title is a heading and a table of contents entry, which hold no links.
            if (LinkedText.Link().IsMatch(issue.Title))
                throw new InvalidDataException($"Known issue {issue.Id} links from its title.");
            foreach (var text in Texts(issue))
                LinkedText.Resolve("Known issue " + issue.Id, text, targets, used);
        }
        return used;
    }

    /// <summary>Every text of an issue that a reader sees apart from its title, where links may stand.</summary>
    public static IEnumerable<string> Texts(KnownIssue issue)
    {
        if (issue.Effect is not null)
            yield return issue.Effect;
        foreach (var detail in issue.EffectDetails ?? [])
        {
            yield return detail.Label;
            yield return detail.Text;
        }
        if (issue.Workaround is not null)
            yield return issue.Workaround;
        foreach (var step in issue.Steps)
        {
            yield return step.Action;
            yield return step.Result;
        }
        yield return issue.Expected;
        yield return issue.Observed;
        yield return issue.Cause;
        if (issue.Notes is not null)
        {
            foreach (var note in issue.Notes.Values)
            {
                yield return note.Effect;
                if (note.Workaround is not null)
                    yield return note.Workaround;
            }
        }
    }

    private static string WeaponDamageNote(ItemPage item)
    {
        string Damage(Stat stat) =>
            StatVocabulary.Display(item.Facts.First(fact => fact.Stat == stat))!;
        return item.Type == "Shield" ? $"{item.Name} deals no damage."
            : item.Facts.Any(fact => fact.Stat == Stat.BodyHitDamage)
                ? $"{item.Name} deals {Damage(Stat.BodyHitDamage)} damage per hit, {Damage(Stat.HeadHitDamage)} on a headshot."
            : $"{item.Name} deals {Damage(Stat.Damage)} damage per hit.";
    }

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex Id();
}
