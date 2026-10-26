using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal static class CatalogGraphCheck
{
    public static void Validate(EntityGraph graph, IReadOnlyList<EntityKindDefinition> kinds)
    {
        var registry = kinds.ToDictionary(kind => kind.Kind);
        var entities = new Dictionary<(EntityKind, string), EntityDocument>();
        var slugs = new HashSet<(EntityKind, string)>();
        var projections =
            new Dictionary<(EntityKind, string, string, RelationDirection), EntityRelation>();
        foreach (var document in graph.Entities)
        {
            var entity = document.Entity;
            if (entity.Key is not { Length: > 0 } key)
            {
                throw new InvalidDataException($"Entity {entity.Name} has no key.");
            }
            if (!entities.TryAdd((entity.Kind, key), document))
            {
                throw new InvalidDataException($"Duplicate {entity.Kind} key: {key}.");
            }
            if (entity.Slug is { } slug && !slugs.Add((entity.Kind, slug)))
            {
                throw new InvalidDataException($"Duplicate {entity.Kind} slug: {slug}.");
            }
            foreach (var relation in document.Relations)
            {
                if (
                    !projections.TryAdd(
                        (entity.Kind, key, relation.Id, relation.Direction),
                        relation
                    )
                )
                {
                    throw new InvalidDataException($"Duplicate relation {relation.Id} on {key}.");
                }
            }
        }
        void Reference(EntityReference reference, string owner)
        {
            if (!registry.TryGetValue(reference.Kind, out var kind))
            {
                throw new InvalidDataException(
                    $"{owner} references unregistered kind {reference.Kind}."
                );
            }
            if (reference.Slug is not null && (!kind.HasPages || reference.Key is null))
            {
                throw new InvalidDataException(
                    $"{owner} links to {reference.Kind} without pages: {reference.Name}."
                );
            }
            if (reference.Key is { } key)
            {
                if (!entities.TryGetValue((reference.Kind, key), out var target))
                {
                    throw new InvalidDataException(
                        $"{owner} references missing {reference.Kind} key: {key}."
                    );
                }
                if (target.Entity != reference)
                {
                    throw new InvalidDataException(
                        $"{owner} has an inconsistent reference to {key}."
                    );
                }
                if (kind.HasPages && reference.Slug is null)
                {
                    throw new InvalidDataException(
                        $"{owner} references {key} without its page slug."
                    );
                }
            }
        }
        void Conditions(IReadOnlyList<RelationCondition> conditions, string owner)
        {
            foreach (var condition in conditions)
            {
                if (condition.Entity is { } entity)
                {
                    Reference(entity, owner);
                }
            }
        }
        foreach (var document in graph.Entities)
        {
            Reference(document.Entity, document.Entity.Key!);
        }
        var rows = new Dictionary<string, RelationRow>(StringComparer.Ordinal);
        foreach (var row in graph.Relations)
        {
            if (!rows.TryAdd(row.Id, row))
            {
                throw new InvalidDataException($"Duplicate relation: {row.Id}.");
            }
            Reference(row.Source, row.Id);
            Reference(row.Target, row.Id);
            Conditions(row.Conditions, row.Id);
            if (row.Attributes is { } attributes)
            {
                Reference(attributes.Entity, row.Id);
                foreach (var place in attributes.Places)
                {
                    Reference(place, row.Id);
                }
                foreach (var input in attributes.Inputs)
                {
                    Reference(input.Item, row.Id);
                }
                if (attributes.Price?.Currency is { } currency)
                {
                    Reference(currency, row.Id);
                }
                Conditions(attributes.Conditions, row.Id);
            }
            CheckProjection(
                row,
                row.Source,
                row.Target,
                RelationDirection.Outgoing,
                row.ForwardLabel
            );
            CheckProjection(
                row,
                row.Target,
                row.Source,
                RelationDirection.Incoming,
                row.ReverseLabel
            );
        }
        void CheckProjection(
            RelationRow row,
            EntityReference owner,
            EntityReference other,
            RelationDirection direction,
            string label
        )
        {
            if (owner.Key is not { } key)
            {
                return;
            }
            if (
                !projections.TryGetValue((owner.Kind, key, row.Id, direction), out var projection)
                || projection.Entity != other
                || projection.Kind != row.Kind
                || projection.Label != label
                || !projection.Conditions.SequenceEqual(row.Conditions)
            )
            {
                throw new InvalidDataException(
                    $"One-way or inconsistent relation {row.Id} on {key}."
                );
            }
        }
        foreach (var document in graph.Entities)
        {
            foreach (var relation in document.Relations)
            {
                if (!rows.TryGetValue(relation.Id, out var row))
                {
                    throw new InvalidDataException(
                        $"{document.Entity.Key} has unknown relation {relation.Id}."
                    );
                }
                var endpoint =
                    relation.Direction == RelationDirection.Outgoing ? row.Source : row.Target;
                if (endpoint.Kind != document.Entity.Kind || endpoint.Key != document.Entity.Key)
                {
                    throw new InvalidDataException(
                        $"Relation {row.Id} is published on unrelated entity {document.Entity.Key}."
                    );
                }
            }
        }
    }
}
