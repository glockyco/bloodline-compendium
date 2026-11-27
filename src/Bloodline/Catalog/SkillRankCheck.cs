using System.Text.Json.Nodes;

namespace Bloodline.Catalog;

/// <summary>
/// The damage of each hit that a skill landed on a still enemy in the lab's latest skill-ranks run, once with its
/// discipline's damage passives at rank 0 and once with all of them at their highest rank.
/// <c>tools/runtime/checks/skill-ranks.sh</c> makes these runs.
/// </summary>
internal sealed record SkillRankCast(string Run, IReadOnlyList<double> Zero, IReadOnlyList<double> Full)
{
    /// <summary>Whether a hit at full ranks is a hit at rank 0 raised by <paramref name="bonus"/>.</summary>
    public bool Rose(double bonus) =>
        Zero.Any(zero => Full.Any(full => Math.Abs(full - zero * (1 + bonus)) <= zero * 0.005));

    /// <summary>Whether both casts hit and every hit at full ranks also landed at rank 0.</summary>
    public bool Unchanged =>
        Zero.Count > 0
        && Full.Count > 0
        && Full.All(full => Zero.Any(zero => Math.Abs(full - zero) <= 0.005));
}

internal static class SkillRankCheck
{
    /// <summary>Each skill's pair of casts in the latest run. Run directories start with their start time.</summary>
    public static IReadOnlyDictionary<string, SkillRankCast> Load(string runtimeDirectory)
    {
        var casts = new Dictionary<string, SkillRankCast>(StringComparer.Ordinal);
        if (!Directory.Exists(runtimeDirectory))
            return casts;
        var run = Directory
            .EnumerateDirectories(runtimeDirectory, "*-skill-ranks")
            .Order(StringComparer.Ordinal)
            .LastOrDefault(directory => File.Exists(Path.Combine(directory, "run.jsonl")));
        if (run is null)
            return casts;
        var zero = new Dictionary<string, IReadOnlyList<double>>(StringComparer.Ordinal);
        foreach (
            var line in File.ReadLines(Path.Combine(run, "run.jsonl")).Where(line => line.Length > 0)
        )
        {
            var entry = JsonNode.Parse(line);
            if (
                entry?["ok"]?.GetValue<bool>() != true
                || entry["args"] is not JsonObject args
                || (args["skill"] as JsonValue)?.GetValue<string>() is not { } skill
                || args["ranks"] is not JsonObject ranks
                || entry["result"] is not JsonObject result
                || result["castAt"] is null
            )
                continue;
            var hits = Hits(result);
            if (ranks.All(rank => rank.Value!.GetValue<double>() == 0))
                zero[skill] = hits;
            else if (zero.TryGetValue(skill, out var before))
                casts[skill] = new SkillRankCast(Path.GetFileName(run), before, hits);
        }
        return casts;
    }

    /// <summary>
    /// The damage of each hit the enemy received, which tells apart hits that land in one frame. A hit that reaches
    /// the enemy another way shows only as lost Health, so a cast without received damage gives its Health losses.
    /// A cast without hits writes an empty table, which is no array.
    /// </summary>
    private static IReadOnlyList<double> Hits(JsonObject result)
    {
        double[] Values(string list, string field) =>
            (result[list] as JsonArray ?? [])
                .Select(hit => hit![field]!.GetValue<double>())
                .Where(damage => damage >= 1)
                .ToArray();
        return Values("receives", "damage") is { Length: > 0 } received
            ? received
            : Values("hits", "loss");
    }
}
