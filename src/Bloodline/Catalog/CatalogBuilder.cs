using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed record CatalogResult(
    ItemIndex Index,
    IReadOnlyList<ItemPage> Items,
    CoverageReport Coverage,
    int NamedPlaces
)
{
    public IReadOnlyList<FactClaim> Claims { get; init; } = [];
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
            var sources = CatalogContext.MergeSources(candidates[item.Id], context.ExcludedLevels);
            item = item with { Icon = icon, Sources = sources };
            items.Add(item);
            void Gap(GapKind kind, string label, string? detail = null) =>
                gapEntries[kind].Add(new GapEntry(label, item.Slug, detail));
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
                    .Add(new GapEntry(item.Name, item.Slug, source.Name) { Source = source.Kind });
            }
        }
        foreach (
            var place in context.Places.All.Where(place =>
                !context.ExcludedLevels.Contains(place.Id) && !context.Places.HasName(place.Id)
            )
        )
        {
            gapEntries[GapKind.PlaceWithoutName].Add(new GapEntry(place.Name, null, place.Id));
        }
        foreach (var gap in records.Gaps)
        {
            gapEntries[GapKind.UnreadObject]
                .Add(new GapEntry(gap.Object ?? gap.Package, null, gap.Package + ": " + gap.Error));
        }
        var uses = items.ToDictionary(
            item => item.Slug,
            _ => new List<ItemUse>(),
            StringComparer.Ordinal
        );
        foreach (var output in items)
        {
            foreach (var source in output.Sources)
            {
                foreach (var input in source.Inputs)
                {
                    if (
                        input.Item.Slug is { } inputSlug
                        && uses.TryGetValue(inputSlug, out var entries)
                    )
                    {
                        entries.Add(
                            new ItemUse(
                                source.Kind,
                                source.Name,
                                new ItemLink(output.Slug, ItemIdentity.DisplayName(output)),
                                input.Quantity
                            )
                        );
                    }
                }
                if (
                    source.Price?.Currency?.Slug is { } currencySlug
                    && uses.TryGetValue(currencySlug, out var payments)
                )
                {
                    payments.Add(
                        new ItemUse(
                            source.Kind,
                            source.Name,
                            new ItemLink(output.Slug, ItemIdentity.DisplayName(output)),
                            source.Price.Amount
                        )
                    );
                }
            }
        }
        items = items
            .Select(item =>
                item with
                {
                    Uses = uses[item.Slug]
                        .Distinct()
                        .OrderBy(use => use.Item.Name, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(use => use.Name, StringComparer.OrdinalIgnoreCase)
                        .ToArray(),
                }
            )
            .ToList();
        var ordered = items
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
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
                Type = item.Type,
                Qualifier = item.Qualifier,
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
        var coverage = new CoverageReport(
            gapEntries
                .Where(pair => pair.Value.Count > 0)
                .OrderBy(pair => pair.Key)
                .Select(pair => new CoverageGap(
                    pair.Key,
                    pair.Value.Distinct()
                        .OrderBy(entry => entry.Label, StringComparer.Ordinal)
                        .ThenBy(entry => entry.ItemSlug, StringComparer.Ordinal)
                        .ThenBy(entry => entry.Detail, StringComparer.Ordinal)
                        .ToArray()
                ))
                .ToArray()
        );
        return new CatalogResult(
            new ItemIndex(categories, summaries),
            ordered,
            coverage,
            context.Places.NamedCount
        )
        {
            Claims = resolver.Claims.Where(claim => !context.IsExcludedClass(claim.Item)).ToArray(),
        };
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
