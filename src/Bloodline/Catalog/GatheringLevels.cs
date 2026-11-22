namespace Bloodline.Catalog;

/// <summary>
/// The gathering level a harvestable of one preset requires, from <c>src/Bloodline/Content/gathering-levels.json</c>.
/// The game sets it when it checks the level, by the preset name, so lab runs measured it. <see cref="Checks"/> is
/// the level the game compares: Woodcutting, Mining or Foraging.
/// </summary>
internal sealed record GatheringLevel(string Preset, string Checks, int Level);

internal sealed record GatheringLevels(
    IReadOnlyList<string> Evidence,
    IReadOnlyList<GatheringLevel> Levels
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
