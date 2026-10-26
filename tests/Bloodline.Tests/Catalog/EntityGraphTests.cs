using Bloodline.Catalog;
using Bloodline.SiteData;

namespace Bloodline.Tests.Catalog;

public sealed class EntityGraphTests
{
    private static readonly EntityReference Merchant = new(
        EntityKind.Merchant,
        "/Game/People/Trader.Trader_C",
        "Trader",
        null,
        null
    );
    private static readonly EntityReference Item = new(
        EntityKind.Item,
        "/Game/Items/Apple.Apple_C",
        "Apple",
        "apple",
        null
    );
    private static readonly RelationCondition Season = new(
        ConditionKind.CalendarEvent,
        new EntityReference(
            EntityKind.CalendarEvent,
            "/Game/Calendar:Autumn",
            "Autumn",
            null,
            null
        ),
        null,
        "During Autumn"
    );

    private static EntityGraph Graph()
    {
        var row = new RelationRow(
            "sale",
            "Shop",
            Merchant,
            Item,
            "Sells",
            "Sold by",
            [Season],
            null
        );
        return new EntityGraph(
            [
                new EntityDocument(
                    Merchant,
                    [
                        new EntityRelation(
                            "sale",
                            "Shop",
                            RelationDirection.Outgoing,
                            Item,
                            "Sells",
                            [Season]
                        ),
                    ]
                ),
                new EntityDocument(
                    Item,
                    [
                        new EntityRelation(
                            "sale",
                            "Shop",
                            RelationDirection.Incoming,
                            Merchant,
                            "Sold by",
                            [Season]
                        ),
                    ]
                ),
                new EntityDocument(Season.Entity!, []),
            ],
            [row]
        );
    }

    [Fact]
    public void AcceptsUnlinkedKindsAndConditionsInBothDirections() =>
        CatalogGraphCheck.Validate(Graph(), EntityKinds.All);

    [Fact]
    public void RejectsDanglingKeyAndNamesItsOwner()
    {
        var graph = Graph();
        graph = graph with
        {
            Entities = graph.Entities.Where(entity => entity.Entity != Merchant).ToArray(),
        };
        var error = Assert.Throws<InvalidDataException>(() =>
            CatalogGraphCheck.Validate(graph, EntityKinds.All)
        );
        Assert.Contains("sale", error.Message);
        Assert.Contains(Merchant.Key!, error.Message);
    }

    [Fact]
    public void RejectsDuplicateKey()
    {
        var graph = Graph();
        graph = graph with { Entities = [.. graph.Entities, graph.Entities[0]] };
        Assert.Contains(
            "Duplicate Merchant key",
            Assert
                .Throws<InvalidDataException>(() =>
                    CatalogGraphCheck.Validate(graph, EntityKinds.All)
                )
                .Message
        );
    }

    [Fact]
    public void RejectsDuplicateSlug()
    {
        var graph = Graph();
        graph = graph with
        {
            Entities =
            [
                .. graph.Entities,
                new EntityDocument(Item with { Key = "/Game/Other/Apple.Apple_C" }, []),
            ],
        };
        Assert.Contains(
            "Duplicate Item slug",
            Assert
                .Throws<InvalidDataException>(() =>
                    CatalogGraphCheck.Validate(graph, EntityKinds.All)
                )
                .Message
        );
    }

    [Fact]
    public void RejectsOneWayRelation()
    {
        var graph = Graph();
        graph = graph with
        {
            Entities = graph
                .Entities.Select(entity =>
                    entity.Entity == Merchant ? entity with { Relations = [] } : entity
                )
                .ToArray(),
        };
        Assert.Contains(
            "One-way",
            Assert
                .Throws<InvalidDataException>(() =>
                    CatalogGraphCheck.Validate(graph, EntityKinds.All)
                )
                .Message
        );
    }

    [Fact]
    public void RejectsLinksToKindsWithoutPages()
    {
        var graph = Graph();
        graph = graph with
        {
            Entities = graph
                .Entities.Select(entity =>
                    entity.Entity == Merchant
                        ? entity with
                        {
                            Entity = Merchant with { Slug = "trader" },
                        }
                        : entity
                )
                .ToArray(),
        };
        Assert.Contains(
            "without pages",
            Assert
                .Throws<InvalidDataException>(() =>
                    CatalogGraphCheck.Validate(graph, EntityKinds.All)
                )
                .Message
        );
    }

