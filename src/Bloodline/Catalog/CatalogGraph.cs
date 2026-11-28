using System.Security.Cryptography;
using System.Text;
using Bloodline.Output;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

/// <summary>An entity of a kind other than items, with the sources the graph turns into its relations.</summary>
internal sealed record GraphEntity(EntityReference Reference, IReadOnlyList<Source> Sources);

/// <summary>
/// The graph, the item pages with their relations and uses, and for every other entity its document and its sources
/// with their relations resolved.
/// </summary>
internal sealed record GraphResult(EntityGraph Graph, IReadOnlyList<ItemPage> Items)
{
    public IReadOnlyDictionary<(EntityKind, string), EntityDocument> Documents { get; init; } =
        new Dictionary<(EntityKind, string), EntityDocument>();

    public IReadOnlyDictionary<(EntityKind, string), IReadOnlyList<Source>> Sources { get; init; } =
        new Dictionary<(EntityKind, string), IReadOnlyList<Source>>();

    /// <summary>What a skill or item makes creatures give on death, by the entity that applies the debuff.</summary>
    public IReadOnlyDictionary<
        (EntityKind, string),
        IReadOnlyList<Harvest>
    > Harvests { get; init; } = new Dictionary<(EntityKind, string), IReadOnlyList<Harvest>>();
}

