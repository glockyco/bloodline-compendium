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
    Discipline,
    Skill,
    Passive,
    Spellsong,
    Profession,
    Mechanic,
    Pickup,
}

/// <summary>
/// A registered kind. A kind with <see cref="Host"/> has no pages of its own: each of its entities is an anchored
/// entry on a page of the host kind, such as a passive on its discipline's page.
/// </summary>
internal sealed record EntityKindDefinition(
    EntityKind Kind,
    bool HasPages,
    string? Route,
    string SearchFilter,
    string StructuredDataType
)
{
    public EntityKind? Host { get; init; }
}

internal sealed record EntityReference(
    EntityKind Kind,
    string? Key,
    string Name,
    string? Slug,
    string? Icon
)
{
    /// <summary>The item version to open within its shared-name page, without a leading hash.</summary>
    public string? Fragment { get; init; }

    /// <summary>
    /// For an entity of a hosted kind, the host page that shows it, such as a passive's discipline. The link leads
    /// to that page at <see cref="Fragment"/>.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EntityReference? Host { get; init; }
}

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
/// empty. A <see cref="Numeric"/> column can also become a stat filter. A numeric column sorts highest first, as
/// more damage reads as better, unless <see cref="Ascending"/> marks a requirement, such as a level, which reads lowest
/// first.
/// </summary>
internal sealed record ListColumn(
    string Id,
    string Label,
    IReadOnlyList<string> Categories,
    bool Numeric,
    string? Unit
)
{
    public bool Ascending { get; init; }
}

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
    public string? VersionId { get; init; }
    public string? VersionLabel { get; init; }

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
) : IVersionedPage
{
    [JsonRequired]
    public EntityKind Kind { get; init; } = EntityKind.Item;

    [JsonRequired]
    public IReadOnlyList<EntityRelation> Relations { get; init; } = [];

    [JsonRequired]
    public string Type { get; init; } = "Unknown";
    public string? VersionId { get; init; }
    public string? VersionLabel { get; init; }
    public string? VersionName { get; init; }

    /// <summary>The other versions on this page, in key order. These entries have no nested versions.</summary>
    [JsonRequired]
    public IReadOnlyList<ItemPage> Versions { get; init; } = [];

    public IReadOnlyList<ProjectileGroup>? ProjectileGroups { get; init; }

    public IReadOnlyList<OnHitEffect>? OnHitEffects { get; init; }

    /// <summary>The console line that gives this item, such as "AddItem Bread", or null when the console cannot.</summary>
    public string? ConsoleCommand { get; init; }

    [JsonRequired]
    public IReadOnlyList<ItemUse> Uses { get; init; } = [];

    /// <summary>Known issues that affect a value this page shows.</summary>
    [JsonRequired]
    public IReadOnlyList<IssueNote> Notes { get; init; } = [];

    [JsonRequired]
    public ItemStatus Status { get; init; } = ItemStatus.Obtainable;

    /// <summary>The item's model renders, one per body the game draws it on, in <c>renders/</c>.</summary>
    [JsonRequired]
    public IReadOnlyList<ItemRender> Renders { get; init; } = [];
}

internal sealed record ProjectileGroup(
    int Count,
    double? BodyHitDamage,
    double? HeadHitDamage,
    string Projectile
)
{
    public string? Condition { get; init; }
}

/// <summary>
/// An effect a weapon's hits can cause. <see cref="Chance"/> is per hit, or null where the game rolls a chance that
/// is not known, such as an Emberspark's burn.
/// </summary>
internal sealed record OnHitEffect(
    string Effect,
    string Trigger,
    double? Chance,
    double? Amount,
    double? TickIntervalSeconds,
    double? DurationSeconds,
    string? Behavior
);

/// <summary>A page's item-specific note about a known issue, falling back to the issue's effect.</summary>
internal sealed record IssueNote(string Issue, string Effect)
{
    public string? Workaround { get; init; }

    /// <summary>Tooltip values with no applied fact, such as the damage a shield never deals.</summary>
    public IReadOnlyDictionary<string, string>? TooltipValues { get; init; }
}

