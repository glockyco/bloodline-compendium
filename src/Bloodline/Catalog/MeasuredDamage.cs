namespace Bloodline.Catalog;

/// <summary>
/// The damage a lab run measured for a ranged weapon whose firing code the catalog cannot follow, such as a bow
/// whose arrow spawns an explosion, from <c>src/Bloodline/Content/measured-damage.json</c>. <see cref="Projectile"/>
/// names what it fires, or is null for a weapon that fires nothing. <see cref="FullyDrawn"/> is a bow's damage when
/// fully drawn, where it differs. <see cref="Evidence"/> names the runs.
/// </summary>
internal sealed record MeasuredDamage(
    string Item,
    string? Projectile,
    double Body,
    double Head,
    double? FullyDrawn,
    IReadOnlyList<string> Evidence
);
