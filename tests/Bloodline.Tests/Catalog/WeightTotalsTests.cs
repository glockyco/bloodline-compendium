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
    public void LootWeightsIncludeEmptyOutcomesWithoutFloatingPointNoise()
    {
        var records = new CatalogRecords(new BuildRecord(1, 1, []), [], [], [], [], [], [], []);
        var context = new CatalogContext(records, new Dictionary<string, ItemPage>(), []);
        var sources = LootRecords
            .Pool(context, Pool(), SourceKind.ContainerLoot, "Invented box", [], [])
            .ToArray();
        Assert.Equal(100, sources.Length);
        Assert.All(sources, source => Assert.Equal(new Weight(0.1, 18.02), source.Source.Weight));
        Assert.All(sources, source => Assert.Null(source.Source.Chance));
        Assert.All(sources, source => Assert.Null(source.Source.ChanceBasis));
        Assert.Equal(
            "A pick may give nothing. Empty outcomes are included when comparing the chances.",
            Assert.Single(sources[0].Source.Notes)
        );
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

    [Fact]
    public void ConfirmedSingleChoiceIncludesEmptyOutcomeWeightsInChance()
    {
        var records = new CatalogRecords(new BuildRecord(1, 1, []), [], [], [], [], [], [], []);
        var context = new CatalogContext(records, new Dictionary<string, ItemPage>(), []);
        var sources = LootRecords.Pool(
            context,
            Pool(),
            SourceKind.EnemyDrop,
            "Invented enemy",
            [],
            [],
            "Chance per kill"
        );
        Assert.All(
            sources,
            source =>
            {
                Assert.Equal(0.1 / 18.02, source.Source.Chance);
                Assert.Equal("Chance per kill", source.Source.ChanceBasis);
            }
        );
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
