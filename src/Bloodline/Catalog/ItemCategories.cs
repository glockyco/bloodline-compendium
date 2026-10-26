namespace Bloodline.Catalog;

/// <summary>
/// The categories the site browses by. They follow the game's item types, except where a type tells a
/// reader nothing: the game files smithing components and raw materials alike under "Etc.", and shop
/// currencies under "None".
/// </summary>
internal static class ItemCategories
{
    public sealed record Category(string Name, string Plural);

    private static readonly Dictionary<string, Category> ByGameType = new(StringComparer.Ordinal)
    {
        ["Weapon"] = new("Weapon", "Weapons"),
        ["Armor"] = new("Armor", "Armor"),
        ["Food"] = new("Food", "Food"),
        ["Potion"] = new("Potion", "Potions"),
        ["Quest"] = new("Quest Item", "Quest Items"),
        ["Note"] = new("Note", "Notes"),
        ["Trade Goods"] = new("Trade Goods", "Trade Goods"),
        ["Gods Gift"] = new("Gods Gift", "Gods Gifts"),
        ["Etc."] = new("Material", "Materials"),
    };

    public static readonly Category WeaponPart = new("Weapon Part", "Weapon Parts");

    public static readonly Category Currency = new("Currency", "Currencies");

    /// <summary>The category of an item, or null when the game files do not name its type.</summary>
    public static Category? For(string? gameType, bool weaponPart, bool currency) =>
        currency ? Currency
        : weaponPart ? WeaponPart
        : gameType is null ? null
        : ByGameType.GetValueOrDefault(gameType) ?? new Category(gameType, gameType);

    /// <summary>The plural of a category name, for headings and lists.</summary>
    public static string Plural(string name) =>
        ByGameType
            .Values.Append(WeaponPart)
            .Append(Currency)
            .FirstOrDefault(category => category.Name == name)
            ?.Plural
        ?? name;
}
