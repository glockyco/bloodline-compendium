using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

/// <summary>
/// The text that the game filled into one item's inventory tooltip in a lab run. <see cref="Widget"/> is null when
/// hovering the item built no tooltip widget.
/// </summary>
internal sealed record TooltipObservation(
    string Item,
    string Run,
    string? Widget,
    IReadOnlyDictionary<string, string> Text,
    IReadOnlyList<string> Hidden
);

/// <summary>
/// A known cause of tooltip differences and the owner that has to act on it. <see cref="Issue"/> marks a game
/// defect, whose ID names its entry in <c>issues/known-issues.json</c>.
/// </summary>
internal sealed record TooltipReason(string Id, string Owner, bool Issue, string Explanation);

/// <summary>
/// One field where an item's tooltip shows another value than the catalog. <see cref="Reason"/> is null when no
/// cause is known. <see cref="Citations"/> are the functions that the catalog's value follows.
/// </summary>
internal sealed record TooltipDifference(
    string Item,
    string Name,
    string Field,
    string? Tooltip,
    string? Catalog,
    string? Reason,
    IReadOnlyList<CodeCitation> Citations
);

internal sealed record TooltipUnread(string Item, string Name, string Reason);

/// <summary>
/// Every published item's facts compared with the tooltip the game shows for it. Written as
/// <c>tooltip-check.json</c> beside the claims.
/// </summary>
internal sealed record TooltipReport(
    long Build,
    string Consumer,
    IReadOnlyList<string> Runs,
    int Compared,
    IReadOnlyList<TooltipReason> Reasons,
    IReadOnlyList<TooltipUnread> Unread,
    IReadOnlyList<TooltipDifference> Differences
);

internal static partial class TooltipCheck
{
    /// <summary>The handler that fills an item's tooltip in the character sheet's inventory.</summary>
    public const string Consumer =
        "/Game/BloodlinesAlpha/UI/Inventory/UI_InventoryItem_2:OnMouseEnter";

    private const string Widget = "W_Tooltip_QualityWhite_C";

    public static readonly IReadOnlyList<TooltipReason> Reasons =
    [
        new(
            "armor-without-row",
            "game",
            true,
            "Equipping finds no ArmorDataTable row named after the item, so it adds no armor, while the tooltip shows the item's ItemInfo.Damage or the row whose armor name is the item's name."
        ),
        new(
            "armor-slot-adds-none",
            "game",
            true,
            "Amulets and support items add no armor, while their tooltips show the ArmorDataTable row whose armor name is the item's name."
        ),
        new(
            "crafted-armor-tooltip",
            "game",
            false,
            "Crafted armor tooltips show the armor that Translate Custom Armor Description writes in place of the description, and no armor class."
        ),
        new(
            "food-tooltip-heals",
            "game",
            true,
            "Food tooltips show PotionInfo's restoration amount, while eating applies the food's FoodEffectDataTable row."
        ),
        new(
            "food-without-row",
            "game",
            true,
            "Eating finds no FoodEffectDataTable row for the food, so it has no effect, while its tooltip shows healing."
        ),
        new("empty-stat-line", "game", true, "The tooltip shows a stat line without a label."),
    ];

