using System.Text.Json.Nodes;
using Bloodline.Output;

namespace Bloodline.Tests.Output;

public sealed class CanonicalJsonTests
{
    [Fact]
    public void SortsObjectKeysOrdinallyAtEveryDepthAndKeepsArrayOrder()
    {
        var json = CanonicalJson.Serialize(
            JsonNode.Parse("""{"b": 1, "a": {"z": [3, 1, 2], "$text": "x", "Y": true}}""")
        );

        Assert.Equal(
            "{\n  \"a\": {\n    \"$text\": \"x\",\n    \"Y\": true,\n    \"z\": [\n      3,\n      1,\n      2\n    ]\n  },\n  \"b\": 1\n}\n",
            json
        );
    }

    [Fact]
    public void WritesSinglePrecisionValuesInTheirShortestForm()
    {
        var json = CanonicalJson.Serialize(
            new JsonArray(JsonValue.Create(0.1f), JsonValue.Create(1.2345678f))
        );

        Assert.Equal("[\n  0.1,\n  1.2345678\n]\n", json);
    }

    [Fact]
    public void KeepsNonAsciiTextReadable()
    {
        Assert.Equal("\"Tÿr's Ēdge\"\n", CanonicalJson.Serialize("Tÿr's Ēdge"));
    }
}
