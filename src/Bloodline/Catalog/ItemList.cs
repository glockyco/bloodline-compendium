using System.Text.Json;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

/// <summary>
/// The item list's filters and columns, declared here so that a new category or game update changes the list without
/// a site change, and each item's values for them.
/// </summary>
internal static class ItemList
{
    private sealed record FacetRule(
        string Id,
        string Label,
        IReadOnlyList<string> Categories,
        Func<ItemPage, IEnumerable<string>> Values
    );

    private sealed record ColumnRule(
        string Id,
        string Label,
        IReadOnlyList<string> Categories,
        bool Numeric,
        string? Unit,
        Func<ItemPage, ListCell?> Cell
    );

    private static readonly IReadOnlyDictionary<ItemStatus, string> Availability = new Dictionary<
        ItemStatus,
        string
    >
    {
        [ItemStatus.Obtainable] = "A way to get it is known",
        [ItemStatus.NoKnownSource] = "No known way to get it",
        [ItemStatus.NotInNormalPlay] = "Not in normal play",
    };

    private static readonly string[] ArmorClasses = ["Light", "Medium", "Heavy", "Unarmored"];

    private static readonly FacetRule[] Facets =
    [
        new("category", "Category", [], item => [item.Category.Slug]),
        new("weaponType", "Weapon Type", ["weapon"], item => Text(item, Stat.WeaponType)),
        new("armorSlot", "Armor Slot", ["armor"], item => [item.Type]),
        new("armorClass", "Armor Class", ["armor"], item => Text(item, Stat.ArmorType)),
        new("weaponPart", "Weapon Part", ["weapon-part"], item => [item.Type]),
        new(
            "effect",
            "Effect",
            ["food", "potion"],
            item =>
                item.Facts.Where(fact => fact.Stat == Stat.Effect && fact.Effect is not null)
                    .Select(fact => fact.Effect!.Name)
        ),
        new(
            "source",
            "How to Get",
            [],
            item => item.Sources.Select(source => Camel(source.Kind.ToString()))
        ),
        new(
            "place",
            "Where to Get",
            [],
            item => item.Sources.SelectMany(source => source.Places).Select(place => place.Name)
        ),
        new("availability", "Availability", [], item => [Camel(item.Status.ToString())]),
    ];

    private static readonly ColumnRule[] Columns =
    [
        new("damage", "Damage", ["weapon"], true, null, item => Number(item, Stat.Damage)),
        new("speed", "Speed", ["weapon"], false, null, item => Word(item, Stat.Speed)),
        new("attackSpeed", "Attack Speed", [], true, null, item => Number(item, Stat.AttackSpeed)),
        new("range", "Range", [], true, null, item => Number(item, Stat.Range)),
        new(
            "magazineSize",
            "Magazine Size",
            [],
            true,
            null,
            item => Number(item, Stat.MagazineSize)
        ),
        new("armor", "Armor", ["armor"], true, null, item => Number(item, Stat.ArmorRating)),
        new("armorClass", "Class", ["armor"], false, null, item => Word(item, Stat.ArmorType)),
        new(
            "heals",
            "Heals",
            ["food", "potion"],
            true,
            null,
            item => Number(item, Stat.RestorationAmount)
        ),
        new(
            "effects",
            "Effects",
            ["food", "potion"],
            false,
            null,
            item =>
                item
                    .Facts.Where(fact => fact.Stat == Stat.Effect && fact.Effect is not null)
                    .ToArray()
                    is { Length: > 0 } effects
                    ? new ListCell(
                        null,
                        string.Join(", ", effects.Select(fact => fact.Effect!.Name).Distinct())
                    )
                    : null
        ),
        new(
            "damageContribution",
            "Damage",
            ["weapon-part"],
            true,
            null,
            item => Number(item, Stat.DamageContribution)
        ),
        new(
            "speedContribution",
            "Speed",
            ["weapon-part"],
            true,
            null,
            item => Number(item, Stat.SpeedContribution)
        ),
        new(
            "uses",
            "Uses",
            ["material"],
            true,
            null,
            item => item.Uses.Count > 0 ? new ListCell(item.Uses.Count, null) : null
        ),
    ];