/// <summary>One player action and what to look for immediately afterward.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record IssueStep(string Action, string Result);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record IssueEffectDetail(string Label, string Text);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record IssueNoteText(string Effect, string? Workaround = null);

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
    string? Effect,
    string? Workaround,
    IReadOnlyList<IssueStep> Steps,
    string Expected,
    string Observed,
    string Cause,
    IReadOnlyList<string> Citations,
    IReadOnlyList<long> Builds,
    ReportStatus Report,
    string? ReportUrl
)
{
    public IReadOnlyList<IssueEffectDetail>? EffectDetails { get; init; }

    /// <summary>
    /// Optional notes for the pages that show this issue, keyed by entity key, such as an item's or a
    /// discipline's class path. A discipline notes an issue only through this entry.
    /// </summary>
    public IReadOnlyDictionary<string, IssueNoteText>? Notes { get; init; }

    /// <summary>
    /// True when the defect concerns something no page describes, such as a weapon assembled from parts, so the
    /// issue affects no page by design and not for lack of a lab run.
    /// </summary>
    public bool? WithoutPages { get; init; }
}

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

    /// <summary>The profession that consumes the item, such as Weaponsmithing, for filtering long use lists.</summary>
    public EntityReference? Profession { get; init; }

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

    /// <summary>The known issue that explains this tooltip difference, when one has been reproduced.</summary>
    public string? GameShowsIssue { get; init; }

    /// <summary>Why the value is unknown, for a fact without a number, text or effect.</summary>
    public string? Unknown { get; init; }
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
    ProjectileDamage,
    BodyHitDamage,
    HeadHitDamage,
    ProjectilesPerShot,
    ChargedBodyHitDamage,
    BlockStaminaCost,
    ExtraFullBlocks,
    ShieldChargeDamage,
    Speed,
    DamageType,
    MagazineSize,
    WeaponBehavior,
    ArmorType,
    ArmorRating,
    Effects,
    Effect,
    RestorationAmount,
    ChoppedEffect,
    CooledEffect,
    GroundEffect,
    HeatedEffect,
    BaseDamage,
    Cooldown,
    ManaCost,
    StaminaCost,
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

    /// <summary>
    /// The events that bring the source into the world for a while, besides its <see cref="Places"/>, such as a Bear
    /// in the world map event Bear.
    /// </summary>
    public IReadOnlyList<SourceEvent> Events { get; init; } = [];

    /// <summary>The progression points a source costs, such as the skill point that unlocks a tree skill.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PointCost? Cost { get; init; }
}

internal enum SourceEventKind
{
    WorldMap,
    Calendar,
}

/// <summary>
/// An event of the world map or the calendar, with the entry of the mechanic page that describes it as
/// <see cref="Page"/>.
/// </summary>
internal sealed record SourceEvent(SourceEventKind Kind, string Name, EntityReference Page);

/// <summary>An amount of progression points, such as 1 skill point.</summary>
internal sealed record PointCost(int Amount, PointKind Points);

