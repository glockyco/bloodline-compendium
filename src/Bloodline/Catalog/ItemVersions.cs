using System.Globalization;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

/// <summary>How items of one name become versions of one item page.</summary>
internal static class ItemVersions
{
    public static readonly VersionPolicy<ItemPage> Policy = new(
        EntityKind.Item,
        Shown: item => new
        {
            item.Type,
            item.Category,
            item.Icon,
            item.Description,
            item.Facts,
            item.OnHitEffects,
            item.ProjectileGroups,
        },
        Merge: copies =>
            copies.Count == 1
                ? copies[0]
                : copies[0] with
                {
                    Sources = EntityVersions.Union(copies.SelectMany(item => item.Sources)),
                    Uses = EntityVersions.Union(copies.SelectMany(item => item.Uses)),
                    Notes = EntityVersions.Union(copies.SelectMany(item => item.Notes)),
                    Status = EntityVersions.MergedStatus(copies.Select(item => item.Status)),
                },
        AsVersion: (item, id, label, name) =>
            item with
            {
                VersionId = id,
                VersionLabel = label,
                VersionName = name,
            },
        WithVersions: (item, others) => item with { Versions = others },
        KindLabel: item => item.Type,
        LastLabel: item =>
            item.Facts.FirstOrDefault(fact => fact.Stat == Stat.Value)?.Number is { } value
                ? "Sells for "
                    + NeutralSalePrice(value).ToString("N0", CultureInfo.InvariantCulture)
                    + " gold"
                : null
    );

    public static Versioned<ItemPage> Group(IReadOnlyList<ItemPage> items) =>
        EntityVersions.Group(items, Policy);

    /// <summary>Item pages lead with a weapon, then armor, then the list's category order, with notes last.</summary>
    public static IEnumerable<ItemPage> Pages(
        IEnumerable<ItemPage> items,
        IReadOnlyList<CategorySummary> categories
    )
    {
        var categoryOrder = categories
            .Select((category, index) => (category.Slug, index))
            .ToDictionary(entry => entry.Slug, entry => entry.index, StringComparer.Ordinal);
        return EntityVersions.Pages(
            items,
            Policy,
            item =>
                item.Category.Slug switch
                {
                    "weapon" => -2,
                    "armor" => -1,
                    "note" => int.MaxValue,
                    _ => categoryOrder[item.Category.Slug],
                }
        );
    }

    /// <summary>
    /// What a merchant pays at neutral respect, as Window_Trading_Shop:GetSellValueWithMultiplier computes it in
    /// 32-bit floats with the multiplier 1.4: the base value times 1.5 minus the multiplier, truncated, and at least 1.
    /// The site's sell-price rule computes the same amount for Details.
    /// </summary>
    private static int NeutralSalePrice(double value) =>
        Math.Max((int)MathF.Truncate((float)value * (1.5f - 1.4f)), 1);
}
