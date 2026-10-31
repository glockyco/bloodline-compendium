using System.Text.RegularExpressions;
using Bloodline.Output;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

/// <summary>
/// Reads <c>issues/known-issues.json</c> and notes its issues on the item pages whose shown values they affect.
/// A note or tooltip cause that names an issue the registry lacks fails the catalog.
/// </summary>
internal static partial class KnownIssues
{
    /// <summary>Knee armor counts only after another armor piece is equipped.</summary>
    public const string KneeArmor = "knee-armor-not-counted";

    /// <summary>A food lists an effect that the food manager's switch does not name.</summary>
    public const string IgnoredFoodEffect = "food-effect-not-applied";

    /// <summary>A food that eating ignores entirely, because the food effect table has no row for it.</summary>
    public const string FoodWithoutRow = "food-without-row";

    /// <summary>Fairy Staff does not fire the fireballs its description promises.</summary>
    public const string FairyStaffNoFireballs = "fairy-staff-no-fireballs";

    /// <summary>Reproduced staves can deal default projectile damage at point-blank range.</summary>
    public const string StaffPointBlankDamage = "staff-point-blank-damage";

    // Only staves whose point-blank damage was reproduced receive this note, not every late damage write.
    private static readonly HashSet<string> PointBlankStaves = new(StringComparer.Ordinal)
    {
        "/Game/BloodlinesAlpha/Interactables/Weapons/Staves/AcolyteGeomancerStaff.AcolyteGeomancerStaff_C",
        "/Game/BloodlinesAlpha/Interactables/Weapons/Halberds/WalkingStick.WalkingStick_C",
        "/Game/BloodlinesAlpha/Interactables/Weapons/Staves/MoonstoneStaff.MoonstoneStaff_C",
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
    /// foods that eating ignores, Fairy Staff's missing fireballs and reproduced point-blank staff damage.
    /// </summary>
    public static (IReadOnlyList<ItemPage> Items, IReadOnlyList<PublishedIssue> Issues) Attach(
        IReadOnlyList<ItemPage> items,
        KnownIssueRegistry registry,
        TooltipReport? tooltips,
        IReadOnlySet<string> ignoredFoodEffects
    )
    {
        var issues = registry.Issues.ToDictionary(issue => issue.Id, StringComparer.Ordinal);
        var notes = items.ToDictionary(
            item => item.Key,
            _ => new SortedSet<string>(StringComparer.Ordinal),
            StringComparer.Ordinal
        );
        var causes = TooltipCheck.Reasons.ToDictionary(reason => reason.Id, StringComparer.Ordinal);
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
            // A tooltip line that the page shows no value for, such as one without a label, needs no note.
            if (difference.Catalog is not null)
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
            if (
                item.Key
                == "/Game/BloodlinesAlpha/Interactables/Weapons/Halberds/FairyStaff.FairyStaff_C"
            )
            {
                notes[item.Key].Add(FairyStaffNoFireballs);
            }
            if (PointBlankStaves.Contains(item.Key))
            {
                notes[item.Key].Add(StaffPointBlankDamage);
            }
        }
        var pages = items
            .Select(item =>
                item with
                {
                    Notes = notes[item.Key]
                        .Select(id => Find(id, "The page of " + item.Name))
                        .Select(issue => new IssueNote(issue.Id, issue.Title, issue.Effect))
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

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex Id();
}
