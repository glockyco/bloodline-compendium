using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
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
        var numbers = Regex
            .Matches(Assert.Single(sources[0].Source.Notes), @"\d+(?:\.\d+)?")
            .Select(match => double.Parse(match.Value, CultureInfo.InvariantCulture));
        Assert.Equal([8.02], numbers);
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
}
