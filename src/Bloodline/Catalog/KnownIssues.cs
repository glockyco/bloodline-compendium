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
    /// <summary>Knee armor counts only after another armor piece is equipped.</summary>
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
    public static (IReadOnlyList<ItemPage> Items, IReadOnlyList<PublishedIssue> Issues) Attach(
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
        }
        foreach (var issue in registry.Issues)
        {
            foreach (var key in issue.ItemNotes?.Keys ?? [])
            {
                if (!notes.TryGetValue(key, out var itemNotes) || !itemNotes.Contains(issue.Id))
                {
                    throw new InvalidDataException(
                        $"Known issue {issue.Id} has an itemNotes entry for {key}, which does not note this issue."
                    );
                }
            }
        }
        // An item's Bugs rows follow the order of the Known Issues page, which is the registry's.
        var order = registry
            .Issues.Select((issue, index) => (issue.Id, index))
            .ToDictionary(entry => entry.Id, entry => entry.index, StringComparer.Ordinal);
        var pages = items
            .Select(item =>
                item with
                {
                    Notes = notes[item.Key]
                        .Select(id => Find(id, "The page of " + item.Name))
                        .OrderBy(issue => order[issue.Id])
                        .Select(issue => new IssueNote(
                            issue.Id,
                            issue.Id switch
                            {
                                "armor-without-row" or "armor-slot-adds-none" =>
                                    $"Wearing {item.Name} adds no armor.",
                                NoProficiency => $"{item.Name} trains no weapon proficiency.",
                                WeaponTooltipDamage => WeaponDamageNote(item),
                                _ => issue.ItemNotes?.GetValueOrDefault(item.Key) ?? issue.Effect,
                            }
                        )
                        {
                            TooltipValues = tooltipValues.GetValueOrDefault((item.Key, issue.Id)),
                        })
                        .ToArray(),
                }
            )
            .ToArray();
        var published = registry
            .Issues.Select(issue => new PublishedIssue(
                issue,
                pages
                    .Where(page => page.Notes.Any(note => note.Issue == issue.Id))
                    .OrderBy(page => page.Name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(page => page.Key, StringComparer.Ordinal)
                    .Select(CatalogGraph.Reference)
                    .ToArray()
            ))
            .ToArray();
        return (pages, published);
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