internal enum PointKind
{
    SkillPoint,
    ProficiencyPoint,
    LevelUpPoint,
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
/// A recipe's context. <see cref="Station"/> names where it is made, <see cref="Level"/> the level of
/// <see cref="Profession"/> it needs, <see cref="Output"/> how many it makes, and <see cref="Learned"/> how the recipe becomes available.
/// </summary>
internal sealed record Recipe(
    EntityReference? Station,
    EntityReference? Profession,
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

/// <summary>
/// A model render of an item on one body: <c>renders/&lt;Hash&gt;.webp</c>, with its size in pixels, so a page can
/// reserve its space before it loads.
/// </summary>
internal sealed record ItemRender(RenderBody Body, string Hash, int Width, int Height);

/// <summary>The body a render shows an item on. Only armor that dresses the body differs between them.</summary>
internal enum RenderBody
{
    Male,
    Female,
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

    /// <summary>A slot in a discipline's skill tree.</summary>
    SkillTree,

    /// <summary>A skill book placed in a level, which teaches its skill when read.</summary>
    SkillBook,

    /// <summary>A note sheet placed in a level, which teaches its spellsong.</summary>
    NoteSheet,

    /// <summary>An event in the game's code, such as becoming a vampire.</summary>
    Event,
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

/// <summary>
/// A hand-written explanation of how a part of the game works, from <c>src/Bloodline/Content/mechanics.json</c>. Its text
/// may link entities as text written for the site does. <see cref="Evidence"/> names the runs and code each fact rests on.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record MechanicContent(
    string Name,
    string Intro,
    IReadOnlyList<MechanicSection> Sections,
    IReadOnlyList<string> Evidence
);

/// <summary>A titled part of a mechanic page. The catalog sets <see cref="Anchor"/>, its fragment, from the heading.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record MechanicSection(string Heading, IReadOnlyList<string> Paragraphs)
{
    public string? Anchor { get; init; }

    /// <summary>Numbers a player compares, shown after the paragraphs. Cells may link as paragraphs do.</summary>
    public MechanicTable? Table { get; init; }

    /// <summary>
    /// In the written content, the name of the event data to show here, such as the world map chances by terrain.
    /// The catalog replaces it with the section's text or <see cref="Table"/>.
    /// </summary>
    public string? Data { get; init; }
}

/// <summary>A small table of a mechanic section, with a heading per column and the cells of each row.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record MechanicTable(
    IReadOnlyList<string> Headings,
    IReadOnlyList<IReadOnlyList<string>> Rows
)
{
    /// <summary>A fragment for each row that other pages link to, or null when no row is a link target.</summary>
    public IReadOnlyList<string>? Anchors { get; init; }

    /// <summary>Short guidance shown beside the row's name, indexed by row.</summary>
    public IReadOnlyDictionary<int, string>? RowNotes { get; init; }

    /// <summary>Chance cells by column, in row order. The matching text cells are empty.</summary>
    public IReadOnlyDictionary<
        int,
        IReadOnlyList<IReadOnlyList<MechanicChance>>
    >? ChanceColumns { get; init; }
}

/// <summary>A fraction with an optional label, such as the terrain where the chance applies.</summary>
internal sealed record MechanicChance(
    double Value,
    string? Label = null,
    string? LabelAnchor = null
);

/// <summary>A mechanic page. Written as <c>mechanics/&lt;slug&gt;.json</c>.</summary>
internal sealed record MechanicPage(
    string Key,
    string Slug,
    string Name,
    string Intro,
    IReadOnlyList<MechanicSection> Sections,
    IReadOnlyList<string> Evidence
)
{
    [JsonRequired]
    public EntityKind Kind { get; init; } = EntityKind.Mechanic;
}

/// <summary>The mechanic pages in their file's order. Written as <c>mechanics.json</c>.</summary>
internal sealed record MechanicIndex(IReadOnlyList<MechanicSummary> Mechanics);

internal sealed record MechanicSummary(string Key, string Slug, string Name, string Intro);

/// <summary>The professions in the order <c>Professions</c> lists them. Written as <c>professions.json</c>.</summary>
internal sealed record ProfessionIndex(IReadOnlyList<ProfessionSummary> Professions);

/// <summary>
/// A profession as the profession list shows it: where its work is done, the lowest and highest level its items need
/// when the game sets levels, how many items it gives and the icon of one.
/// </summary>
internal sealed record ProfessionSummary(string Key, string Slug, string Name, int Count)
{
    public string? Icon { get; init; }

    [JsonRequired]
    public IReadOnlyList<EntityReference> Stations { get; init; } = [];

    public int? MinLevel { get; init; }
    public int? MaxLevel { get; init; }