internal static class CatalogGraph
{
    public static GraphResult Build(
        IReadOnlyList<ItemPage> items,
        IEnumerable<EntityReference>? references = null,
        IReadOnlyDictionary<string, EntityReference>? itemReferences = null,
        IReadOnlyList<GraphEntity>? others = null
    )
    {
        others ??= [];
        var entities = new Dictionary<(EntityKind Kind, string Key), EntityReference>();
        foreach (var item in items)
        {
            entities.Add((EntityKind.Item, item.Key), Reference(item));
        }
        foreach (var other in others)
        {
            entities.Add((other.Reference.Kind, other.Reference.Key!), other.Reference);
        }
        var outputs = items.ToDictionary(item => item.Key, StringComparer.Ordinal);
        EntityReference Resolve(EntityReference reference)
        {
            if (
                reference.Kind == EntityKind.Item
                && reference.Key is { } original
                && itemReferences is not null
                && itemReferences.TryGetValue(original, out var canonical)
            )
                reference = canonical;
            // Several classes share one creature name, such as two Wolves, and a reader meets the name. Mechanisms keep
            // the class, which events and conditions look up, and pages get the name.
            if (reference.Kind == EntityKind.Creature)
                reference = WorldSourceData.Creature(reference.Name);
            if (reference.Key is not { } key)
            {
                return reference with { Slug = null, Icon = null, Fragment = null };
            }
            var identity = (reference.Kind, key);
            if (entities.TryGetValue(identity, out var known))
            {
                // Items resolve to their page's name, and any other key names exactly one thing a player meets.
                if (reference.Kind != EntityKind.Item && known.Name != reference.Name)
                {
                    throw new InvalidDataException(
                        $"The {reference.Kind} key {key} names both {known.Name} and {reference.Name}."
                    );
                }
                return known;
            }
            entities.Add(identity, reference);
            return reference;
        }
        foreach (var reference in references ?? [])
        {
            Resolve(reference);
        }
        IReadOnlyList<RelationCondition> Conditions(IReadOnlyList<RelationCondition> conditions) =>
            conditions
                .Select(condition =>
                    condition with
                    {
                        Entity = condition.Entity is { } entity ? Resolve(entity) : null,
                    }
                )
                .ToArray();

        var rows = new Dictionary<string, RelationRow>(StringComparer.Ordinal);
        var uses = items.ToDictionary(
            item => item.Key,
            _ => new List<ItemUse>(),
            StringComparer.Ordinal
        );
        var harvests = new Dictionary<(EntityKind Kind, string Key), List<Harvest>>();
        var pages = new List<ItemPage>(items.Count);
        foreach (var item in items)
        {
            pages.Add(item with { Sources = ResolveSources(Reference(item), item.Sources) });
        }
        var otherSources = others.ToDictionary(
            other => (other.Reference.Kind, other.Reference.Key!),
            other => ResolveSources(other.Reference, other.Sources)
        );

        IReadOnlyList<Source> ResolveSources(
            EntityReference target,
            IReadOnlyList<Source> originals
        )
        {
            var sources = new Dictionary<string, Source>(StringComparer.Ordinal);
            foreach (var original in originals)
            {
                var source = original with
                {
                    Entity = Resolve(
                        original.Entity.Name.Length == 0
                            ? original.Entity with
                            {
                                Name = original.Name,
                            }
                            : original.Entity
                    ),
                    Places = original.Places.Select(Resolve).ToArray(),
                    Inputs = original
                        .Inputs.Select(input => input with { Item = Resolve(input.Item) })
                        .ToArray(),
                    Price = original.Price is { Currency: { } currency } price
                        ? price with
                        {
                            Currency = Resolve(currency),
                        }
                        : original.Price,
                    Conditions = Conditions(original.Conditions),
                    // A recipe's profession is an entity the graph and its pages know, as a condition's is.
                    Recipe = original.Recipe is { Profession: { } profession } recipe
                        ? recipe with
                        {
                            Profession = Resolve(profession),
                        }
                        : original.Recipe,
                };
                var id = Id(source.Kind.ToString(), source.Entity, target, source);
                source = source with { RelationId = id };
                // Each input and payment names its own relation, so the page that renders it carries that relation.
                source = source with
                {
                    Inputs = source
                        .Inputs.Select(input =>
                            input with
                            {
                                RelationId = source.Unconfirmed
                                    ? null
                                    : UseId("Input", input.Item, input.Quantity),
                            }
                        )
                        .ToArray(),
                    Price = source.Price is { Currency: { } paid } cost
                        ? cost with
                        {
                            RelationId = source.Unconfirmed
                                ? null
                                : UseId("Payment", paid, cost.Amount),
                        }
                        : source.Price,
                };
                rows.TryAdd(
                    id,
                    new RelationRow(
                        id,
                        source.Kind.ToString(),
                        source.Entity,
                        target,
                        ForwardLabel(source.Kind),
                        ReverseLabel(source.Kind),
                        source.Conditions,
                        source
                    )
                );
                sources.TryAdd(id, source);
                // The page of what applies the debuff shows the item it harvests, so that page carries the relation.
                if (
                    source
                        is {
                            Kind: SourceKind.DebuffKill,
                            Unconfirmed: false,
                            Entity.Key: { } giver
                        }
                    && entities.ContainsKey((source.Entity.Kind, giver))
                )
                {
                    if (!harvests.TryGetValue((source.Entity.Kind, giver), out var list))
                        harvests[(source.Entity.Kind, giver)] = list = [];
                    list.Add(
                        new Harvest(target, string.Join(" ", source.Triggers)) { RelationId = id }
                    );
                }
                if (!source.Unconfirmed)
                {
                    foreach (var input in source.Inputs)
                    {
                        AddUse(
                            "Input",
                            input.Item,
                            input.Quantity,
                            input.RelationId!,
                            source,
                            target
                        );
                    }
                    if (source.Price is { Currency: { } payment, RelationId: { } paymentId })
                    {
                        AddUse("Payment", payment, source.Price.Amount, paymentId, source, target);
                    }
                }

                string UseId(string kind, EntityReference input, int quantity) =>
                    Id(kind, input, target, new { RelationId = id, quantity });
            }
            return sources.Values.ToArray();
        }

        void AddUse(
            string kind,
            EntityReference input,
            int quantity,
            string id,
            Source source,
            EntityReference target
        )
        {
            rows.TryAdd(
                id,
                new RelationRow(
                    id,
                    kind,
                    input,
                    target,
                    kind == "Payment" ? "Pays for" : "Used to make",
                    kind == "Payment" ? "Paid with" : "Requires",
                    source.Conditions,
                    source
                )
            );
            if (input.Key is { } key && uses.TryGetValue(key, out var entries))
            {
                entries.Add(
                    new ItemUse(source.Kind, source.Name, target, quantity)
                    {
                        RelationId = id,
                        Conditions = source.Conditions,
                        Profession =
                            source.Recipe?.Profession
                            ?? source
                                .Conditions.FirstOrDefault(condition =>
                                    condition.Kind == ConditionKind.ProficiencyLevel
                                )
                                ?.Entity,
                        OutputCategory =
                            target.Key is { } output && outputs.TryGetValue(output, out var made)
                                ? made.Category.Name
                                : null,
                        OutputType =
                            target.Key is { } typed && outputs.TryGetValue(typed, out var product)
                                ? product.Type
                                : null,
                    }
                );
            }
        }

        var orderedRows = rows.Values.OrderBy(row => row.Id, StringComparer.Ordinal).ToArray();
        var projections = entities.Keys.ToDictionary(key => key, _ => new List<EntityRelation>());
        foreach (var row in orderedRows)
        {
            if (row.Source.Key is { } sourceKey)
            {
                projections[(row.Source.Kind, sourceKey)]
                    .Add(
                        new EntityRelation(
                            row.Id,
                            row.Kind,
                            RelationDirection.Outgoing,
                            row.Target,
                            row.ForwardLabel,
                            row.Conditions
                        )
                    );
            }
            if (row.Target.Key is { } targetKey)
            {
                projections[(row.Target.Kind, targetKey)]
                    .Add(
                        new EntityRelation(
                            row.Id,
                            row.Kind,
                            RelationDirection.Incoming,
                            row.Source,
                            row.ReverseLabel,
                            row.Conditions
                        )
                    );
            }
        }
        var documents = entities
            .OrderBy(pair => pair.Key.Kind)
            .ThenBy(pair => pair.Key.Key, StringComparer.Ordinal)
            .Select(pair => new EntityDocument(pair.Value, projections[pair.Key].ToArray()))
            .ToArray();
        var graph = new EntityGraph(documents, orderedRows);
        CatalogGraphCheck.Validate(graph, EntityKinds.All);
        return new GraphResult(
            graph,
            pages
                .Select(item =>
                    item with
                    {
                        Relations = projections[(EntityKind.Item, item.Key)].ToArray(),
                        Uses = uses[item.Key]
                            .DistinctBy(use => use.RelationId)
                            .OrderBy(use => use.Item.Name, StringComparer.OrdinalIgnoreCase)
                            .ThenBy(use => use.Name, StringComparer.OrdinalIgnoreCase)
                            .ToArray(),
                        Harvests =
                            harvests
                                .GetValueOrDefault((EntityKind.Item, item.Key))
                                ?.OrderBy(harvest => harvest.Item.Name, StringComparer.Ordinal)
                                .ToArray()
                            ?? [],
                    }
                )
                .ToArray()
        )
        {
            Documents = documents
                .Where(document => document.Entity.Kind != EntityKind.Item)
                .ToDictionary(document => (document.Entity.Kind, document.Entity.Key!)),
            Sources = otherSources,
            Harvests = harvests.ToDictionary(
                pair => pair.Key,
                pair =>
                    (IReadOnlyList<Harvest>)
                        pair
                            .Value.OrderBy(harvest => harvest.Item.Name, StringComparer.Ordinal)
                            .ToArray()
            ),
        };
    }

