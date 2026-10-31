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

/// <summary>
/// The categories, the list's filters and columns, and a summary of every item, in name order. Written as
/// <c>items.json</c>.
/// </summary>
internal sealed record ItemIndex(
    IReadOnlyList<CategorySummary> Categories,
    IReadOnlyList<ItemSummary> Items
)
{
    [JsonRequired]
    public IReadOnlyList<ListFacet> Facets { get; init; } = [];

    [JsonRequired]
    public IReadOnlyList<ListColumn> Columns { get; init; } = [];
}

/// <summary>
/// A filter of the item list. <see cref="Categories"/> names the category slugs it applies to, all when empty.
/// <see cref="Values"/> gives the value order, alphabetical when empty.
/// </summary>
internal sealed record ListFacet(
    string Id,
    string Label,
    IReadOnlyList<string> Categories,
    IReadOnlyList<string> Values
)
{
    /// <summary>Reader labels for values that are keys, such as category slugs. Other values read as they are.</summary>
    [JsonRequired]
    public IReadOnlyDictionary<string, string> Labels { get; init; } =
        new Dictionary<string, string>();
}

/// <summary>
/// A column of the item list. <see cref="Categories"/> names the category slugs that show it by default, none when
/// empty. A <see cref="Numeric"/> column can also become a stat filter.
/// </summary>
internal sealed record ListColumn(
    string Id,
    string Label,
    IReadOnlyList<string> Categories,
    bool Numeric,
    string? Unit
);

/// <summary>A cell of the item list, with the number it sorts and filters by when it has one.</summary>
internal sealed record ListCell(double? Number, string? Text);

/// <summary>A category with its plural name for headings, such as Weapon and Weapons.</summary>
internal sealed record CategorySummary(string Slug, string Name, string Plural, int Count);

/// <summary>What the item list shows of an item. <see cref="Icon"/> is the hash of an icon in <c>icons/</c>.</summary>
internal sealed record ItemSummary(
    string Slug,
    string Name,
    string Category,
    int? Value,
    string? Icon
)
{
    [JsonRequired]
    public string Key { get; init; } = "";

    [JsonRequired]
    public EntityKind Kind { get; init; } = EntityKind.Item;

    [JsonRequired]
    public string Type { get; init; } = "Unknown";
    public string? Qualifier { get; init; }

    [JsonRequired]
    public ItemStatus Status { get; init; } = ItemStatus.Obtainable;

    /// <summary>The item's values for each facet of <see cref="ItemIndex.Facets"/>.</summary>
    [JsonRequired]
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Facets { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>The item's cells for each column of <see cref="ItemIndex.Columns"/> that it has a value for.</summary>
    [JsonRequired]
    public IReadOnlyDictionary<string, ListCell> Cells { get; init; } =
        new Dictionary<string, ListCell>();

    /// <summary>
    /// The ways to get the item at each value of its place facet, in that facet's order, as source facet values, so
    /// that a place filter can keep to the selected ways to get it.
    /// </summary>
    [JsonRequired]
    public IReadOnlyList<IReadOnlyList<string>> PlaceSources { get; init; } = [];
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

    /// <summary>Known issues that affect a value this page shows.</summary>
    [JsonRequired]
    public IReadOnlyList<IssueNote> Notes { get; init; } = [];

    [JsonRequired]
    public ItemStatus Status { get; init; } = ItemStatus.Obtainable;
}

/// <summary>A page's note about a known issue, with the issue's title and its effect on players.</summary>
internal sealed record IssueNote(string Issue, string Title, string Effect);

internal enum ReportStatus
{
    NotReported,
    Reported,
}

/// <summary>
/// A reproduced defect in the game. <see cref="Effect"/> and <see cref="Workaround"/> are for players, and
/// <see cref="Steps"/> reproduce the defect without mods. <see cref="Observed"/> states what the running game showed.
/// <see cref="Cause"/> is derived from the game's code and data, which <see cref="Citations"/> name as
/// package:function. <see cref="Builds"/> are the Steam builds it was confirmed on.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record KnownIssue(
    string Id,
    string Title,
    string Effect,
    string? Workaround,
    IReadOnlyList<string> Steps,
    string Expected,
    string Observed,
    string Cause,
    IReadOnlyList<string> Citations,
    IReadOnlyList<long> Builds,
    ReportStatus Report,
    string? ReportUrl
);