    [Fact]
    public void RejectsMissingReverseCondition()
    {
        var graph = Graph();
        graph = graph with
        {
            Entities = graph
                .Entities.Select(entity =>
                    entity.Entity == Merchant
                        ? entity with
                        {
                            Relations = [entity.Relations[0] with { Conditions = [] }],
                        }
                        : entity
                )
                .ToArray(),
        };
        Assert.Contains(
            "inconsistent",
            Assert
                .Throws<InvalidDataException>(() =>
                    CatalogGraphCheck.Validate(graph, EntityKinds.All)
                )
                .Message
        );
    }

    [Fact]
    public void IdentitySurvivesRenamingAndInputRelationsShareConditions()
    {
        var input = new ItemPage(
            "/Game/Items/Grain.Grain_C",
            "grain",
            "Grain",
            null,
            new CategoryLink("materials", "Material"),
            null,
            [],
            []
        );
        var output = new ItemPage(
            Item.Key!,
            Item.Slug!,
            Item.Name,
            null,
            new CategoryLink("food", "Food"),
            null,
            [],
            [
                new Source(
                    SourceKind.Cooking,
                    "Apple recipe",
                    [],
                    null,
                    new Amount(1, 1),
                    null,
                    null,
                    [new ItemAmount(CatalogGraph.Reference(input), 2)],
                    []
                )
                {
                    Entity = Merchant,
                    Conditions = [Season],
                },
            ]
        );
        var result = CatalogGraph.Build([input, output]);
        var renamed = CatalogGraph.Build([
            input,
            output with
            {
                Name = "New apple",
                Slug = "new-apple",
            },
        ]);
        Assert.Equal(Item.Key, renamed.Items.Single(item => item.Name == "New apple").Key);
        var use = Assert.Single(result.Items.Single(item => item.Key == input.Key).Uses);
        Assert.Equal([Season], use.Conditions);
        Assert.Equal(2, use.Quantity);
        var row = Assert.Single(result.Graph.Relations, relation => relation.Id == use.RelationId);
        Assert.Equal(input.Key, row.Source.Key);
        Assert.Equal(Item.Key, row.Target.Key);
        Assert.Equal(
            2,
            result.Graph.Entities.Sum(entity =>
                entity.Relations.Count(relation => relation.Id == row.Id)
            )
        );
    }

    [Fact]
    public void PublishesUnreadableConditionsAndUnresolvedReferencesInCoverage()
    {
        var type = new Bloodline.Records.ClassRecord(
            Item.Key!,
            [ItemIdentity.BaseItem],
            System
                .Text.Json.Nodes.JsonNode.Parse(
                    """{"ItemInfo":{"ItemName":{"$text":"Apple"},"ItemValue":1}}"""
                )!
                .AsObject()
        );
        var records = new CatalogRecords(
            new Bloodline.Records.BuildRecord(1159290, 1, []),
            [type],
            [],
            [],
            [],
            [],
            [],
            []
        );
        var result = CatalogBuilder.Build(records, [], [new UnknownGrant()]);
        var page = Assert.Single(result.Items);
        Assert.Null(Assert.Single(page.Sources).Entity.Key);
        Assert.Equal(ConditionKind.Unknown, Assert.Single(page.Sources[0].Conditions).Kind);
        Assert.Contains(
            result.Coverage.Gaps.Single(gap => gap.Kind == GapKind.UnknownCondition).Entries,
            entry => entry.Entity?.Slug == page.Slug && entry.Detail!.Contains("Unreadable check")
        );
        Assert.Contains(
            result.Coverage.Gaps.Single(gap => gap.Kind == GapKind.UnresolvedReference).Entries,
            entry =>
                entry.Entity?.Slug == page.Slug && entry.Detail == "Person: Unidentified speaker"
        );
    }

    private sealed class UnknownGrant : ISourceMechanism
    {
        public IEnumerable<ItemSource> FindSources(CatalogContext context)
        {
            yield return new ItemSource(
                Item.Key!,
                new Source(
                    SourceKind.Dialogue,
                    "Unidentified speaker",
                    [],
                    null,
                    new Amount(1, 1),
                    null,
                    null,
                    [],
                    []
                )
                {
                    Entity = new EntityReference(
                        EntityKind.Person,
                        null,
                        "Unidentified speaker",
                        null,
                        null
                    ),
                    Conditions =
                    [
                        new RelationCondition(
                            ConditionKind.Unknown,
                            null,
                            null,
                            "Unreadable check"
                        ),
                    ],
                }
            );
        }
    }
}
