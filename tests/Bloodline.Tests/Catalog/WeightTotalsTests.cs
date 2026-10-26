using System.Text.Json.Nodes;
using Bloodline.Catalog;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Tests.Catalog;

public sealed class WeightTotalsTests
{
    private static JsonObject Pool()
    {
        var entries = new JsonArray();
        for (var index = 0; index < 100; index++)
        {
            entries.Add(JsonNode.Parse("""[{"$ref":"/Invented/Item.Item_C"},0.1]"""));
        }
        entries.Add(JsonNode.Parse("""[null,8]"""));
        entries.Add(JsonNode.Parse("""[null,0.02]"""));
        return new JsonObject { ["$map"] = entries };
    }

    [Fact]
    public void WorldWeightsRetainStoredDecimalPrecisionAcrossManyEntries()
    {
        var weights = WorldSourceData.Pool(Pool()).Select(entry => entry.Weight).ToArray();
        Assert.Equal(100, weights.Length);
        Assert.All(weights, weight => Assert.Equal(new Weight(0.1, 18.02), weight));
    }

    [Fact]
    public void NativeAndParsedNumericValuesHaveTheSameDecimalTotal()
    {
        var native = new JsonNode?[]
        {
            JsonValue.Create(0.1),
            JsonValue.Create(0.2),
            JsonValue.Create(1),
        };
        Assert.Equal(1.3, WeightTotals.Sum(native));
        Assert.Equal(1.3, WeightTotals.Sum(JsonNode.Parse("[0.1,0.2,1]")!.AsArray()));
    }

    private static ItemSource[] Sources(PickCount? picks)
    {
        var records = new CatalogRecords(new BuildRecord(1, 1, []), [], [], [], [], [], [], []);
        var context = new CatalogContext(records, new Dictionary<string, ItemPage>(), []);
        return LootRecords
            .Pool(
                context,
                Pool(),
                SourceKind.EnemyDrop,
                "Invented enemy",
                [],
                [],
                new EntityReference(
                    EntityKind.Creature,
                    "/Invented/Enemy.Enemy_C",
                    "Invented enemy",
                    null,
                    null
                ),
                OddsEvent.Kill,
                picks,
                "Unknown picks."
            )
            .ToArray();
    }

    [Fact]
    public void OnePickChanceIncludesEmptyOutcomeWeights()
    {
        var sources = Sources(PickCount.Fixed(1));
        Assert.Equal(100, sources.Length);
        Assert.All(
            sources,
            source =>
                Assert.Equal(
                    new Odds(OddsEvent.Kill, 0.1 / 18.02, 0.1 / 18.02, null),
                    source.Source.Odds
                )
        );
    }

    [Fact]
    public void UnknownPickCountLeavesTheChanceUnknown()
    {
        Assert.All(
            Sources(null),
            source =>
                Assert.Equal(
                    new Odds(OddsEvent.Kill, null, null, "Unknown picks."),
                    source.Source.Odds
                )
        );
    }

    [Theory]
    [InlineData(4, 4, new[] { 5 }, new[] { 1.0 })]
    [InlineData(0, 0, new[] { 1 }, new[] { 1.0 })]
    [InlineData(2, 3, new[] { 3, 4 }, new[] { 0.5, 0.5 })]
    public void ContainersPickOnceMoreThanTheirDrawnBound(
        int minimum,
        int maximum,
        int[] picks,
        double[] probabilities
    )
    {
        var counts = PickCount.Container(minimum, maximum).Counts;
        Assert.Equal(picks, counts.Select(count => count.Picks));
        Assert.Equal(probabilities, counts.Select(count => count.Probability));
    }

    [Fact]
    public void ContainerBoundsAreDrawnAgainOnEveryPass()
    {
        // Bounds 3 and 11 always make 4 picks, and a fifth only when the fresh draw reaches 4, 8 times in 9.
        var counts = PickCount.Container(3, 11).Counts;
        Assert.Equal((4, 1.0 / 9), (counts[0].Picks, counts[0].Probability), new Close());
        Assert.Equal(12, counts[^1].Picks);
        Assert.Equal(1, counts.Sum(count => count.Probability), 12);
    }

    [Fact]
    public void ChanceOfAtLeastOneCopyCombinesEveryPick()
    {
        var odds = LootOdds.Of(OddsEvent.Container, 0.5, PickCount.Container(1, 1));
        Assert.Equal(new Odds(OddsEvent.Container, 0.75, 1, null), odds);
    }

    private sealed class Close : IEqualityComparer<(int, double)>
    {
        public bool Equals((int, double) x, (int, double) y) =>
            x.Item1 == y.Item1 && Math.Abs(x.Item2 - y.Item2) < 1e-12;

        public int GetHashCode((int, double) obj) => obj.Item1;
    }

    [Theory]
    [InlineData(2, 8, 0.25)]
    [InlineData(2, 2, 1.0)]
    [InlineData(2, 0, null)]
    [InlineData(2, -1, null)]
    [InlineData(2, 1, null)]
    public void ChanceRequiresAUsableSingleChoiceDenominator(
        double value,
        double total,
        double? expected
    )
    {
        Assert.Equal(expected, WeightTotals.Chance(new Weight(value, total)));
    }
}
