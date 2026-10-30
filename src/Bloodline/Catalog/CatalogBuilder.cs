using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed record CatalogResult(
    ItemIndex Index,
    IReadOnlyList<ItemPage> Items,
    MaintainerReport Report,
    int NamedPlaces,
    EntityGraph Graph
)
{
    public IReadOnlyList<FactClaim> Claims { get; init; } = [];

    /// <summary>The comparison with in-game tooltips, null when no lab run of the build read any.</summary>
    public TooltipReport? Tooltips { get; init; }

    /// <summary>The known game issues, each with the published pages that note it.</summary>
    public IReadOnlyList<PublishedIssue> Issues { get; init; } = [];
}

internal static class CatalogBuilder
{
    public static CatalogResult Build(
        CatalogRecords records,
        IReadOnlyList<Exclusion>? exclusions = null,
        IReadOnlyList<ISourceMechanism>? mechanisms = null,
        string? iconsDirectory = null
    )
    {
        exclusions ??= Exclusions.Reviewed;
        mechanisms ??=
        [
            new PickupMechanism(),
            new ShopMechanism(),
            new CurrencyShopMechanism(),
            new CraftingMechanism(),
            new CookingMechanism(),
            new DialogueGrantMechanism(),
            new FishingMechanism(),
            new GatheringMechanism(),
            new FarmingMechanism(),
            new BountyNoteMechanism(),
            new SettlementDeliveryMechanism(),
            new PickupObjectMechanism(),
            new ContainerLootMechanism(),
            new RandomLootMechanism(),
            new EnemyDropMechanism(),
        ];
        Exclusions.Validate(records, exclusions);
        var resolver = new ItemFacts(
            records.Enums,
            records.Tables,
            records.Code,
            records.Build.BuildId
        );
        var selected = records.Classes.Where(ItemIdentity.IsInventoryItem).ToArray();
        var initial = selected.ToDictionary(
            item => item.Path,
            item => resolver.Resolve(item, ""),
            StringComparer.OrdinalIgnoreCase
        );
        var identities = ItemIdentity.Identities(initial.Values);
        foreach (var (id, identity) in identities)
        {
            initial[id] = initial[id] with { Slug = identity.Slug, Qualifier = identity.Qualifier };
        }
        var context = new CatalogContext(records, initial, exclusions);
        var candidates = mechanisms
            .SelectMany(mechanism => mechanism.FindSources(context))
            .Where(candidate => !context.IsExcludedClass(candidate.ItemClass))
            .ToLookup(candidate => candidate.ItemClass, StringComparer.OrdinalIgnoreCase);
        var textures = records.Textures.ToDictionary(
            texture => texture.Path,
            StringComparer.OrdinalIgnoreCase
        );
        var statuses = new ItemStatuses(records, context.ExcludedLevels);
        var gapEntries = Enum.GetValues<GapKind>()
            .ToDictionary(kind => kind, _ => new List<GapEntry>());
        var items = new List<ItemPage>();
        foreach (var type in selected.Where(type => !context.IsExcludedClass(type.Path)))
        {
            var item = initial[type.Path];
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
                gapEntries[GapKind.UnknownCondition]
                    .Add(
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
            item = item with
            {
                Icon = icon,
                Sources = sources,
                Status = statuses.Of(item.Key, sources.Count),
            };
            items.Add(item);
            void Gap(GapKind kind, string label, string? detail = null) =>
                gapEntries[kind].Add(new GapEntry(label, CatalogGraph.Reference(item), detail));
            if (sources.Count == 0)
            {
                Gap(GapKind.ItemWithoutSource, item.Name);
            }
            if (icon is null)
            {
                Gap(
                    GapKind.ItemWithoutIcon,
                    item.Name,
                    texturePath ?? "No icon texture reference."
                );
            }
            foreach (var fact in item.Facts.Where(fact => StatVocabulary.Display(fact) is null))
            {
                Gap(GapKind.UnknownFact, item.Name, fact.Label);
            }
            if (item.Type == "Unknown")
            {
                Gap(GapKind.UnknownFact, item.Name, "Type");
            }
            if (item.Description is null)
            {
                Gap(GapKind.UnknownFact, item.Name, "Description");
            }
            foreach (var source in sources.Where(SourceCoverage.HasUnknownPlace))
            {
                gapEntries[GapKind.SourceWithoutPlace]
                    .Add(
                        new GapEntry(item.Name, CatalogGraph.Reference(item), source.Name)
                        {
                            Source = source.Kind,
                        }
                    );
            }
        }
        foreach (
            var place in context.Places.All.Where(place =>
                place.Key is { } key
                && !context.ExcludedLevels.Contains(key)
                && !context.Places.HasName(key)
            )
        )
        {
            gapEntries[GapKind.PlaceWithoutName].Add(new GapEntry(place.Name, place, place.Key));
        }
        foreach (var gap in records.Gaps)
        {
            gapEntries[GapKind.UnreadObject]
                .Add(new GapEntry(gap.Object ?? gap.Package, null, gap.Package + ": " + gap.Error));
        }
        foreach (var mapping in CraftingMechanism.MissingRows(context))
        {
            gapEntries[GapKind.MissingRecipeRow]
                .Add(
                    new GapEntry(
                        context.ItemLink(mapping.Class).Name,
                        null,
                        "Recipe row " + mapping.Name
                    )
                );
        }
        foreach (var (recipe, name, item) in CraftingMechanism.RenamedProducts(context))
        {
            gapEntries[GapKind.RenamedRecipeProduct]
                .Add(
                    new GapEntry(name, null, recipe + ": built on " + context.ItemLink(item).Name)
                );
        }
        var graphResult = CatalogGraph.Build(items, context.Places.All);
        var withoutPages = EntityKinds
            .All.Where(kind => !kind.HasPages)
            .Select(kind => kind.Kind)
            .ToHashSet();
        foreach (
            var shared in graphResult
                .Graph.Entities.Select(document => document.Entity)
                .Where(entity => withoutPages.Contains(entity.Kind) && entity.Key is not null)
                .GroupBy(entity => (entity.Kind, entity.Name))
                .Where(group => group.Count() > 1)
        )
        {
            foreach (var entity in shared.OrderBy(entity => entity.Key, StringComparer.Ordinal))
            {
                gapEntries[GapKind.SharedName]
                    .Add(new GapEntry(entity.Kind + ": " + entity.Name, entity, entity.Key));
            }
        }
        foreach (var item in graphResult.Items)
        {
            foreach (var source in item.Sources)
            {
                foreach (var condition in source.Conditions)
                {
                    if (condition.Kind == ConditionKind.Unknown)
                    {
                        gapEntries[GapKind.UnknownCondition]
                            .Add(
                                new GapEntry(
                                    item.Name,
                                    CatalogGraph.Reference(item),
                                    source.Name + ": " + condition.Label
                                )
                                {
                                    Source = source.Kind,
                                }
                            );
                    }
                }
            }
            foreach (var reference in References(item).Where(reference => reference.Key is null))
            {
                gapEntries[GapKind.UnresolvedReference]
                    .Add(
                        new GapEntry(
                            item.Name,
                            CatalogGraph.Reference(item),
                            reference.Kind + ": " + reference.Name
                        )
                    );
            }
        }
        var claims = resolver.Claims.Where(claim => !context.IsExcludedClass(claim.Item)).ToArray();
        var tooltips =
            records.Tooltips.Count == 0
                ? null
                : TooltipCheck.Check(
                    records.Build.BuildId,
                    graphResult.Items,
                    records.Tooltips,
                    claims
                );
        var pages = graphResult.Items.ToDictionary(item => item.Key, StringComparer.Ordinal);
        // Known causes are owned elsewhere, so coverage lists only the differences that no cause explains.
        foreach (
            var difference in tooltips?.Differences.Where(difference => difference.Reason is null)
                ?? []
        )
        {
            var item = pages[difference.Item];
            gapEntries[GapKind.TooltipDifference]
                .Add(
                    new GapEntry(
                        item.Name,
                        CatalogGraph.Reference(item),
                        TooltipCheck.Describe(difference)
                    )
                );
        }
        // Issue causes cite code as facts do, so they fail on the same missing or partly parsed functions.
        if (records.Code is { } code)
        {
            foreach (var issue in records.Issues.Issues)
            {
                ClaimCitations.Resolve("Known issue " + issue.Id, issue.Citations, code);
            }
        }
        var (noted, issues) = KnownIssues.Attach(
            TooltipCheck.WithGameShows(graphResult.Items, tooltips),
            records.Issues,
            tooltips,
            resolver.IgnoredFoodEffects
        );
        var ordered = noted
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Key, StringComparer.Ordinal)
            .ToArray();
        var summaries = ordered
            .Select(item => new ItemSummary(
                item.Slug,
                item.Name,
                item.Category.Slug,
                ReadValue(item),
                item.Icon,
                item.Sources.Select(source => source.Kind).Distinct().Order().ToArray()
            )
            {
                Key = item.Key,
                Type = item.Type,
                Qualifier = item.Qualifier,
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
        var report = new MaintainerReport(
            gapEntries
                .Where(pair => pair.Value.Count > 0)
                .OrderBy(pair => pair.Key)
                .Select(pair => new ReportGap(
                    pair.Key,
                    pair.Value.Distinct()
                        .OrderBy(entry => entry.Label, StringComparer.Ordinal)
                        .ThenBy(entry => entry.Entity?.Slug, StringComparer.Ordinal)
                        .ThenBy(entry => entry.Detail, StringComparer.Ordinal)
                        .ToArray()
                ))
                .ToArray(),
            ordered
                .Where(item => item.Status != ItemStatus.Obtainable)
                .Select(item => new StatusEvidence(
                    CatalogGraph.Reference(item),
                    item.Status,
                    statuses.NamedBy(item.Key)
                ))
                .ToArray()
        );
        return new CatalogResult(
            new ItemIndex(categories, summaries)
            {
                Facets = ItemList.FacetDefinitions(ordered, categories),
                Columns = ItemList.ColumnDefinitions(ordered),
            },
            ordered,
            report,
            context.Places.NamedCount,
            graphResult.Graph
        )
        {
            Claims = claims,
            Tooltips = tooltips,
            Issues = issues,
        };
    }

    private static IEnumerable<EntityReference> References(ItemPage item)
    {
        foreach (var source in item.Sources)
        {
            yield return source.Entity;
            foreach (var place in source.Places)
            {
                yield return place;
            }
            foreach (var input in source.Inputs)
            {
                yield return input.Item;
            }
            if (source.Price?.Currency is { } currency)
            {
                yield return currency;
            }
            foreach (var condition in source.Conditions)
            {
                if (condition.Entity is { } entity)
                {
                    yield return entity;
                }
            }
        }
    }

    private static int? ReadValue(ItemPage item) =>
        item.Facts.First(fact => fact.Stat == Stat.Value).Number is { } value
        && value == Math.Floor(value)
        && value is >= int.MinValue and <= int.MaxValue
            ? (int)value
            : null;

    private static bool IsTextureGap(GapRecord gap, string path) =>
        gap.Object is null
            ? gap.Package.Equals(path.Split('.')[0], StringComparison.OrdinalIgnoreCase)
            : gap.Object.Equals(path, StringComparison.OrdinalIgnoreCase);
}
