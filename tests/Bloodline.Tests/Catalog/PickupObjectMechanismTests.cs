using System.Text.Json.Nodes;
using Bloodline.Catalog;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Tests.Catalog;

public sealed class PickupObjectMechanismTests
{
    private const string Item = "/Game/Invented/Leaf.Leaf_C";
    private const string Object = "/Game/Invented/Bundle.Bundle_C";
    private const string Level = "/Game/Invented/Grove";

    private static JsonObject Parse(string json) => JsonNode.Parse(json)!.AsObject();

    private static CatalogContext Context(
        JsonObject defaults,
        JsonObject? overrides = null,
        string? ancestor = null,
        bool placed = true,
        bool excluded = false
    )
    {
        var records = new CatalogRecords(
            new BuildRecord(1159290, 1, []),
            [
                new ClassRecord(
                    Item,
                    [ItemIdentity.BaseItem],
                    Parse("""{"ItemInfo":{"ItemName":{"$text":"Leaf"},"Quantity":3}}""")
                ),
                new ClassRecord(Object, [ancestor ?? ItemIdentity.Interactable], defaults),
            ],
            [],
            [],
            [
                new LevelRecord(
                    Level,
                    [],
                    placed
                        ? [new ActorRecord("Bundle1", Object, overrides ?? new JsonObject())]
                        : []
                ),
            ],
            [],
            [],
            []
        );
        return new CatalogContext(
            records,
            new Dictionary<string, ItemPage>(),
            excluded
                ?
                [
                    new Exclusion(
                        ExclusionKind.Level,
                        Level,
                        "Invented test level",
                        "Invented evidence"
                    ),
                ]
                : []
        );
    }

    [Fact]
    public void UsesDefaultItemAndPlacedQuantityOverrides()
    {
        var defaults = Parse(
            """{"Item":{"$ref":"/Game/Invented/Leaf.Leaf_C"},"Multiple Items?":true,"Amount of Items":1}"""
        );
        var source = Assert
            .Single(
                new PickupObjectMechanism().FindSources(
                    Context(defaults, Parse("""{"Amount of Items":4}"""))
                )
            )
            .Source;
        Assert.Equal(SourceKind.Pickup, source.Kind);
        Assert.Equal(new Amount(4, 4), source.Amount);
        Assert.Equal(Level, Assert.Single(source.Places).Id);
        Assert.Empty(source.Inputs);
        Assert.Null(source.Chance);
    }

    [Fact]
    public void PlacedItemReferenceReplacesDefault()
    {
        var defaults = Parse("""{"Item":{"$ref":"/Game/Invented/Other.Other_C"}}""");
        var placed = Parse("""{"Item":{"$ref":"/Game/Invented/Leaf.Leaf_C"}}""");
        var source = Assert.Single(
            new PickupObjectMechanism().FindSources(Context(defaults, placed))
        );
        Assert.Equal(Item, source.ItemClass);
        Assert.Equal(new Amount(3, 3), source.Source.Amount);
    }

    [Fact]
    public void LootItemConsumerUsesItemInfoRatherThanUnusedAmountSetting()
    {
        var defaults = Parse(
            """{"Item":{"$ref":"/Game/Invented/Leaf.Leaf_C"},"Amount of Items":9}"""
        );
        var source = Assert
            .Single(
                new PickupObjectMechanism().FindSources(
                    Context(defaults, ancestor: PickupObjectMechanism.LootItem)
                )
            )
            .Source;
        Assert.Equal(new Amount(3, 3), source.Amount);
    }

    [Fact]
    public void CollectorUsesConfirmedClassDefaultGrantProperty()
    {
        var defaults = Parse("""{"Cragstone Shard":{"$ref":"/Game/Invented/Leaf.Leaf_C"}}""");
        var source = Assert
            .Single(
                new PickupObjectMechanism().FindSources(
                    Context(
                        defaults,
                        ancestor: "/Game/BloodlinesAlpha/Interactables/CragstoneShard.CragstoneShard_C"
                    )
                )
            )
            .Source;
        Assert.Equal(new Amount(3, 3), source.Amount);
        Assert.Equal(Level, Assert.Single(source.Places).Id);
    }

    [Fact]
    public void CollectibleExplicitGrantCountIsNotItsItemInfoQuantity()
    {
        var defaults = Parse(
            """{"Petals":{"$ref":"/Game/Invented/Other.Other_C"},"Illumiscuttle":{"$ref":"/Game/Invented/Leaf.Leaf_C"}}"""
        );
        var source = Assert
            .Single(
                new PickupObjectMechanism().FindSources(
                    Context(
                        defaults,
                        ancestor: "/Game/BloodlinesAlpha/Interactables/BugInteractionObjects/Illumiscuttle_Pickup.Illumiscuttle_Pickup_C"
                    )
                )
            )
            .Source;
        Assert.Equal(new Amount(1, 1), source.Amount);
    }

    [Theory]
    [InlineData("""{"Item":null}""")]
    [InlineData(
        """{"Item":{"$ref":"/Game/Invented/Leaf.Leaf_C"},"Multiple Items?":true,"Amount of Items":0}"""
    )]
    [InlineData("""{"Preview Item":{"$ref":"/Game/Invented/Leaf.Leaf_C"}}""")]
    public void MissingItemsZeroAmountsAndUnconsumedReferencesDoNotGrant(string json) =>
        Assert.Empty(new PickupObjectMechanism().FindSources(Context(Parse(json))));

    [Fact]
    public void ExcludedPlacementsDoNotSupplyPlacesToDefaultSources()
    {
        var defaults = Parse("""{"Item":{"$ref":"/Game/Invented/Leaf.Leaf_C"}}""");
        var source = Assert
            .Single(new PickupObjectMechanism().FindSources(Context(defaults, excluded: true)))
            .Source;
        Assert.Empty(source.Places);
        Assert.True(SourceCoverage.HasUnknownPlace(source));
    }

    [Fact]
    public void UnplacedDefaultRetainsAnUnknownPlace()
    {
        var defaults = Parse("""{"Item":{"$ref":"/Game/Invented/Leaf.Leaf_C"}}""");
        Assert.Empty(
            Assert
                .Single(new PickupObjectMechanism().FindSources(Context(defaults, placed: false)))
                .Source.Places
        );
    }

    [Fact]
    public void SingleItemSwitchIgnoresTheMultipleItemCount()
    {
        var defaults = Parse(
            """{"Item":{"$ref":"/Game/Invented/Leaf.Leaf_C"},"Multiple Items?":false,"Amount of Items":9}"""
        );
        var source = Assert
            .Single(new PickupObjectMechanism().FindSources(Context(defaults)))
            .Source;
        Assert.Equal(new Amount(3, 3), source.Amount);
    }

    [Fact]
    public void UnknownMultipleItemSwitchRetainsTheSettingWithoutAnAggregateCount()
    {
        var defaults = Parse(
            """{"Item":{"$ref":"/Game/Invented/Leaf.Leaf_C"},"Amount of Items":9}"""
        );
        var source = Assert
            .Single(new PickupObjectMechanism().FindSources(Context(defaults)))
            .Source;
        Assert.Null(source.Amount);
        Assert.Contains(
            source.Notes,
            note => note.Contains("Amount of Items setting: 9.", StringComparison.Ordinal)
        );
    }
}