    /// <summary>
    /// The latest tooltip of each item that a tooltip batch read in the build's lab runs. Run directories start
    /// with their start time, so a later run's reading replaces an earlier one.
    /// </summary>
    public static IReadOnlyList<TooltipObservation> Load(string runtimeDirectory)
    {
        var latest = new Dictionary<string, TooltipObservation>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(runtimeDirectory))
        {
            return [];
        }
        foreach (
            var run in Directory
                .EnumerateDirectories(runtimeDirectory)
                .Order(StringComparer.Ordinal)
        )
        {
            var log = Path.Combine(run, "run.jsonl");
            if (!File.Exists(log))
            {
                continue;
            }
            foreach (var line in File.ReadLines(log).Where(line => line.Length > 0))
            {
                var entry = JsonNode.Parse(line);
                if (
                    entry?["command"]?.GetValue<string>() != "tooltip-batch"
                    || entry["ok"]?.GetValue<bool>() != true
                )
                {
                    continue;
                }
                // The lab's bridge writes an empty list as an empty object.
                foreach (var row in entry["result"]?["tooltips"] as JsonArray ?? [])
                {
                    if (row?["item"]?.GetValue<string>() is not { } item)
                    {
                        continue;
                    }
                    latest[item] = new TooltipObservation(
                        item,
                        Path.GetFileName(run),
                        row["widget"]?.GetValue<string>(),
                        (row["text"] as JsonObject ?? []).ToDictionary(
                            pair => pair.Key,
                            pair => pair.Value?.GetValue<string>() ?? "",
                            StringComparer.Ordinal
                        ),
                        (row["hidden"] as JsonArray ?? [])
                            .Select(name => name!.GetValue<string>())
                            .ToArray()
                    );
                }
            }
        }
        return latest.Values.OrderBy(tooltip => tooltip.Item, StringComparer.Ordinal).ToArray();
    }

    public static TooltipReport Check(
        long build,
        IReadOnlyList<ItemPage> items,
        IReadOnlyList<TooltipObservation> tooltips,
        IReadOnlyList<FactClaim> claims
    )
    {
        var read = tooltips.ToDictionary(tooltip => tooltip.Item, StringComparer.OrdinalIgnoreCase);
        var citations = claims
            .GroupBy(claim => (claim.Item, claim.Fact))
            .ToDictionary(group => group.Key, group => group.First().Citations);
        var unread = new List<TooltipUnread>();
        var differences = new List<TooltipDifference>();
        var runs = new SortedSet<string>(StringComparer.Ordinal);
        var compared = 0;
        foreach (var item in items.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            if (!read.TryGetValue(item.Key, out var tooltip))
            {
                unread.Add(new(item.Key, item.Name, "No lab run has read its tooltip."));
                continue;
            }
            runs.Add(tooltip.Run);
            if (tooltip.Widget != Widget)
            {
                unread.Add(
                    new(
                        item.Key,
                        item.Name,
                        tooltip.Widget is null
                            ? "Hovering it builds no tooltip widget."
                            : "Its tooltip widget is " + tooltip.Widget + "."
                    )
                );
                continue;
            }
            compared++;
            Compare(
                item,
                tooltip,
                (field, shown, published, reason) =>
                    differences.Add(
                        new(
                            item.Key,
                            item.Name,
                            field,
                            shown,
                            published,
                            reason,
                            citations.GetValueOrDefault((item.Key, field)) ?? []
                        )
                    )
            );
        }
        return new TooltipReport(
            build,
            Consumer,
            runs.ToArray(),
            compared,
            Reasons,
            unread,
            differences
        );
    }

    /// <summary>
    /// A coverage entry's wording for a difference without a known cause. Names and descriptions are game prose,
    /// which the site's own sentences do not quote, so only the kind of difference is named. The report keeps them.
    /// </summary>
    public static string Describe(TooltipDifference difference) =>
        difference.Field is "Name" or "Description"
            ? "The game's tooltip shows "
                + (string.IsNullOrWhiteSpace(difference.Tooltip) ? "no " : "another ")
                + difference.Field.ToLowerInvariant()
                + "."
            : difference.Field
                + ": the game's tooltip shows "
                + Shown(difference.Tooltip)
                + ", and this reference shows "
                + Shown(difference.Catalog)
                + ".";

    private static string Shown(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "nothing" : value.Trim();

    private static void Compare(
        ItemPage item,
        TooltipObservation tooltip,
        Action<string, string?, string?, string?> add
    )
    {
        string? Shown(string block) =>
            tooltip.Hidden.Contains(block) ? null : tooltip.Text.GetValueOrDefault(block);
        Fact? Find(Stat stat) => item.Facts.FirstOrDefault(fact => fact.Stat == stat);
        var armorType = Find(Stat.ArmorType);
        var crafted = armorType?.Text == "Custom";
        // Retrieve Player Armor never adds the slots of amulets and support items, so they have no armor type.
        var slotWithoutArmor = item.Category.Name == "Armor" && armorType is null;
        var armorReason =
            crafted ? "crafted-armor-tooltip"
            : armorType?.Text == "Unarmored" ? "armor-without-row"
            : slotWithoutArmor ? "armor-slot-adds-none"
            : null;
        var foodReason =
            Find(Stat.Effects)?.Text == "None" ? "food-without-row" : "food-tooltip-heals";

        void Number(string field, string shown, Stat stat, string? reason, double? fallback = null)
        {
            var fact = Find(stat);
            var published = fact?.Number ?? fallback;
            var number = double.TryParse(
                shown.Replace(",", "", StringComparison.Ordinal),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var parsed
            )
                ? parsed
                : (double?)null;
            if (number is null || number != published)
            {
                add(
                    field,
                    shown,
                    fact is null
                        ? published?.ToString("G", CultureInfo.InvariantCulture)
                        : StatVocabulary.Display(fact),
                    reason
                );
            }
        }

        if (Shown("TextBlock_0") is { } name && name != item.Name)
        {
            add("Name", name, item.Name, null);
        }
        if (Shown("TextBlock_2") is { } description && description != (item.Description ?? ""))
        {
            // Crafted armor shows its armor here, which must still agree with the catalog's rating.
            if (crafted && CraftedArmor().Match(description) is { Success: true } armor)
            {
                Number("Armor rating", armor.Groups["value"].Value, Stat.ArmorRating, null);
                add("Description", description, item.Description, armorReason);
            }
            else
            {
                add("Description", description, item.Description, null);
            }
        }
        if (Shown("TextBlock_3") is { } line && StatLine().Match(line) is { Success: true } stat)
        {
            var value = stat.Groups["value"].Value;
            switch (stat.Groups["label"].Value)
            {
                case "":
                    add("Stat line", line, null, "empty-stat-line");
                    break;
                case "Damage":
                    Number("Damage", value, Stat.Damage, null);
                    break;
                case "ARMOR":
                    Number("Armor rating", value, Stat.ArmorRating, armorReason);
                    break;
                case "Heals" when value.EndsWith("hp", StringComparison.Ordinal):
                    // A food that heals nothing publishes no amount, so the tooltip's amount differs from zero.
                    Number(
                        "Restoration amount",
                        value[..^2],
                        Stat.RestorationAmount,
                        foodReason,
                        0
                    );
                    break;
                default:
                    add("Stat line", line, null, null);
                    break;
            }
        }
        if (Shown("SpeedTypeBlock") is { } speedOrType)
        {
            if (item.Category.Name == "Weapon")
            {
                var speed = Find(Stat.Speed)?.Text;
                if (speedOrType != speed)
                {
                    add("Speed", speedOrType, speed, null);
                }
            }
            else if (item.Category.Name == "Armor")
            {
                var shownType = speedOrType.StartsWith("Type: ", StringComparison.Ordinal)
                    ? speedOrType["Type: ".Length..]
                    : speedOrType;
                if (shownType != (armorType?.Text ?? ""))
                {
                    add("Armor type", speedOrType, armorType?.Text, armorReason);
                }
            }
            else
            {
                add("Speed or type line", speedOrType, null, null);
            }
        }
    }

    [GeneratedRegex(@"^(?<label>[^:]*): ?(?<value>.*)$", RegexOptions.Singleline)]
    private static partial Regex StatLine();

    [GeneratedRegex(@"^Armor: (?<value>-?[0-9,]+)\s*$")]
    private static partial Regex CraftedArmor();
}
