using System.Globalization;
using System.Text.Json.Nodes;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

/// <summary>The texts a skill's in-game tooltip showed in one lab run.</summary>
internal sealed record SkillTooltip(string Skill, string Run, IReadOnlyList<string> Texts);

/// <summary>
/// Compares each skill's facts with the tooltip the game built for it, which
/// <c>tools/runtime/checks/skill-tooltips.lua</c> reads for every tree skill.
/// </summary>
internal static class SkillTooltipCheck
{
    /// <summary>
    /// The latest tooltip of each skill in the build's lab runs. Run directories start with their start time, so a
    /// later run's reading replaces an earlier one.
    /// </summary>
    public static IReadOnlyList<SkillTooltip> Load(string runtimeDirectory)
    {
        var latest = new Dictionary<string, SkillTooltip>(StringComparer.Ordinal);
        if (!Directory.Exists(runtimeDirectory))
            return [];
        foreach (
            var run in Directory
                .EnumerateDirectories(runtimeDirectory)
                .Order(StringComparer.Ordinal)
        )
        {
            var log = Path.Combine(run, "run.jsonl");
            if (!File.Exists(log))
                continue;
            foreach (var line in File.ReadLines(log).Where(line => line.Length > 0))
            {
                var entry = JsonNode.Parse(line);
                if (
                    entry?["ok"]?.GetValue<bool>() != true
                    || entry["result"] is not JsonObject result
                    || (result["check"] as JsonValue)?.GetValue<string>() != "skill-tooltips"
                    || result["skills"] is not JsonObject skills
                )
                    continue;
                foreach (var (skill, blocks) in skills)
                {
                    latest[skill] = new SkillTooltip(
                        skill,
                        Path.GetFileName(run),
                        (blocks as JsonArray ?? [])
                            .Where(block => block?["visible"]?.GetValue<bool>() == true)
                            .Select(block => block!["text"]!.GetValue<string>())
                            .Where(text => text.Length > 0)
                            .ToArray()
                    );
                }
            }
        }
        return latest.Values.OrderBy(tooltip => tooltip.Skill, StringComparer.Ordinal).ToArray();
    }

    /// <summary>The texts the game's skill tooltip shows for a skill's facts, as the tooltip words them.</summary>
    public static IReadOnlyList<string> Expected(SkillPage skill)
    {
        var texts = new List<string> { skill.Name };
        if (skill.Description is { Length: > 0 } description)
            texts.Add(description);
        foreach (var fact in skill.Facts.Where(fact => fact.Number is not null))
        {
            var number = fact.Number!.Value.ToString("G", CultureInfo.InvariantCulture);
            texts.Add(
                fact.Stat == Stat.Cooldown
                    ? $"{fact.TooltipLabel}: {number} {fact.Unit}"
                    : $"{fact.TooltipLabel}: {number}"
            );
        }
        return texts;
    }

    /// <summary>Lists every skill whose tooltip shows other texts than its facts, as maintainer report gaps.</summary>
    public static void Compare(
        IReadOnlyList<SkillPage> skills,
        IReadOnlyList<SkillTooltip> tooltips,
        Coverage coverage
    )
    {
        var pages = skills.ToDictionary(skill => skill.Key, StringComparer.Ordinal);
        foreach (var tooltip in tooltips)
        {
            if (!pages.TryGetValue(tooltip.Skill, out var skill))
                continue;
            var expected = Expected(skill);
            var missing = expected.Except(tooltip.Texts, StringComparer.Ordinal).ToArray();
            var extra = tooltip.Texts.Except(expected, StringComparer.Ordinal).ToArray();
            if (missing.Length == 0 && extra.Length == 0)
                continue;
            coverage.Add(
                GapKind.TooltipDifference,
                skill.Name,
                new EntityReference(
                    EntityKind.Skill,
                    skill.Key,
                    skill.Name,
                    skill.Slug,
                    skill.Icon
                ),
                $"Run {tooltip.Run}: facts give \"{string.Join("\", \"", missing)}\", the tooltip shows \"{string.Join("\", \"", extra)}\"."
            );
        }
    }
}
