using System.Security.Cryptography;
using System.Text;
using Bloodline.Output;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed record GraphResult(EntityGraph Graph, IReadOnlyList<ItemPage> Items);

internal static class CatalogGraph
{
    public static GraphResult Build(
        IReadOnlyList<ItemPage> items,
        IEnumerable<EntityReference>? references = null
    )
    {
        var entities = new Dictionary<(EntityKind Kind, string Key), EntityReference>();
        foreach (var item in items)
        {
            entities.Add((EntityKind.Item, item.Key), Reference(item));
        }
        var outputs = items.ToDictionary(item => item.Key, StringComparer.Ordinal);
        EntityReference Resolve(EntityReference reference)
        {
            if (reference.Key is not { } key)
            {
                return reference with { Slug = null, Icon = null };
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
        var pages = new List<ItemPage>(items.Count);
        foreach (var item in items)
        {
            var target = Reference(item);
            var sources = new Dictionary<string, Source>(StringComparer.Ordinal);
            foreach (var original in item.Sources)
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
                                RelationId = UseId("Input", input.Item, input.Quantity),
                            }
                        )
                        .ToArray(),
                    Price = source.Price is { Currency: { } paid } cost
                        ? cost with
                        {
                            RelationId = UseId("Payment", paid, cost.Amount),
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
                foreach (var input in source.Inputs)
                {
                    AddUse("Input", input.Item, input.Quantity, input.RelationId!, source, target);
                }
                if (source.Price is { Currency: { } payment, RelationId: { } paymentId })
                {
                    AddUse("Payment", payment, source.Price.Amount, paymentId, source, target);
                }

                string UseId(string kind, EntityReference input, int quantity) =>
                    Id(kind, input, target, new { RelationId = id, quantity });
            }
            pages.Add(item with { Sources = sources.Values.ToArray() });
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
                        Discipline = source.Recipe?.Discipline,
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
                    }
                )
                .ToArray()
        );
    }

    public static EntityReference Reference(ItemPage item) =>
        new(EntityKind.Item, item.Key, ItemIdentity.DisplayName(item), item.Slug, item.Icon);

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
            _ => "Provides",
        };

    private static string ReverseLabel(SourceKind kind) =>
        kind switch
        {
            SourceKind.Shop or SourceKind.CurrencyShop => "Sold by",
            SourceKind.Smithing or SourceKind.Crafting or SourceKind.Cooking => "Made with",
            SourceKind.EnemyDrop => "Dropped by",
            SourceKind.Dialogue => "Given by",
            _ => "Obtained from",
        };
}
