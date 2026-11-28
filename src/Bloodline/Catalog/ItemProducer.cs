using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

/// <summary>The item kind's output: its list index, its pages and what it adds to the catalog result.</summary>
internal sealed record ItemOutput(
    ItemIndex Index,
    IReadOnlyList<ItemPage> Items,
    IReadOnlyList<StatusEvidence> Statuses,
    IReadOnlyList<FactClaim> Claims,
    TooltipReport? Tooltips
);

/// <summary>
/// Produces items: selects the inventory classes, resolves their facts, icons and renders, attaches their sources and
/// statuses, compares them with in-game tooltips and groups same-named versions. The catalog builds the shared graph
/// between <see cref="Prepare"/> and <see cref="Finish"/>.
/// </summary>
internal sealed class ItemProducer
{
    private readonly CatalogRecords records;
    private readonly ItemFacts resolver;
    private readonly IReadOnlyList<ClassRecord> selected;
    private readonly string? iconsDirectory;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<ItemRender>>? renders;

    public ItemProducer(
        CatalogRecords records,
        string? iconsDirectory,
        IReadOnlyDictionary<string, IReadOnlyList<ItemRender>>? renders
    )
    {
        this.records = records;
        this.iconsDirectory = iconsDirectory;
        this.renders = renders;
        resolver = new ItemFacts(
            records.Enums,
            records.Tables,
            records.Code,
            records.Build.BuildId,
            records.Classes,
            records.MeasuredDamage
        );
        selected = records.Classes.Where(ItemIdentity.IsInventoryItem).ToArray();
        foreach (var entry in records.MeasuredDamage)
            if (!selected.Any(item => item.Path == entry.Item))
                throw new InvalidDataException(
                    $"Measured damage names {entry.Item}, which is no item."
                );
        Initial = selected.ToDictionary(
            item => item.Path,
            item =>
                resolver.Resolve(
                    item,
                    ItemIdentity.Slug(
                        RecordValues.Text(RecordValues.At(item.Properties, "ItemInfo.ItemName"))!
                    )
                ),
            StringComparer.OrdinalIgnoreCase
        );
    }

    /// <summary>Every item with its facts, before sources, which mechanisms read to name and link items.</summary>
    public IReadOnlyDictionary<string, ItemPage> Initial { get; }

    /// <summary>The prepared items: pages grouped by shared name and what <see cref="Finish"/> needs.</summary>
    internal sealed record Prepared(
        Versioned<ItemPage> Grouped,
        ItemStatuses Statuses,
        IReadOnlyList<FactClaim> Claims,
        TooltipReport? Tooltips
    );

    public Prepared Prepare(
        CatalogContext context,
        ILookup<string, EntitySource> candidates,
        Coverage coverage
    )
    {
        var textures = records.Textures.ToDictionary(
            texture => texture.Path,
            StringComparer.OrdinalIgnoreCase
        );
        var statuses = new ItemStatuses(records, context.ExcludedLevels, context.CanAppear);
        var items = new List<ItemPage>();
        foreach (var type in selected.Where(type => !context.IsExcludedClass(type.Path)))
        {
            var item = Initial[type.Path];
            var texturePath = RecordValues.AnyReference(
                RecordValues.At(type.Properties, "ItemInfo.ItemImage")
            );
            string? icon = null;
            if (
                texturePath is not null
                && textures.TryGetValue(texturePath, out var texture)
                && !records.Gaps.Any(gap => IsTextureGap(gap, texturePath))
                && (
                    iconsDirectory is null
                    || File.Exists(Path.Combine(iconsDirectory, texture.Hash + ".png"))
                )
            )
            {
                icon = texture.Hash;
            }
            foreach (var candidate in candidates[item.Key])
            foreach (var label in candidate.Unresolved)
                coverage.Add(
                    GapKind.UnknownCondition,
                    new GapEntry(
                        item.Name,
                        CatalogGraph.Reference(item),
                        candidate.Source.Name + ": " + label
                    )
                    {
                        Source = candidate.Source.Kind,
                    }
                );
            var sources = CatalogContext.MergeSources(candidates[item.Key], context.ExcludedLevels);
            var confirmedSources = sources.Count(source => !source.Unconfirmed);
            item = item with
            {
                Icon = icon,
                Renders = renders?.GetValueOrDefault(item.Key) ?? [],
                Sources = sources,
                Status =
                    sources.Count > 0 && confirmedSources == 0
                        ? ItemStatus.NoKnownSource
                        : statuses.Of(item.Key, confirmedSources),
            };
            items.Add(item);
            var reference = CatalogGraph.Reference(item);
            if (confirmedSources == 0)
                coverage.Add(GapKind.ItemWithoutSource, item.Name, reference);
            if (icon is null)
                coverage.Add(
                    GapKind.ItemWithoutIcon,
                    item.Name,
                    reference,
                    texturePath ?? "No icon texture reference."
                );
            foreach (var fact in item.Facts.Where(fact => StatVocabulary.Display(fact) is null))
                coverage.Add(GapKind.UnknownFact, item.Name, reference, fact.Label);
            if (item.Type == "Unknown")
                coverage.Add(GapKind.UnknownFact, item.Name, reference, "Type");
            if (item.Description is null)
                coverage.Add(GapKind.UnknownFact, item.Name, reference, "Description");
            foreach (var source in sources.Where(SourceCoverage.HasUnknownPlace))
                coverage.Add(
                    GapKind.SourceWithoutPlace,
                    new GapEntry(item.Name, reference, source.Name) { Source = source.Kind }
                );
        }
        foreach (var mapping in CraftingMechanism.MissingRows(context))
            coverage.Add(
                GapKind.MissingRecipeRow,
                context.ItemLink(mapping.Class).Name,
                null,
                "Recipe row " + mapping.Name
            );
        foreach (var (recipe, name, item) in CraftingMechanism.RenamedProducts(context))
            coverage.Add(
                GapKind.RenamedRecipeProduct,
                name,
                null,
                recipe + ": built on " + context.ItemLink(item).Name
            );
        var claims = resolver.Claims.Where(claim => !context.IsExcludedClass(claim.Key)).ToArray();
        var tooltips =
            records.Tooltips.Count == 0
                ? null
                : TooltipCheck.Check(records.Build.BuildId, items, records.Tooltips, claims);
        var pages = items.ToDictionary(item => item.Key, StringComparer.Ordinal);
        // Known causes are owned elsewhere, so coverage lists only the differences that no cause explains.
        foreach (
            var difference in tooltips?.Differences.Where(difference => difference.Reason is null)
                ?? []
        )
        {
            var item = pages[difference.Item];
            coverage.Add(
                GapKind.TooltipDifference,
                item.Name,
                CatalogGraph.Reference(item),
                TooltipCheck.Describe(difference)
            );
        }
        var noted = KnownIssues.Attach(
            TooltipCheck.WithGameShows(items, tooltips),
            records.Issues,
            tooltips,
            resolver.IgnoredFoodEffects,
            resolver.UntrainedWeapons
        );
        return new Prepared(ItemVersions.Group(noted), statuses, claims, tooltips);
    }

