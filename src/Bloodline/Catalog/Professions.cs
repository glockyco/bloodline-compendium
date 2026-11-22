using Bloodline.SiteData;

namespace Bloodline.Catalog;

/// <summary>
/// The leveled crafts and trades the character sheet lists under Passive Levels, which the site calls professions.
/// Recipes, crops and fishing spots name them in their own spellings, such as "Focus crafting", so every producer
/// resolves a name here and gets one reference per profession.
/// </summary>
internal static class Professions
{
    private static readonly string[] Names =
    [
        "Weaponsmithing",
        "Focus Crafting",
        "Smelting",
        "Engineering",
        "Cooking",
        "Farming",
        "Fishing",
    ];

    private static readonly Dictionary<string, EntityReference> ByName = Names.ToDictionary(
        name => name,
        name => new EntityReference(EntityKind.Profession, "Profession:" + name, name, null, null),
        StringComparer.OrdinalIgnoreCase
    );

    /// <summary>Every profession's reference, in the order above.</summary>
    public static IEnumerable<EntityReference> All => Names.Select(name => ByName[name]);

    /// <summary>
    /// The profession a producer names. An unknown name fails the catalog, so a game update that adds a craft is
    /// added here instead of showing as unlinked text.
    /// </summary>
    public static EntityReference Reference(string name) =>
        ByName.TryGetValue(name, out var reference)
            ? reference
            : throw new InvalidOperationException($"{name} is not a known profession.");
}
