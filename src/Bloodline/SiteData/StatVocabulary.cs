using System.Globalization;

namespace Bloodline.SiteData;

/// <summary>
/// The one place that names item stats for readers. Labels also form qualifiers in item slugs, such as
/// "5 Armor rating", so a changed label changes URLs. Tooltip labels follow the game's own item tooltip.
/// </summary>
internal static class StatVocabulary
{
    private sealed record Term(string Label, string? TooltipLabel = null, string? Unit = null);

    private static readonly Dictionary<Stat, Term> Terms = new()
    {
        [Stat.Value] = new("Value", Unit: "gold"),
        [Stat.Category] = new("Category"),
        [Stat.DamageContribution] = new("Damage contribution"),
        [Stat.SpeedContribution] = new("Speed contribution"),
        [Stat.WeaponType] = new("Weapon type", "Type"),
        [Stat.Damage] = new("Damage", "Damage"),
        [Stat.AttackSpeed] = new("Attack speed", "Speed"),
        [Stat.Range] = new("Range"),
        [Stat.DamageType] = new("Damage type"),
        [Stat.MagazineSize] = new("Magazine size"),
        [Stat.ArmorType] = new("Armor type", "Type"),
        [Stat.ArmorRating] = new("Armor rating", "Armor"),
        [Stat.Effects] = new("Effects"),
        [Stat.Effect] = new("Effect"),
        [Stat.RestorationAmount] = new("Restoration amount", "Heals"),
        [Stat.HealthDamage] = new("Health damage"),
        [Stat.ManaDamage] = new("Mana damage"),
        [Stat.ChoppedEffect] = new("Chopped effect"),
        [Stat.CooledEffect] = new("Cooled effect"),
        [Stat.GroundEffect] = new("Ground effect"),
        [Stat.HeatedEffect] = new("Heated effect"),
    };

    /// <summary>A fact whose value the game files do not determine.</summary>
    public static Fact Unknown(Stat stat, string? label = null) =>
        Make(stat, null, null, null, label);

    public static Fact Number(Stat stat, double? value) => Make(stat, value, null, null);

    public static Fact Text(Stat stat, string? value) => Make(stat, null, value, null);

    public static Fact Effect(Stat stat, Effect? value, string? label = null) =>
        Make(stat, null, null, value, label);

    private static Fact Make(
        Stat stat,
        double? number,
        string? text,
        Effect? effect,
        string? label = null
    )
    {
        var term = Terms[stat];
        return new Fact(stat, label ?? term.Label, number, text, effect)
        {
            TooltipLabel = term.TooltipLabel,
            Unit = term.Unit,
        };
    }

    /// <summary>
    /// The value in invariant form, as item qualifiers and coverage details have always written it, or null
    /// when it is unknown.
    /// </summary>
    public static string? Display(Fact fact) =>
        fact.Number?.ToString("G", CultureInfo.InvariantCulture)
        ?? fact.Text
        ?? (
            fact.Effect is { } effect
                ? effect.Name
                    + " "
                    + effect.Power.ToString("G", CultureInfo.InvariantCulture)
                    + (
                        effect.Seconds == 0
                            ? ""
                            : " for "
                                + effect.Seconds.ToString("G", CultureInfo.InvariantCulture)
                                + " s"
                    )
                : null
        );
}
