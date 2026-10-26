using System.Text.Json.Serialization;

namespace Bloodline.SiteData;

// Site data is the contract with the site. JSON Schema is exported from these types, and the site
// generates its TypeScript types from that schema, so change these types instead of the generated files.

/// <summary>The game build and code commit that one site's data comes from. Written as <c>build.json</c>.</summary>
internal sealed record SiteBuild(
    int AppId,
    long BuildId,
    IReadOnlyList<SiteDepot> Depots,
    string Commit,
    bool Dirty,
    SiteRelease Release
);

/// <summary>An installed depot. Manifest IDs exceed what JavaScript numbers hold exactly, so they stay strings.</summary>
internal sealed record SiteDepot(long DepotId, string ManifestId);

internal sealed record SiteRelease(
    string Title,
    string PatchDate,
    string PatchNotesUrl,
    string DataDate
);

/// <summary>The categories and a summary of every item, in name order. Written as <c>items.json</c>.</summary>
internal sealed record ItemIndex(
    IReadOnlyList<CategorySummary> Categories,
    IReadOnlyList<ItemSummary> Items
);

/// <summary>A category with its plural name for headings, such as Weapon and Weapons.</summary>
internal sealed record CategorySummary(string Slug, string Name, string Plural, int Count);

/// <summary>What the item list shows of an item. <see cref="Icon"/> is the hash of an icon in <c>icons/</c>.</summary>
internal sealed record ItemSummary(
    string Slug,
    string Name,
    string Category,
    int? Value,
    string? Icon,
    IReadOnlyList<SourceKind> SourceKinds
)
{
    [JsonRequired]
    public string Type { get; init; } = "Unknown";
    public string? Qualifier { get; init; }
}

/// <summary>
/// Everything an item page shows. Written as <c>items/&lt;slug&gt;.json</c>. <see cref="Id"/> is the item's
/// Blueprint class path. A null <see cref="Icon"/> means that no icon was found or decoded.
/// </summary>
internal sealed record ItemPage(
    string Id,
    string Slug,
    string Name,
    string? Description,
    CategoryLink Category,
    string? Icon,
    IReadOnlyList<Fact> Facts,
    IReadOnlyList<Source> Sources
)
{
    [JsonRequired]
    public string Type { get; init; } = "Unknown";
    public string? Qualifier { get; init; }

    [JsonRequired]
    public IReadOnlyList<ItemUse> Uses { get; init; } = [];
}

internal sealed record ItemUse(SourceKind Kind, string Name, ItemLink Item, int Quantity);

internal sealed record CategoryLink(string Slug, string Name);

/// <summary>
/// A fact about an item, such as its value or damage. <see cref="Stat"/> names it, and the label, tooltip
/// label and unit come from <see cref="StatVocabulary"/>, so every page uses the same terms. At most one of
/// <see cref="Number"/>, <see cref="Text"/> and <see cref="Effect"/> holds the value. All three are null when
/// the game files do not determine it.
/// </summary>
internal sealed record Fact(Stat Stat, string Label, double? Number, string? Text, Effect? Effect)
{
    public string? TooltipLabel { get; init; }
    public string? Unit { get; init; }
}

/// <summary>An effect with its strength and duration in seconds. A zero duration means it does not expire.</summary>
internal sealed record Effect(string Name, double Power, double Seconds);

internal enum Stat
{
    Value,
    Category,
    DamageContribution,
    SpeedContribution,
    WeaponType,
    Damage,
    AttackSpeed,
    Range,
    DamageType,
    MagazineSize,
    ArmorType,
    ArmorRating,
    Effects,
    Effect,
    RestorationAmount,
    HealthDamage,
    ManaDamage,
    ChoppedEffect,
    CooledEffect,
    GroundEffect,
    HeatedEffect,
}

/// <summary>
/// One way to get an item. <see cref="Name"/> names what gives it, such as a vendor, recipe, container or
/// enemy. An empty <see cref="Places"/> means that no place is known. <see cref="Weight"/> is the raw weight
/// in its pool, and <see cref="Chance"/> is a probability from 0 to 1, given only where the selection rule
/// is known. <see cref="Inputs"/> lists what the source consumes, such as ingredients or a planted seed.
/// </summary>
internal sealed record Source(
    SourceKind Kind,
    string Name,
    IReadOnlyList<Place> Places,
    Price? Price,
    Amount? Amount,
    Weight? Weight,
    double? Chance,
    IReadOnlyList<ItemAmount> Inputs,
    IReadOnlyList<string> Notes
)
{
    public string? ChanceBasis { get; init; }
    public Amount? Picks { get; init; }
}

internal enum SourceKind
{
    Shop,
    CurrencyShop,
    Smithing,
    Crafting,
    Cooking,
    ContainerLoot,
    RandomLoot,
    EnemyDrop,
    Pickup,
    Dialogue,
    Fishing,
    Gathering,
    Farming,
    BountyNote,
    SettlementDelivery,
}

/// <summary>A level, identified by its package path, with the name players know it by.</summary>
internal sealed record Place(string Id, string Name);

/// <summary>A price in gold, the game's money, or in a currency item when <see cref="Currency"/> is set.</summary>
internal sealed record Price(int Amount, ItemLink? Currency);

/// <summary>How many of the item a source gives at once.</summary>
internal sealed record Amount(int Min, int Max);

/// <summary>An item's raw weight and the total weight of the pool it is drawn from.</summary>
internal sealed record Weight(double Value, double Total);

internal sealed record ItemAmount(ItemLink Item, int Quantity);

/// <summary>A link to an item page. <see cref="Slug"/> is null when the item has no page.</summary>
internal sealed record ItemLink(string? Slug, string Name);

/// <summary>Every known gap in the site's data. Written as <c>coverage.json</c>.</summary>
internal sealed record CoverageReport(IReadOnlyList<CoverageGap> Gaps);

internal sealed record CoverageGap(GapKind Kind, IReadOnlyList<GapEntry> Entries);

internal enum GapKind
{
    ItemWithoutSource,
    SourceWithoutPlace,
    ItemWithoutIcon,
    UnknownFact,
    PlaceWithoutName,
    UnreadObject,
}

/// <summary>
/// One entry of a gap. <see cref="ItemSlug"/> links to the affected item when there is one.
/// <see cref="Source"/> names the kind of source a source gap is about, so pages can word it.
/// </summary>
internal sealed record GapEntry(string Label, string? ItemSlug, string? Detail)
{
    public SourceKind? Source { get; init; }
}

/// <summary>The files of the site data, gathered only to export one schema for all of them.</summary>
internal sealed record SiteDataFiles(
    SiteBuild Build,
    ItemIndex Items,
    ItemPage Item,
    CoverageReport Coverage
);