/// <summary>The registry of reproduced game defects, <c>issues/known-issues.json</c>.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record KnownIssueRegistry(IReadOnlyList<KnownIssue> Issues)
{
    [JsonPropertyName("$schema")]
    public string? Schema { get; init; }
}

/// <summary>A known issue with the pages that note it. Written as <c>issues.json</c>.</summary>
internal sealed record PublishedIssue(KnownIssue Issue, IReadOnlyList<EntityReference> Affected);

internal sealed record ItemUse(SourceKind Kind, string Name, EntityReference Item, int Quantity)
{
    [JsonRequired]
    public string RelationId { get; init; } = "";

    [JsonRequired]
    public IReadOnlyList<RelationCondition> Conditions { get; init; } = [];

    /// <summary>The discipline that consumes the item, such as Weaponsmithing, for filtering long use lists.</summary>
    public string? Discipline { get; init; }

    /// <summary>The category of what the use makes, such as Weapon Part.</summary>
    public string? OutputCategory { get; init; }

    /// <summary>The type of what the use makes, such as Blade.</summary>
    public string? OutputType { get; init; }
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

    /// <summary>The value the game's tooltip shows where it differs from the applied value for a known reason.</summary>
    public string? GameShows { get; init; }
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
    Damage,
    AttackSpeed,
    Speed,
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
/// enemy. An empty <see cref="Places"/> means that no place is known. <see cref="Inputs"/> lists what the source
/// consumes, such as ingredients or a planted seed.
/// </summary>
internal sealed record Source(
    SourceKind Kind,
    string Name,
    IReadOnlyList<EntityReference> Places,
    Price? Price,
    Amount? Amount,
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

    /// <summary>The files name this source, but do not establish that it can give the item in play.</summary>
    [JsonRequired]
    public bool Unconfirmed { get; init; }

    /// <summary>The chance per event, for sources that choose at random.</summary>
    public Odds? Odds { get; init; }

    /// <summary>
    /// The lowest and highest chance, as fractions from zero to one, that a loot container is present.
    /// Set only for container loot when at least one container can be absent. Null means always present.
    /// </summary>
    public ChanceRange? Spawn { get; init; }

    /// <summary>What a crafted, cooked or smithed source needs and gives.</summary>
    public Recipe? Recipe { get; init; }

    /// <summary>The source is a building in the player's own settlement.</summary>
    public bool Settlement { get; init; }

    /// <summary>The lines of a conversation that give the item, in the game's own words.</summary>
    public IReadOnlyList<Quote> Quotes { get; init; } = [];
}

/// <summary>A line of game dialogue. <see cref="Player"/> marks a line the player chooses.</summary>
internal sealed record Quote(string Text, bool Player);

internal enum OddsEvent
{
    Container,
    /// <summary>All loose loot generated by one placement of a ground-loot source.</summary>
    LootSpot,
    Kill,
    Delivery,
    Use,
    Catch,

