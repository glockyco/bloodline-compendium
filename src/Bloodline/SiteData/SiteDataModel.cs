using System.Text.Json.Serialization;

namespace Bloodline.SiteData;

// Site data is the contract with the site. JSON Schema is exported from these types, and the site
// generates its TypeScript types from that schema, so change these types instead of the generated files.

internal enum EntityKind
{
    Item,
    Merchant,
    Person,
    Creature,
    Place,
    Container,
    Recipe,
    Building,
    CalendarEvent,
    Quest,
    Resource,
    Activity,
}

internal sealed record EntityKindDefinition(
    EntityKind Kind,
    bool HasPages,
    string? Route,
    string SearchFilter,
    string StructuredDataType
);

internal sealed record EntityReference(
    EntityKind Kind,
    string? Key,
    string Name,
    string? Slug,
    string? Icon
);

internal enum ConditionKind
{
    CalendarEvent,
    Quest,
    QuestStage,
    GuildRank,
    DisciplineLevel,
    ProficiencyLevel,
    Building,
    GameState,
    Unknown,
}

internal sealed record RelationCondition(
    ConditionKind Kind,
    EntityReference? Entity,
    double? Value,
    string Label
);

internal enum RelationDirection
{
    Outgoing,
    Incoming,
}

internal sealed record EntityRelation(
    string Id,
    string Kind,
    RelationDirection Direction,
    EntityReference Entity,
    string Label,
    IReadOnlyList<RelationCondition> Conditions
);

internal sealed record RelationRow(
    string Id,
    string Kind,
    EntityReference Source,
    EntityReference Target,
    string ForwardLabel,
    string ReverseLabel,
    IReadOnlyList<RelationCondition> Conditions,
    Source? Attributes
);

internal sealed record EntityDocument(
    EntityReference Entity,
    IReadOnlyList<EntityRelation> Relations
);

internal sealed record EntityGraph(
    IReadOnlyList<EntityDocument> Entities,
    IReadOnlyList<RelationRow> Relations
);

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
    public string Key { get; init; } = "";

    [JsonRequired]
    public EntityKind Kind { get; init; } = EntityKind.Item;

    [JsonRequired]
    public string Type { get; init; } = "Unknown";
    public string? Qualifier { get; init; }
}

/// <summary>
/// Everything an item page shows. Written as <c>items/&lt;slug&gt;.json</c>. <see cref="Key"/> is the item's
/// Blueprint class path. A null <see cref="Icon"/> means that no icon was found or decoded.
/// </summary>
internal sealed record ItemPage(
    string Key,
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
    public EntityKind Kind { get; init; } = EntityKind.Item;

    [JsonRequired]
    public IReadOnlyList<EntityRelation> Relations { get; init; } = [];

    [JsonRequired]
    public string Type { get; init; } = "Unknown";
    public string? Qualifier { get; init; }

    [JsonRequired]
    public IReadOnlyList<ItemUse> Uses { get; init; } = [];
}

internal sealed record ItemUse(SourceKind Kind, string Name, EntityReference Item, int Quantity)
{
    [JsonRequired]
    public string RelationId { get; init; } = "";

    [JsonRequired]
    public IReadOnlyList<RelationCondition> Conditions { get; init; } = [];
}

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

/// <summary>
/// An effect with its strength and its duration in seconds. <see cref="Power"/> is null when the game gives the
/// effect no strength, and <see cref="Seconds"/> is zero when it gives no duration.
/// </summary>
internal sealed record Effect(string Name, double? Power, double Seconds);

internal enum Stat
{
    Value,
    Category,
    DamageContribution,
    SpeedContribution,
    WeaponType,
    Damage,
    AttackSpeed,
    Speed,
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
    IReadOnlyList<EntityReference> Places,
    Price? Price,
    Amount? Amount,
    Weight? Weight,
    double? Chance,
    IReadOnlyList<ItemAmount> Inputs,
    IReadOnlyList<string> Notes
)
{
    [JsonRequired]
    public EntityReference Entity { get; init; } = new(EntityKind.Activity, null, "", null, null);

    [JsonRequired]
    public IReadOnlyList<RelationCondition> Conditions { get; init; } = [];

    [JsonRequired]
    public string RelationId { get; init; } = "";
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

/// <summary>A price in gold, the game's money, or in a currency item when <see cref="Currency"/> is set.</summary>
internal sealed record Price(int Amount, EntityReference? Currency);

/// <summary>How many of the item a source gives at once.</summary>
internal sealed record Amount(int Min, int Max);

/// <summary>An item's raw weight and the total weight of the pool it is drawn from.</summary>
internal sealed record Weight(double Value, double Total);

internal sealed record ItemAmount(EntityReference Item, int Quantity);

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
    UnknownCondition,
    UnresolvedReference,
}

/// <summary>
/// One entry of a gap. <see cref="Entity"/> names the affected entity when there is one.
/// <see cref="Source"/> names the kind of source a source gap is about, so pages can word it.
/// </summary>
internal sealed record GapEntry(string Label, EntityReference? Entity, string? Detail)
{
    public SourceKind? Source { get; init; }
}

/// <summary>The files of the site data, gathered only to export one schema for all of them.</summary>
internal sealed record SiteDataFiles(
    SiteBuild Build,
    ItemIndex Items,
    ItemPage Item,
    CoverageReport Coverage,
    IReadOnlyList<EntityKindDefinition> Kinds,
    EntityGraph Graph
);