    /// <summary>A few things the profession gives, from its lowest level to its highest, to show what it makes.</summary>
    [JsonRequired]
    public IReadOnlyList<EntityReference> Examples { get; init; } = [];
}

/// <summary>
/// Everything a profession page shows: where its work is done and what it makes, gathers or catches, by the level
/// it needs. Written as <c>professions/&lt;slug&gt;.json</c>.
/// </summary>
internal sealed record ProfessionPage(
    string Key,
    string Slug,
    string Name,
    IReadOnlyList<EntityReference> Stations,
    IReadOnlyList<ProfessionRow> Rows
)
{
    [JsonRequired]
    public EntityKind Kind { get; init; } = EntityKind.Profession;
}

/// <summary>
/// One thing a profession gives: the item, the way it is made, gathered or caught, the profession level it needs when
/// the game sets one, the profession experience it gives when the game says, what it uses up, such as a recipe's
/// ingredients or a planted seed, and how many a recipe makes.
/// </summary>
internal sealed record ProfessionRow(
    EntityReference Product,
    SourceKind Kind,
    int? Level,
    double? Experience,
    IReadOnlyList<ItemAmount> Inputs,
    int? Output
);

/// <summary>The gathering resources by category and name. Written as <c>resources.json</c>.</summary>
internal sealed record ResourceIndex(IReadOnlyList<ResourceSummary> Resources);

/// <summary>A resource as the resource list shows it: its category, the level it needs and how many items it gives.</summary>
internal sealed record ResourceSummary(
    string Key,
    string Slug,
    string Name,
    string Category,
    int Loot
)
{
    public string? Icon { get; init; }

    /// <summary>The profession whose level it needs and that level, when the game sets one.</summary>
    public string? Profession { get; init; }

    public int? Level { get; init; }
}

/// <summary>
/// Everything a resource page shows: how a harvest works when lab runs measured it, the items it gives with the chance
/// per item, and where it stands. <see cref="Category"/> is the profession whose level it checks, Farming for crops,
/// or Gathering. Written as <c>resources/&lt;slug&gt;.json</c>.
/// </summary>
internal sealed record ResourcePage(
    string Key,
    string Slug,
    string Name,
    string Category,
    IReadOnlyList<ResourceLoot> Loot,
    IReadOnlyList<ResourcePlace> Places
)
{
    [JsonRequired]
    public EntityKind Kind { get; init; } = EntityKind.Resource;

    public string? Icon { get; init; }

    public ResourceHarvest? Harvest { get; init; }

    /// <summary>The game chooses at random what each of some spots is, so only some of them give this resource.</summary>
    [JsonRequired]
    public bool Drawn { get; init; }
}

/// <summary>
/// What one harvest of a resource gives, as runs yield1, yield7, forage1 and curve1 measured it. <see cref="Experience"/>
/// is given per pick, or for Woodcutting and Mining only when you hit the minigame's green area, which also rolls for
/// bonus items with <see cref="Bonus"/>: the chance at each level from <see cref="BonusFrom"/>, the last one holding for
/// every level above. Bonus is empty when the resource never rolls it.
/// </summary>
internal sealed record ResourceHarvest(
    string Profession,
    int Level,
    int Experience,
    bool Minigame,
    int ItemsPerSpot,
    ResourceAfterUse AfterUse,
    int BonusFrom,
    IReadOnlyList<double> Bonus
);

internal enum ResourceAfterUse
{
    /// <summary>The spot is removed a few seconds after its last harvest.</summary>
    Removed,

