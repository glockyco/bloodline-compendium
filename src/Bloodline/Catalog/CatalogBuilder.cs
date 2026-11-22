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

    /// <summary>The link targets that text written for the site uses, as <c>links.json</c>.</summary>
    public IReadOnlyDictionary<string, EntityReference> Links { get; init; } =
        new Dictionary<string, EntityReference>();

    /// <summary>Disciplines and skills with their lists.</summary>
    public SkillOutput Skills { get; init; } =
        new(
            new DisciplineIndex([]),
            [],
            new SkillIndex([], []),
            [],
            [],
            new Dictionary<string, EntityReference>()
        );
}

internal static class CatalogBuilder
{
    public static CatalogResult Build(
        CatalogRecords records,
        IReadOnlyList<Exclusion>? exclusions = null,
        IReadOnlyList<ISourceMechanism>? mechanisms = null,
        string? iconsDirectory = null,
        IReadOnlyDictionary<string, IReadOnlyList<ItemRender>>? renders = null
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
            new SkillTreeMechanism(),
            new SkillBookMechanism(),
            new NoteSheetMechanism(),
            new SkillEventMechanism(),
        ];
        Exclusions.Validate(records, exclusions);
        var coverage = new Coverage();
        var itemProducer = new ItemProducer(records, iconsDirectory, renders);
        var context = new CatalogContext(records, itemProducer.Initial, exclusions);
        var candidates = mechanisms
            .SelectMany(mechanism => mechanism.FindSources(context))
            .Where(candidate => !context.IsExcludedClass(candidate.Key))
            .ToArray();
        var prepared = itemProducer.Prepare(
            context,
            candidates
                .Where(candidate => candidate.Kind == EntityKind.Item)
                .ToLookup(candidate => candidate.Key, StringComparer.OrdinalIgnoreCase),
            coverage
        );
        var skillProducer = new SkillProducer(context, iconsDirectory);
        var skillsPrepared = skillProducer.Prepare(
            candidates
                .Where(candidate => candidate.Kind != EntityKind.Item)
                .ToLookup(candidate => (candidate.Kind, candidate.Key)),
            coverage
        );
        foreach (
            var place in context.Places.All.Where(place =>
                place.Key is { } key
                && !context.ExcludedLevels.Contains(key)
                && !context.Places.HasName(key)
            )
        )
            coverage.Add(GapKind.PlaceWithoutName, new GapEntry(place.Name, place, place.Key));
        foreach (var gap in records.Gaps)
            coverage.Add(
                GapKind.UnreadObject,
                gap.Object ?? gap.Package,
                null,
                gap.Package + ": " + gap.Error
            );
        // Issue causes cite code as facts do, so they fail on the same missing or partly parsed functions.
        if (records.Code is { } code)
        {
            foreach (var issue in records.Issues.Issues)
                ClaimCitations.Resolve("Known issue " + issue.Id, issue.Citations, code);
        }
        var graphResult = CatalogGraph.Build(
            prepared.Grouped.Entities,
            context.Places.All,
            prepared.Grouped.References,
            skillsPrepared.GraphEntities
        );
        coverage.Canonicalize(reference => ItemProducer.Canonical(prepared, reference));
        var items = itemProducer.Finish(prepared, graphResult.Items, coverage);
        SharedNames(graphResult.Graph, coverage);
        foreach (var item in graphResult.Items)
            SourceGaps(CatalogGraph.Reference(item), item.Name, item.Sources, coverage);
        var skills = skillProducer.Finish(skillsPrepared, graphResult);
        SkillTooltipCheck.Compare(skills.Entries, records.SkillTooltips, coverage);
        // The Known Issues page lists every page of every kind that notes an issue.
        var issues = KnownIssues.Publish(
            records.Issues,
            [
                .. items.Items.Select(item => (CatalogGraph.Reference(item), item.Notes)),
                .. skills.Disciplines.Select(page =>
                    (SkillProducer.DisciplineReference(page), page.Notes)
                ),
                .. skills.Skills.Select(skill =>
                    (EntityVersions.Reference(skill, EntityKind.Skill), skill.Notes)
                ),
            ],
            prepared
                .Grouped.References.Concat(skills.References)
                .ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value,
                    StringComparer.OrdinalIgnoreCase
                )
        );
        foreach (var skill in skills.Skills)
            SourceGaps(
                EntityVersions.Reference(skill, EntityKind.Skill),
                skill.Name,
                skill.Sources,
                coverage
            );
        return new CatalogResult(
            items.Index,
            items.Items,
            coverage.Report(items.Statuses),
            context.Places.NamedCount,
            graphResult.Graph
        )
        {
            Claims = items.Claims,
            Tooltips = items.Tooltips,
            Issues = issues,
            Links = KnownIssues.Links(
                records.Issues,
                LinkedText.Targets([
                    .. items.Items.Select(CatalogGraph.Reference),
                    .. skills.Disciplines.Select(SkillProducer.DisciplineReference),
                    .. skills.Skills.Select(skill =>
                        EntityVersions.Reference(skill, EntityKind.Skill)
                    ),
                    .. Professions.All,
                ])
            ),
            Skills = skills,
        };
    }

    /// <summary>
    /// Entities of kinds without pages that share a name, which need a decision before their kind gets pages.
    /// </summary>
    private static void SharedNames(EntityGraph graph, Coverage coverage)
    {
        var withoutPages = EntityKinds
            .All.Where(kind => !kind.HasPages)
            .Select(kind => kind.Kind)
            .ToHashSet();
        foreach (
            var shared in graph
                .Entities.Select(document => document.Entity)
                .Where(entity => withoutPages.Contains(entity.Kind) && entity.Key is not null)
                .GroupBy(entity => (entity.Kind, entity.Name))
                .Where(group => group.Count() > 1)
        )
        {
            foreach (var entity in shared.OrderBy(entity => entity.Key, StringComparer.Ordinal))
                coverage.Add(
                    GapKind.SharedName,
                    entity.Kind + ": " + entity.Name,
                    entity,
                    entity.Key
                );
        }
    }

    /// <summary>The unknown conditions and unresolved references in one entity's sources.</summary>
    private static void SourceGaps(
        EntityReference entity,
        string name,
        IReadOnlyList<Source> sources,
        Coverage coverage
    )
    {
        foreach (var source in sources)
        {
            foreach (var condition in source.Conditions.Where(c => c.Kind == ConditionKind.Unknown))
                coverage.Add(
                    GapKind.UnknownCondition,
                    new GapEntry(name, entity, source.Name + ": " + condition.Label)
                    {
                        Source = source.Kind,
                    }
                );
        }
        foreach (var reference in References(sources).Where(reference => reference.Key is null))
            coverage.Add(
                GapKind.UnresolvedReference,
                name,
                entity,
                reference.Kind + ": " + reference.Name
            );
    }

    private static IEnumerable<EntityReference> References(IEnumerable<Source> sources)
    {
        foreach (var source in sources)
        {
            yield return source.Entity;
            foreach (var place in source.Places)
                yield return place;
            foreach (var input in source.Inputs)
                yield return input.Item;
            if (source.Price?.Currency is { } currency)
                yield return currency;
            foreach (var condition in source.Conditions)
            {
                if (condition.Entity is { } entity)
                    yield return entity;
            }
        }
    }
}