    /// <summary>One pick of a source whose number of picks per action depends on the player's skill.</summary>
    Pick,
}

/// <summary>
/// The chance of at least one copy per <see cref="Event"/>, and how many times the item is expected per event.
/// Both are null when the selection rule is not read, and <see cref="Unknown"/> then says why in player terms.
/// </summary>
internal sealed record Odds(OddsEvent Event, double? Chance, double? Expected, string? Unknown)
{
    /// <summary>
    /// The highest chance of at least one copy per event, as a fraction from zero to one.
    /// When set, <see cref="Chance"/> is the lowest chance. Null means there is no chance range.
    /// </summary>
    public double? HighestChance { get; init; }
}

/// <summary>The lowest and highest probabilities, as fractions from zero to one.</summary>
internal sealed record ChanceRange(double Min, double Max);

/// <summary>
/// A recipe's context. <see cref="Station"/> names where it is made, <see cref="Level"/> the discipline level it
/// needs, <see cref="Output"/> how many it makes, and <see cref="Learned"/> how the recipe becomes available.
/// </summary>
internal sealed record Recipe(
    EntityReference? Station,
    string? Discipline,
    int? Level,
    double? Experience,
    int Output,
    string? Learned
);

internal enum ItemStatus
{
    Obtainable,
    NoKnownSource,
    NotInNormalPlay,
}

internal enum PriceRule
{
    /// <summary>The amount is what the player pays.</summary>
    Fixed,

    /// <summary>
    /// The amount is the base value, and the trading window multiplies it by the player's respect in
    /// <see cref="Price.City"/>, or by the neutral multiplier where the merchant stands in no city with respect.
    /// </summary>
    Trading,
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
    Stable,
    Contract,
    SeasonalGift,
    Archaeology,
    Reward,
}

/// <summary>A price in gold, the game's money, or in a currency item when <see cref="Currency"/> is set.</summary>
internal sealed record Price(int Amount, EntityReference? Currency)
{
    public PriceRule Rule { get; init; } = PriceRule.Fixed;

    /// <summary>The city whose respect sets a trading price, null where respect does not apply.</summary>
    public EntityReference? City { get; init; }

    /// <summary>The graph's relation for paying with <see cref="Currency"/>.</summary>
    public string? RelationId { get; init; }
}

/// <summary>How many of the item a source gives at once.</summary>
internal sealed record Amount(int Min, int Max);

/// <summary>An item and how many a source needs. <see cref="RelationId"/> names the graph's relation for it.</summary>
internal sealed record ItemAmount(EntityReference Item, int Quantity)
{
    public string? RelationId { get; init; }
}

/// <summary>
/// The crafting rules that tools and part pages use. Written as <c>crafting.json</c>.
/// </summary>
internal sealed record CraftingData(
    IReadOnlyList<WeaponFamily> Weapons,
    IReadOnlyList<WeaponPart> Parts,
    IReadOnlyList<QualityGrade> Qualities,
    IReadOnlyList<ArmorQuality> ArmorQualities
);

/// <summary>A crafted weapon family, such as Greatsword, and the part kinds it is assembled from, in order.</summary>
internal sealed record WeaponFamily(string Id, string Name, IReadOnlyList<string> PartKinds);

/// <summary>
/// A weapon part with the part kind it fills, the weapon families it fits and what it adds to the finished weapon.
/// </summary>
internal sealed record WeaponPart(
    EntityReference Item,
    string Kind,
    IReadOnlyList<string> Families,
    double? Damage,
    double? Speed,
    double? Value
);

/// <summary>A crafting quality grade, such as S, and the factor it applies to the finished weapon's stats.</summary>
internal sealed record QualityGrade(string Grade, double Multiplier);

/// <summary>
/// An armorsmithing quality, such as Masterwork, the armorsmithing level it needs and the armor it gives for each
/// armor class, such as Heavy.
/// </summary>
internal sealed record ArmorQuality(
    string Name,
    int Level,
    IReadOnlyDictionary<string, double> ArmorByClass
);

/// <summary>The files of the site data, gathered only to export one schema for all of them.</summary>
internal sealed record SiteDataFiles(
    SiteBuild Build,
    ItemIndex Items,
    ItemPage Item,
    IReadOnlyList<EntityKindDefinition> Kinds,
    EntityGraph Graph,
    IReadOnlyList<PublishedIssue> Issues,
    CraftingData Crafting
);
