using System.Text.Json.Nodes;

namespace Bloodline.Catalog;

/// <summary>
/// What the lab's skill-ranks runs saw each skill's hits take from a still enemy, once with its discipline's damage
/// passives at rank 0 and once with all of them at their highest rank. <c>tools/runtime/checks/skill-ranks.sh</c>
/// makes these runs.
/// </summary>
internal sealed record SkillRankCast(string Run, IReadOnlyList<double> Zero, IReadOnlyList<double> Full)
{
    /// <summary>Whether a hit at full ranks is a hit at rank 0 raised by <paramref name="bonus"/>.</summary>
    public bool Rose(double bonus) =>
        Zero.Any(zero =>
            zero > 0 && Full.Any(full => Math.Abs(full - zero * (1 + bonus)) <= zero * 0.005)
        );

    /// <summary>Whether the casts hit and every hit at full ranks also landed at rank 0.</summary>
    public bool Unchanged =>
        Zero.Count > 0
        && Full.Count > 0
        && Full.All(full => Zero.Any(zero => Math.Abs(full - zero) <= 0.005));
}

internal static class SkillRankCheck
{
    /// <summary>
    /// The latest pair of casts of each skill in which both casts hit, or else its latest pair. A cast can miss the
    /// enemy in one run and hit in the next, so a run without a hit does not replace one with hits. Run directories
    /// start with their start time.
    /// </summary>
    public static IReadOnlyDictionary<string, SkillRankCast> Load(string runtimeDirectory)
    {
        var latest = new Dictionary<string, SkillRankCast>(StringComparer.Ordinal);
        if (!Directory.Exists(runtimeDirectory))
            return latest;
        foreach (
            var run in Directory
                .EnumerateDirectories(runtimeDirectory, "*-skill-ranks")
                .Order(StringComparer.Ordinal)
        )
        {
            var log = Path.Combine(run, "run.jsonl");
            if (!File.Exists(log))
                continue;
            var zero = new Dictionary<string, IReadOnlyList<double>>(StringComparer.Ordinal);
            foreach (var line in File.ReadLines(log).Where(line => line.Length > 0))
            {
                var entry = JsonNode.Parse(line);
                if (
                    entry?["ok"]?.GetValue<bool>() != true
                    || entry["args"] is not JsonObject args
                    || (args["skill"] as JsonValue)?.GetValue<string>() is not { } skill
                    || args["ranks"] is not JsonObject ranks
                    || entry["result"]?["hits"] is not JsonArray hits
                )
                    continue;
                // Losses under one point are status ticks and float noise, not a skill's hit.
                var losses = hits.Select(hit => hit!["loss"]!.GetValue<double>())
                    .Where(loss => loss >= 1)
                    .ToArray();
                if (ranks.All(rank => rank.Value!.GetValue<double>() == 0))
                    zero[skill] = losses;
                else if (
                    zero.TryGetValue(skill, out var before)
                    && (
                        (before.Count > 0 && losses.Length > 0)
                        || !latest.TryGetValue(skill, out var kept)
                        || kept.Zero.Count == 0
                        || kept.Full.Count == 0
                    )
                )
                    latest[skill] = new SkillRankCast(Path.GetFileName(run), before, losses);
            }
        }
        return latest;
    }
}