    /// <summary>The canonical reference of an item version: its shared-name page when it has one.</summary>
    public static EntityReference Canonical(Prepared prepared, EntityReference reference) =>
        reference.Kind == EntityKind.Item
        && reference.Key is { } key
        && prepared.Grouped.References.TryGetValue(key, out var canonical)
            ? canonical
            : reference;

    public ItemOutput Finish(
        Prepared prepared,
        IReadOnlyList<ItemPage> graphItems,
        Coverage coverage
    )
    {
        var canonicalItems = graphItems.ToDictionary(item => item.Key, StringComparer.Ordinal);
        coverage.RemoveAll(
            GapKind.ItemWithoutSource,
            entry =>
                entry.Entity?.Key is { } key
                && canonicalItems.TryGetValue(key, out var item)
                && item.Sources.Any(source => !source.Unconfirmed)
        );
        var ordered = graphItems
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Key, StringComparer.Ordinal)
            .ToArray();
        var summaries = ordered
            .Select(item => new ItemSummary(
                item.Slug,
                item.Name,
                item.Category.Slug,
                ReadValue(item),
                item.Icon
            )
            {
                Key = item.Key,
                Type = item.Type,
                VersionId = item.VersionId,
                VersionLabel = item.VersionLabel,
                Status = item.Status,
                Facets = ItemList.FacetsOf(item),
                Cells = ItemList.CellsOf(item),
                PlaceSources = ItemList.PlaceSourcesOf(item),
            })
            .ToArray();
        var categories = ordered
            .GroupBy(item => item.Category)
            .OrderBy(group => group.Key.Name, StringComparer.Ordinal)
            .Select(group => new CategorySummary(
                group.Key.Slug,
                group.Key.Name,
                ItemCategories.Plural(group.Key.Name),
                group.Count()
            ))
            .ToArray();
        return new ItemOutput(
            new ItemIndex(categories, summaries)
            {
                Facets = ItemList.FacetDefinitions(ordered, categories),
                Columns = ItemList.ColumnDefinitions(ordered),
            },
            ordered,
            ordered
                .Where(item => item.Status != ItemStatus.Obtainable)
                .Select(item => new StatusEvidence(
                    CatalogGraph.Reference(item),
                    item.Status,
                    prepared.Statuses.NamedBy(item.Key)
                ))
                .ToArray(),
            prepared.Claims,
            prepared.Tooltips
        );
    }

    private static int? ReadValue(ItemPage item) =>
        item.Facts.FirstOrDefault(fact => fact.Stat == Stat.Value)?.Number is { } value
        && value == Math.Floor(value)
        && value is >= int.MinValue and <= int.MaxValue
            ? (int)value
            : null;

    private static bool IsTextureGap(GapRecord gap, string path) =>
        gap.Object is null
            ? gap.Package.Equals(path.Split('.')[0], StringComparison.OrdinalIgnoreCase)
            : gap.Object.Equals(path, StringComparison.OrdinalIgnoreCase);
}
