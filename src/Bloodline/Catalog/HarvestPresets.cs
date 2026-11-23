namespace Bloodline.Catalog;

/// <summary>
/// What a harvestable of one preset gives and the gathering level it requires, from
/// <c>src/Bloodline/Content/harvest-presets.json</c>. The game sets both from the preset's name in code the catalog does
/// not read, so lab runs measured them. <see cref="Checks"/> is the level the game compares: Woodcutting, Mining or
/// Foraging. Each gather gives one item of <see cref="Loot"/>, picked by its weight over the table's sum.
/// </summary>
internal sealed record HarvestPreset(
    string Preset,
    string Checks,
    int Level,
    IReadOnlyList<HarvestLoot> Loot
);

internal sealed record HarvestLoot(string Item, decimal Weight);

internal sealed record HarvestPresets(
    IReadOnlyList<string> Evidence,
    IReadOnlyList<HarvestPreset> Presets
)
{
    /// <summary>The lists of the harvestable class whose names Check Player Level gives a level, by the level they check.</summary>
    public static readonly IReadOnlyDictionary<string, string> Lists = new Dictionary<
        string,
        string
    >
    {
        ["Woodcutting Items"] = "Woodcutting",
        ["Mining Items"] = "Mining",
        ["Foraging EXP Items"] = "Foraging",
    };
}
