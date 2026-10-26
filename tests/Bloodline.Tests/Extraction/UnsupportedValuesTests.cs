using System.Text.Json.Nodes;
using Bloodline.Extraction;

namespace Bloodline.Tests.Extraction;

public sealed class UnsupportedValuesTests
{
    [Fact]
    public void ReportsOnlyUnresolvedFinalValuesAfterDefaultsAreOverridden()
    {
        var merged = DefaultChain.Merge([
            new(
                Parse(
                    """{"Known":{"$unsupported":"NativeInitialValue"},"Unknown":{"$unsupported":"DelegateProperty"}}"""
                ),
                Parse("""{"Known":{"X":1,"Y":2}}""")
            ),
        ]);
        var gap = Assert.Single(UnsupportedValues.Find(merged));
        Assert.Equal("/Unknown", gap.Path);
        Assert.Equal("DelegateProperty", gap.Type);
    }

    [Fact]
    public void KeepsDistinctArrayLocationsAndEscapesJsonPointerSegments()
    {
        var values = Parse(
            """{"A/B~C":[{"$unsupported":"One"},{"nested":{"$unsupported":"Two"}}]}"""
        );
        Assert.Collection(
            UnsupportedValues.Find(values),
            first =>
            {
                Assert.Equal("/A~1B~0C/0", first.Path);
                Assert.Equal("One", first.Type);
            },
            second =>
            {
                Assert.Equal("/A~1B~0C/1/nested", second.Path);
                Assert.Equal("Two", second.Type);
            }
        );
    }

    private static JsonObject Parse(string json) => JsonNode.Parse(json)!.AsObject();
}