    public static EntityReference Reference(ItemPage item) =>
        EntityVersions.Reference(item, EntityKind.Item);

    private static string Id<T>(
        string kind,
        EntityReference source,
        EntityReference target,
        T attributes
    ) =>
        Convert.ToHexStringLower(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(
                    CanonicalJson.Serialize(
                        new
                        {
                            kind,
                            source,
                            target,
                            attributes,
                        }
                    )
                )
            )
        );

    private static string ForwardLabel(SourceKind kind) =>
        kind switch
        {
            SourceKind.Shop or SourceKind.CurrencyShop => "Sells",
            SourceKind.Smithing or SourceKind.Crafting or SourceKind.Cooking => "Produces",
            SourceKind.EnemyDrop => "Drops",
            SourceKind.Dialogue => "Gives",
            SourceKind.SkillTree => "Holds",
            SourceKind.SkillBook or SourceKind.NoteSheet => "Teaches",
            SourceKind.Event => "Unlocks",
            _ => "Provides",
        };

    private static string ReverseLabel(SourceKind kind) =>
        kind switch
        {
            SourceKind.Shop or SourceKind.CurrencyShop => "Sold by",
            SourceKind.Smithing or SourceKind.Crafting or SourceKind.Cooking => "Made with",
            SourceKind.EnemyDrop => "Dropped by",
            SourceKind.Dialogue => "Given by",
            SourceKind.SkillTree => "In the tree of",
            SourceKind.SkillBook or SourceKind.NoteSheet => "Taught by",
            SourceKind.Event => "Unlocked by",
            _ => "Obtained from",
        };
}