    /// <summary>The spot stays and refuses further harvests.</summary>
    Stays,
}

/// <summary>An item a resource gives, with the chance per harvest, by the relations that the item page shows too.</summary>
internal sealed record ResourceLoot(
    EntityReference Item,
    IReadOnlyList<string> RelationIds,
    double? Chance,
    bool Unconfirmed
)
{
    /// <summary>The item's value in gold, as its page gives it.</summary>
    public double? Value { get; init; }

    [JsonRequired]
    public IReadOnlyList<RelationCondition> Conditions { get; init; } = [];
}

/// <summary>
/// A place a resource stands in: how many of it a visit finds on average, since the game decides on each load which
/// spots are there and what a drawn spot becomes, and how many it can hold at most. <see cref="More"/> marks a place
/// where a dungeon adds outcrops or trees in a number no run has measured, which neither count includes.
/// </summary>
internal sealed record ResourcePlace(EntityReference Place, double Expected, int Most, bool More);

/// <summary>The disciplines in the skill window's order. Written as <c>disciplines.json</c>.</summary>
internal sealed record DisciplineIndex(IReadOnlyList<DisciplineSummary> Disciplines);

/// <summary>
/// A discipline as the discipline list shows it. <see cref="Category"/> is the skill window's category, such as
/// Magic.
/// </summary>
internal sealed record DisciplineSummary(
    string Key,
    string Slug,
    string Name,
    CategoryLink Category,
    int Skills,
    int Passives,
    int Spellsongs
)
{
    /// <summary>The discipline's icon, as <see cref="DisciplinePage.Icon"/>.</summary>
    public string? Icon { get; init; }
}

/// <summary>
/// Everything a discipline page shows, laid out as the game's tree window. Written as
/// <c>disciplines/&lt;slug&gt;.json</c>. <see cref="Key"/> is the tree's widget class path. <see cref="LevelName"/>
/// is the name the game tracks the discipline's level by, such as Pyromancy, which the progression rules read.
/// </summary>
internal sealed record DisciplinePage(
    string Key,
    string Slug,
    string Name,
    CategoryLink Category,
    string? LevelName,
    IReadOnlyList<SkillRow> SkillRows,
    IReadOnlyList<PassiveRow> PassiveRows,
    IReadOnlyList<SpellsongEntry> Spellsongs
)
{
    [JsonRequired]
    public EntityKind Kind { get; init; } = EntityKind.Discipline;

    [JsonRequired]
    public IReadOnlyList<EntityRelation> Relations { get; init; } = [];

    /// <summary>Known issues that affect what this page shows.</summary>
    [JsonRequired]
    public IReadOnlyList<IssueNote> Notes { get; init; } = [];

    /// <summary>
    /// The icon that stands for the discipline in lists and search: its first skill's, or its first passive's when it
    /// has no skills, because the game draws no icon of its own for a tree.
    /// </summary>
    public string? Icon { get; init; }
}

/// <summary>A row of the tree's skill panel: the level its label shows and its slots in the game's order.</summary>
internal sealed record SkillRow(int Level, IReadOnlyList<SkillSlotEntry> Slots);

/// <summary>
/// A skill slot: its skill, and the level of <see cref="LevelDiscipline"/> that unlocks it. That discipline is
/// usually the slot's own, but a slot can check another's.
/// </summary>
internal sealed record SkillSlotEntry(
    EntityReference Skill,
    int RequiredLevel,
    EntityReference LevelDiscipline
);

/// <summary>A row of the tree's proficiency panel: the level its label shows and its passives in order.</summary>
internal sealed record PassiveRow(int Level, IReadOnlyList<PassiveEntry> Passives);

/// <summary>
/// A passive of a discipline. <see cref="Anchor"/> is its fragment on the discipline page. It unlocks at
/// <see cref="RequiredLevel"/> in <see cref="LevelDiscipline"/>. Rank k costs <see cref="PointCost"/> plus k - 1
/// times <see cref="CostIncrease"/>, for <see cref="Ranks"/> ranks, or null when the game's files do not give its
/// maximum rank. <see cref="RankName"/> is the name the game gives its later ranks, when it has one.
/// </summary>
internal sealed record PassiveEntry(
    string Key,
    string Anchor,
    string Name,
    string? Description,
    int RequiredLevel,
    EntityReference LevelDiscipline,
    int PointCost,
    int CostIncrease,
    int? Ranks,
    string? RankName,
    string? Icon
);

/// <summary>
/// A spellsong, played on the lute. <see cref="Tune"/> is its notes as the game numbers them, from 1 to 4.
/// </summary>
internal sealed record SpellsongEntry(
    string Key,
    string Anchor,
    string Name,
    string? Description,
    IReadOnlyList<int> Tune,
    int Charges,
    double Experience,
    IReadOnlyList<Source> Sources
);

/// <summary>The skill list's filters, columns and rows. Written as <c>skills.json</c>.</summary>
internal sealed record SkillIndex(
    IReadOnlyList<CategorySummary> Categories,
    IReadOnlyList<SkillSummary> Skills
)
{
    [JsonRequired]
    public IReadOnlyList<ListFacet> Facets { get; init; } = [];

    [JsonRequired]
    public IReadOnlyList<ListColumn> Columns { get; init; } = [];
}

/// <summary>What the skill list shows of a skill. <see cref="Category"/> is its skill window category slug.</summary>
internal sealed record SkillSummary(
    string Key,
    string Slug,
    string Name,
    string? Icon,
    string Category,
    ItemStatus Status
)
{
    [JsonRequired]
    public EntityKind Kind { get; init; } = EntityKind.Skill;

    /// <summary>The version this row leads to on its page, and the label its tab shows, as item rows carry.</summary>
    public string? VersionId { get; init; }
    public string? VersionLabel { get; init; }

    [JsonRequired]
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Facets { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>();

    [JsonRequired]
    public IReadOnlyDictionary<string, ListCell> Cells { get; init; } =
        new Dictionary<string, ListCell>();

    [JsonRequired]
    public IReadOnlyList<IReadOnlyList<string>> PlaceSources { get; init; } = [];
}

/// <summary>
/// Everything a skill page shows. Written as <c>skills/&lt;slug&gt;.json</c>. <see cref="Key"/> is the skill's class
/// path.
/// </summary>
internal sealed record SkillPage(
    string Key,
    string Slug,
    string Name,
    string? Description,
    string? Icon,
    IReadOnlyList<Fact> Facts,
    IReadOnlyList<Source> Sources
) : IVersionedPage
{
    [JsonRequired]
    public EntityKind Kind { get; init; } = EntityKind.Skill;

    [JsonRequired]
    public ItemStatus Status { get; init; } = ItemStatus.Obtainable;

    public string? VersionId { get; init; }
    public string? VersionLabel { get; init; }
    public string? VersionName { get; init; }

    /// <summary>The other versions on this page, as on an item page. These entries have no nested versions.</summary>
    [JsonRequired]
    public IReadOnlyList<SkillPage> Versions { get; init; } = [];

    [JsonRequired]
    public IReadOnlyList<EntityRelation> Relations { get; init; } = [];

    [JsonRequired]
    public IReadOnlyList<IssueNote> Notes { get; init; } = [];

    /// <summary>The passives whose ranks raise this skill's damage, or null when none do.</summary>
    public DamageScaling? DamageScaling { get; init; }
}

/// <summary>
/// The damage passives of <see cref="Discipline"/> that a skill's cast applies. Their bonuses add up before they
/// multiply the skill's damage, as run skill-damage-recovery measured.
/// </summary>
internal sealed record DamageScaling(
    EntityReference Discipline,
    IReadOnlyList<ScalingPassive> Passives
);

/// <summary>A passive that adds <see cref="PerRank"/>, as a fraction, to a skill's damage for each of its ranks.</summary>
internal sealed record ScalingPassive(EntityReference Passive, double PerRank, int Ranks);

/// <summary>An entity whose entities of one name share a page, one version for each different thing they show.</summary>
internal interface IVersionedPage
{
    string Key { get; }
    string Slug { get; }
    string Name { get; }
    string? Icon { get; }
    string? Description { get; }
    IReadOnlyList<Fact> Facts { get; }
    IReadOnlyList<Source> Sources { get; }
    ItemStatus Status { get; }
    string? VersionId { get; }
}

/// <summary>The files of the site data, gathered only to export one schema for all of them.</summary>
internal sealed record SiteDataFiles(
    SiteBuild Build,
    ItemIndex Items,
    ItemPage Item,
    IReadOnlyList<EntityKindDefinition> Kinds,
    EntityGraph Graph,
    IReadOnlyList<PublishedIssue> Issues,
    IReadOnlyDictionary<string, EntityReference> Links,
    CraftingData Crafting,
    DisciplineIndex Disciplines,
    DisciplinePage Discipline,
    SkillIndex Skills,
    SkillPage Skill,
    ProfessionIndex Professions,
    ProfessionPage Profession,
    MechanicIndex Mechanics,
    MechanicPage Mechanic,
    ResourceIndex Resources,
    ResourcePage Resource,
    PlannerData Planner
);

/// <summary>
/// The skill planner's inputs, which only the planner loads: every discipline's rows and passives and every skill a
/// character can learn, by catalog key. <see cref="Format"/> changes when a field changes meaning, so a stored build
/// can tell it was planned against other inputs. <see cref="Build"/> is the game build the inputs come from.
/// </summary>
internal sealed record PlannerData(
    long Build,
    int Format,
    IReadOnlyList<PlannerDiscipline> Disciplines,
    IReadOnlyList<PlannerSkill> Skills
);

/// <summary>
/// A discipline as the planner edits it. <see cref="LevelName"/> is the level the game tracks for it, which the
/// proficiency rule reads, or null for a tree without a level of its own.
/// </summary>
internal sealed record PlannerDiscipline(
    string Key,
    string Slug,
    string Name,
    string? Icon,
    string Category,
    string? LevelName,
    IReadOnlyList<PlannerSlot> Slots,
    IReadOnlyList<PlannerPassive> Passives
);

/// <summary>
/// A skill slot of a discipline's tree: the row it shows in, the skill's key, and the level of
/// <see cref="LevelDiscipline"/> it needs.
/// </summary>
internal sealed record PlannerSlot(
    int Row,
    string Skill,
    int RequiredLevel,
    string LevelDiscipline
);

/// <summary>
/// A passive as the planner buys its ranks. <see cref="Ranks"/> is null when the game's files do not give its maximum
/// rank, so the planner offers only the ranks it knows.
/// </summary>
internal sealed record PlannerPassive(
    string Key,
    string Anchor,
    string Name,
    string? Icon,
    int RequiredLevel,
    string LevelDiscipline,
    int PointCost,
    int CostIncrease,
    int? Ranks
);

/// <summary>
/// A skill the planner can place on the hotbar. <see cref="Learned"/> is true for a skill a book, conversation or
/// event teaches, which costs no skill point, and false for one only a tree holds.
/// </summary>
internal sealed record PlannerSkill(
    string Key,
    string Slug,
    string Name,
    string? Icon,
    bool Learned
);
