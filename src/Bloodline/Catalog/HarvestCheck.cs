using System.Text.Json.Nodes;
using Bloodline.Records;

namespace Bloodline.Catalog;

/// <summary>
/// A harvestable that a census found in play, with the preset it applied and the loot it gives from. A restored one is
/// one of the saved harvestables that loading spawns again at its saved spot, whichever level loads.
/// </summary>
internal sealed record CensusHarvestable(
    string Name,
    bool Custom,
    string? Preset,
    IReadOnlyList<HarvestLoot> Loot,
    bool Restored = false
);

/// <summary>The harvestables in play in one level, as the latest run of harvest-census.lua read them.</summary>
internal sealed record HarvestCensus(
    string Level,
    IReadOnlyList<CensusHarvestable> Harvestables,
    string Run
);

/// <summary>
/// Compares each level's harvestables in play, which <c>tools/runtime/harvest-sweep.mjs</c> reads with
/// <c>tools/runtime/checks/harvest-census.lua</c>, with what the catalog says the level gives, placed or spawned, and
/// each one's loot with its preset's measured loot. A game update that changes a spawner or a preset then shows in the
/// maintainer report.
/// </summary>
internal static class HarvestCheck
{
    /// <summary>
    /// The latest census of each level in the build's runs. Run directories start with their start time, so a later
    /// census of a level replaces an earlier one.
    /// </summary>
    public static IReadOnlyList<HarvestCensus> Load(string runtimeDirectory)
    {
        if (!Directory.Exists(runtimeDirectory))
            return [];
        var latest = new Dictionary<string, HarvestCensus>(StringComparer.OrdinalIgnoreCase);
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
                    || entry["command"]?.GetValue<string>() != "eval"
                    || entry["result"] is not JsonObject result
                    || RecordValues.String(result["level"]) is not { } level
                    || !result.ContainsKey("harvestables")
                )
                    continue;
                // The bridge writes an empty Lua table as an object, so a level without harvestables reads as {}.
                var harvestables = (result["harvestables"] as JsonArray ?? [])
                    .OfType<JsonObject>()
                    .Select(harvestable => new CensusHarvestable(
                        RecordValues.String(harvestable["name"]) ?? "",
                        RecordValues.Boolean(harvestable["custom"]) == true,
                        RecordValues.String(harvestable["preset"]),
                        (harvestable["loot"] as JsonArray ?? [])
                            .OfType<JsonObject>()
                            .Select(loot => new HarvestLoot(
                                RecordValues.String(loot["item"]) ?? "",
                                (decimal)(RecordValues.Number(loot["weight"]) ?? 0)
                            ))
                            .ToArray(),
                        RecordValues.Boolean(harvestable["restored"]) == true
                    ))
                    .ToArray();
                latest[level] = new HarvestCensus(level, harvestables, Path.GetFileName(run));
            }
        }
        return [.. latest.Values.OrderBy(census => census.Level, StringComparer.Ordinal)];
    }

    /// <summary>
    /// Lists every preset in play that the catalog does not give in its level, every preset whose loot differs from its
    /// measured loot, and every spawned harvestable without a custom preset, as maintainer report gaps. Restored
    /// harvestables belong to the level where the save was made, so they are left out.
    /// </summary>
    public static void Compare(
        CatalogContext context,
        IReadOnlyList<HarvestCensus> censuses,
        Coverage coverage
    )
    {
        var expected = GatheringMechanism.PresetsByLevel(context);
        var measured = context.Records.HarvestPresets.Presets.ToDictionary(
            preset => preset.Preset,
            StringComparer.Ordinal
        );
        foreach (var census in censuses)
        {
            var level = context.Records.Levels.FirstOrDefault(level =>
                level.Path.EndsWith("/" + census.Level, StringComparison.OrdinalIgnoreCase)
            );
            if (level is null || context.ExcludedLevels.Contains(level.Path))
                continue;
            var place = context.Places.Get(level.Path);
            void Differ(string label, string detail) =>
                coverage.Add(
                    GapKind.HarvestDifference,
                    place.Name + ": " + label,
                    place,
                    $"Run {census.Run}: {detail}"
                );
            var presets = expected.GetValueOrDefault(level.Path) ?? new HashSet<string>();
            foreach (
                var group in census
                    .Harvestables.Where(harvestable =>
                        harvestable.Custom
                        && harvestable.Preset is not null
                        && !harvestable.Restored
                    )
                    .GroupBy(harvestable => harvestable.Preset!, StringComparer.Ordinal)
            )
            {
                if (!presets.Contains(group.Key))
                    Differ(
                        group.Key,
                        $"{group.Count()} in play, which the catalog does not give in this level."
                    );
                if (
                    measured.TryGetValue(group.Key, out var preset)
                    && group.Any(harvestable => !SameLoot(harvestable.Loot, preset.Loot))
                )
                    Differ(group.Key, "loot in play differs from harvest-presets.json.");
            }
            var placed = level.Actors.Select(actor => actor.Name).ToHashSet(StringComparer.Ordinal);
            var unmodeled = census.Harvestables.Count(harvestable =>
                !harvestable.Custom && !placed.Contains(harvestable.Name)
            );
            if (unmodeled > 0)
                Differ(
                    "spawned harvestables without a custom preset",
                    $"{unmodeled} in play, which no modeled spawner places."
                );
        }
    }

    private static bool SameLoot(
        IReadOnlyList<HarvestLoot> live,
        IReadOnlyList<HarvestLoot> measured
    ) =>
        live.Count == measured.Count
        && measured.All(expected =>
            live.Any(actual =>
                actual.Item == expected.Item && Math.Abs(actual.Weight - expected.Weight) < 0.001m
            )
        );
}
