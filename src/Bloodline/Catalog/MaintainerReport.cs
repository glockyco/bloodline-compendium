using Bloodline.SiteData;

namespace Bloodline.Catalog;

/// <summary>
/// What the catalog could not read or resolve, for the update procedure to review. Written as
/// <c>report.json</c> beside the site data, which it never enters. <see cref="Statuses"/> holds the evidence for
/// every item that is not obtainable.
/// </summary>
internal sealed record MaintainerReport(
    IReadOnlyList<ReportGap> Gaps,
    IReadOnlyList<StatusEvidence> Statuses
);

internal sealed record ReportGap(GapKind Kind, IReadOnlyList<GapEntry> Entries);

/// <summary>An item's status and the records outside the item that name it.</summary>
internal sealed record StatusEvidence(
    EntityReference Item,
    ItemStatus Status,
    IReadOnlyList<string> NamedBy
);

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
    TooltipDifference,

    /// <summary>
    /// Entities of a kind without pages that share a name, which need a decision before the kind gets pages: one
    /// thing a player meets, such as a town made of several levels, or distinct things that need telling apart.
    /// </summary>
    SharedName,

    /// <summary>
    /// Recipes that make an item on another item's class under the recipe's own name, which no page describes.
    /// </summary>
    RenamedRecipeProduct,

    /// <summary>Items the crafting window maps to a recipe row that the recipe table lacks.</summary>
    MissingRecipeRow,
}

/// <summary>
/// One entry of a gap. <see cref="Entity"/> names the affected entity when there is one, and
/// <see cref="Source"/> the kind of source a source gap is about.
/// </summary>
internal sealed record GapEntry(string Label, EntityReference? Entity, string? Detail)
{
    public SourceKind? Source { get; init; }
}
