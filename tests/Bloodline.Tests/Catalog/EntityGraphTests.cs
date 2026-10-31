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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FileOnlyAcquisitionRetainsItsRelationWithoutClaimingInputsOrPaymentsAreUsed(
        bool unconfirmed
    )
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
        var source = new Source(
            SourceKind.Cooking,
            "Apple recipe",
            [],
            new Price(3, CatalogGraph.Reference(input)),
            new Amount(1, 1),
            [new ItemAmount(CatalogGraph.Reference(input), 2)],
            []
        )
        {
            Entity = Merchant,
            Unconfirmed = unconfirmed,
        };
        var output = input with
        {
            Key = Item.Key!,
            Slug = Item.Slug!,
            Name = Item.Name,
            Sources = [source],
        };
        var result = CatalogGraph.Build([input, output]);
        var published = Assert.Single(
            result.Items.Single(page => page.Key == output.Key).Sources
        );
        var acquisition = Assert.Single(
            result.Graph.Relations,
            relation => relation.Kind == "Cooking"
        );
        Assert.Equal(published.RelationId, acquisition.Id);
        Assert.Equal(unconfirmed, acquisition.Attributes!.Unconfirmed);
        Assert.Equal(
            unconfirmed ? 0 : 2,
            result.Items.Single(page => page.Key == input.Key).Uses.Count
        );
        Assert.Equal(
            unconfirmed ? 0 : 2,
            result.Graph.Relations.Count(relation => relation.Kind is "Input" or "Payment")
        );
        Assert.Equal(unconfirmed, published.Inputs[0].RelationId is null);
        Assert.Equal(unconfirmed, published.Price!.RelationId is null);
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
            result.Report.Gaps.Single(gap => gap.Kind == GapKind.UnknownCondition).Entries,
            entry => entry.Entity?.Slug == page.Slug && entry.Detail!.Contains("Unreadable check")
        );
        Assert.Contains(
            result.Report.Gaps.Single(gap => gap.Kind == GapKind.UnresolvedReference).Entries,
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

    [Fact]
    public void ContainersSharingAClassKeepTheNamesPlayersSee()
    {
        const string generic = "/Game/Loot/GenericLootable.GenericLootable_C";
        Source Loot(string name) =>
            new(SourceKind.ContainerLoot, name, [], null, new Amount(1, 1), [], [])
            {
                Entity = WorldSourceData.Reference(EntityKind.Container, generic, name),
            };
        var page = new ItemPage(
            Item.Key!,
            Item.Slug!,
            Item.Name,
            null,
            new CategoryLink("food", "Food"),
            null,
            [],
            [Loot("Cabinet"), Loot("Miner's Crate")]
        );
        var built = CatalogGraph.Build([page]).Items.Single();
        Assert.Equal(
            ["Cabinet", "Miner's Crate"],
            built.Sources.Select(source => source.Entity.Name)
        );

        // A key that still names two things fails instead of renaming one of them.
        var shared = page with
        {
            Sources = page
                .Sources.Select(source =>
                    source with
                    {
                        Entity = source.Entity with { Key = generic },
                    }
                )
                .ToArray(),
        };
        Assert.Throws<InvalidDataException>(() => CatalogGraph.Build([shared]));
    }
}