    /// <summary>The list's filters with the values the items have, in reading order.</summary>
    public static IReadOnlyList<ListFacet> FacetDefinitions(
        IReadOnlyList<ItemPage> items,
        IReadOnlyList<CategorySummary> categories
    ) =>
        Facets
            .Select(rule =>
            {
                var values = items
                    .SelectMany(item => ValuesOf(rule, item))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
                IEnumerable<string> ordered = rule.Id switch
                {
                    "category" => categories
                        .Select(category => category.Slug)
                        .Where(values.Contains),
                    "armorClass" => ArmorClasses
                        .Where(values.Contains)
                        .Concat(values.Except(ArmorClasses).Order(StringComparer.Ordinal)),
                    "source" => Enum.GetNames<SourceKind>().Select(Camel).Where(values.Contains),
                    "availability" => Enum.GetNames<ItemStatus>()
                        .Select(Camel)
                        .Where(values.Contains),
                    _ => values.Order(StringComparer.OrdinalIgnoreCase),
                };
                var labels = rule.Id switch
                {
                    "category" => categories.ToDictionary(
                        category => category.Slug,
                        category => category.Plural
                    ),
                    "availability" => Availability.ToDictionary(
                        pair => Camel(pair.Key.ToString()),
                        pair => pair.Value
                    ),
                    _ => new Dictionary<string, string>(),
                };
                return new ListFacet(rule.Id, rule.Label, rule.Categories, ordered.ToArray())
                {
                    Labels = labels,
                };
            })
            .Where(facet => facet.Values.Count > 0)
            .ToArray();

    public static IReadOnlyList<ListColumn> ColumnDefinitions(IReadOnlyList<ItemPage> items) =>
        Columns
            .Where(rule => items.Any(item => rule.Cell(item) is not null))
            .Select(rule => new ListColumn(
                rule.Id,
                rule.Label,
                rule.Categories,
                rule.Numeric,
                rule.Unit
            ))
            .ToArray();

    // A facet that belongs to some categories holds values only for their items.
    private static IEnumerable<string> ValuesOf(FacetRule rule, ItemPage item) =>
        rule.Categories.Count == 0 || rule.Categories.Contains(item.Category.Slug)
            ? rule.Values(item)
            : [];

    public static IReadOnlyDictionary<string, IReadOnlyList<string>> FacetsOf(ItemPage item) =>
        Facets
            .Select(rule =>
                (rule.Id, Values: ValuesOf(rule, item).Distinct(StringComparer.Ordinal).ToArray())
            )
            .Where(entry => entry.Values.Length > 0)
            .ToDictionary(entry => entry.Id, entry => (IReadOnlyList<string>)entry.Values);

    public static IReadOnlyDictionary<string, ListCell> CellsOf(ItemPage item) =>
        Columns
            .Select(rule => (rule.Id, Cell: rule.Cell(item)))
            .Where(entry => entry.Cell is not null)
            .ToDictionary(entry => entry.Id, entry => entry.Cell!);

    // Site data writes enums in camel case, and facet values match them.
    private static string Camel(string name) => JsonNamingPolicy.CamelCase.ConvertName(name);

    private static IEnumerable<string> Text(ItemPage item, Stat stat) =>
        item
            .Facts.Where(fact => fact.Stat == stat && fact.Text is not null)
            .Select(fact => fact.Text!);

    private static ListCell? Number(ItemPage item, Stat stat) =>
        item.Facts.FirstOrDefault(fact => fact.Stat == stat && fact.Number is not null) is { } fact
            ? new ListCell(fact.Number, null)
            : null;

    private static ListCell? Word(ItemPage item, Stat stat) =>
        item.Facts.FirstOrDefault(fact => fact.Stat == stat && fact.Text is not null) is { } fact
            ? new ListCell(null, fact.Text)
            : null;
}
