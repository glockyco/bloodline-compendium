using System.Text.Json.Nodes;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

/// <summary>A skill slot or passive button as the game built it in a skill window that a lab run opened.</summary>
internal sealed record TreeEntry(
    string Tree,
    string Widget,
    string? Skill,
    string? Name,
    int? RequiredLevel,
    string? LevelToCheck,
    int? PointCost,
    int? CostIncrease,
    int? Ranks,
    string Run
);

/// <summary>
/// Compares the published skill rows and passives with the skill windows the game built in the latest run of
/// <c>tools/runtime/checks/skill-trees.lua</c>, which opens every tree and lists each slot's and passive button's
/// values. A game update that changes a tree then shows in the maintainer report instead of only in the records.
/// </summary>
internal static class TreeCheck
{
    /// <summary>
    /// The slots and passive buttons of the latest skill-trees run of the build. Run directories start with their start
    /// time, so a later run's trees replace an earlier one's.
    /// </summary>
    public static IReadOnlyList<TreeEntry> Load(string runtimeDirectory)
    {
        if (!Directory.Exists(runtimeDirectory))
            return [];
        IReadOnlyList<TreeEntry> latest = [];
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
                    || result["menus"] is null
                    || result["trees"] is not JsonObject trees
                )
                    continue;
                latest = trees
                    .SelectMany(tree => Entries(tree.Key, tree.Value, Path.GetFileName(run)))
                    .ToArray();
            }
        }
        return latest;
    }

    private static IEnumerable<TreeEntry> Entries(string tree, JsonNode? node, string run)
    {
        static int? Integer(JsonNode? value) =>
            value is JsonValue number && number.TryGetValue<double>(out var result)
                ? (int)result
                : null;
        static string? Text(JsonNode? value) =>
            value is JsonValue text && text.TryGetValue<string>(out var result) ? result : null;
        foreach (var slot in node?["slots"] as JsonArray ?? [])
            if (slot?["visible"]?.GetValue<bool>() == true)
                yield return new TreeEntry(
                    tree,
                    Text(slot["widget"])!,
                    Text(slot["Skill"]),
                    null,
                    Integer(slot["Required Level"]),
                    Text(slot["Level To Check"]),
                    null,
                    null,
                    null,
                    run
                );
        foreach (var passive in node?["passives"] as JsonArray ?? [])
            if (passive?["visible"]?.GetValue<bool>() == true)
                yield return new TreeEntry(
                    tree,
                    Text(passive["widget"])!,
                    null,
                    Text(passive["Passive Name"]),
                    Integer(passive["Required Level"]),
                    Text(passive["Level To Check"]),
                    Integer(passive["Point Cost"]),
                    Integer(passive["Point Increase Per Level"]),
                    passive["MultiLevel"]?.GetValue<bool>() == true
                        ? Integer(passive["Max MultiLevel"])
                        : 1,
                    run
                );
    }

    /// <summary>
    /// Lists every slot and passive whose published values differ from the window the run built, and every one that
    /// only one side has, as maintainer report gaps.
    /// </summary>
    public static void Compare(
        IReadOnlyList<DisciplinePage> disciplines,
        IReadOnlyDictionary<string, EntityReference> skills,
        IReadOnlyList<TreeEntry> entries,
        Coverage coverage
    )
    {
        var levelNames = disciplines.ToDictionary(
            page => page.Key,
            page => page.LevelName,
            StringComparer.Ordinal
        );
        foreach (var page in disciplines)
        {
            var built = entries.Where(entry => entry.Tree == page.Key).ToArray();
            if (built.Length == 0)
                continue;
            var reference = SkillProducer.DisciplineReference(page);
            void Differ(string label, string detail) =>
                coverage.Add(
                    GapKind.TreeDifference,
                    page.Name + ": " + label,
                    reference,
                    $"Run {built[0].Run}: {detail}"
                );
            string? LevelName(EntityReference discipline) =>
                levelNames.GetValueOrDefault(discipline.Key ?? "");
            var slots = page
                .SkillRows.SelectMany(row => row.Slots)
                .ToDictionary(slot => slot.Skill.Key ?? "", StringComparer.Ordinal);
            foreach (var slot in built.Where(entry => entry.Skill is not null))
            {
                // Skills of one name that show the same are one page, so a slot names the page its class merged into.
                var key = skills.GetValueOrDefault(slot.Skill!)?.Key ?? slot.Skill!;
                if (!slots.Remove(key, out var published))
                {
                    Differ(
                        slot.Widget,
                        $"the window has {slot.Skill}, which no published row holds."
                    );
                    continue;
                }
                if (published.RequiredLevel != slot.RequiredLevel)
                    Differ(
                        published.Skill.Name,
                        $"required level {published.RequiredLevel} published, {slot.RequiredLevel} in the window."
                    );
                if (LevelName(published.LevelDiscipline) != slot.LevelToCheck)
                    Differ(
                        published.Skill.Name,
                        $"checks {LevelName(published.LevelDiscipline)} published, {slot.LevelToCheck} in the window."
                    );
            }
            foreach (var missing in slots.Values)
                Differ(missing.Skill.Name, "published, but the window has no such slot.");
            var passives = page
                .PassiveRows.SelectMany(row => row.Passives)
                .ToDictionary(passive => passive.Key, StringComparer.Ordinal);
            foreach (var passive in built.Where(entry => entry.Skill is null))
            {
                var key = page.Key + ":" + passive.Widget;
                if (!passives.Remove(key, out var published))
                {
                    Differ(
                        passive.Name ?? passive.Widget,
                        "the window has this passive, which no published row holds."
                    );
                    continue;
                }
                foreach (
                    var (field, ours, theirs) in new (string, int?, int?)[]
                    {
                        ("required level", published.RequiredLevel, passive.RequiredLevel),
                        ("point cost", published.PointCost, passive.PointCost),
                        ("cost increase", published.CostIncrease, passive.CostIncrease),
                        ("ranks", published.Ranks, passive.Ranks),
                    }
                )
                    if (ours != theirs)
                        Differ(
                            published.Name,
                            $"{field} {ours} published, {theirs} in the window."
                        );
                if (LevelName(published.LevelDiscipline) != passive.LevelToCheck)
                    Differ(
                        published.Name,
                        $"checks {LevelName(published.LevelDiscipline)} published, {passive.LevelToCheck} in the window."
                    );
            }
            foreach (var missing in passives.Values)
                Differ(missing.Name, "published, but the window has no such passive.");
        }
    }
}
