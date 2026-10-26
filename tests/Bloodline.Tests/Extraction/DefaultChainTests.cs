using System.Text.Json.Nodes;
using Bloodline.Extraction;

namespace Bloodline.Tests.Extraction;

public sealed class DefaultChainTests
{
    [Fact]
    public void MergesBlueprintAncestorsWithoutInventingNativeFields()
    {
        var result = DefaultChain.Merge([
            new(
                Parse("""{"Count":0,"Info":{"Damage":0,"Name":""},"Pool":[]}"""),
                Parse("""{"Count":9,"Info":{"Damage":12,"Name":"Sword"},"Pool":[1,2]}""")
            ),
            new(Parse("""{"OwnFlag":false}"""), Parse("""{"Info":{"Damage":17},"Pool":[3]}""")),
        ]);
        Assert.Equal(9, result["Count"]!.GetValue<int>());
        Assert.Equal(17, result["Info"]!["Damage"]!.GetValue<int>());
        Assert.Equal("Sword", result["Info"]!["Name"]!.GetValue<string>());
        Assert.Equal("[3]", result["Pool"]!.ToJsonString());
        Assert.False(result["OwnFlag"]!.GetValue<bool>());
        Assert.False(result.ContainsKey("NativeHealth"));
    }

    [Fact]
    public void KeepsExplicitEmptyAndNullOverrides()
    {
        var result = DefaultChain.Merge([
            new(
                new(),
                Parse(
                    """{"Count":9,"Flag":true,"Name":"Sword","Pool":[1],"Object":{"$ref":"/Game/A.A"}}"""
                )
            ),
            new(new(), Parse("""{"Count":0,"Flag":false,"Name":"","Pool":[],"Object":null}""")),
        ]);
        Assert.Equal(0, result["Count"]!.GetValue<int>());
        Assert.False(result["Flag"]!.GetValue<bool>());
        Assert.Equal("", result["Name"]!.GetValue<string>());
        Assert.Empty(result["Pool"]!.AsArray());
        Assert.True(result.ContainsKey("Object"));
        Assert.Null(result["Object"]);
    }

    [Fact]
    public void StructDefaultsFillFieldsBeforeClassOverrides()
    {
        var result = DefaultChain.Merge([
            new(
                Parse("""{"Info":{"Damage":4,"Name":"Unnamed","Extra":{"Weight":2}}}"""),
                Parse("""{"Info":{"Name":"Sword","Extra":{"Value":8}}}""")
            ),
        ]);
        Assert.Equal(4, result["Info"]!["Damage"]!.GetValue<int>());
        Assert.Equal(2, result["Info"]!["Extra"]!["Weight"]!.GetValue<int>());
        Assert.Equal(8, result["Info"]!["Extra"]!["Value"]!.GetValue<int>());
    }

    [Fact]
    public void PlacementOnlyCarriesOverridesButCompletesOverriddenStructs()
    {
        var result = DefaultChain.Placement(
            Parse("""{"Info":{"Damage":4,"Name":"Sword"},"Pool":[1],"Unchanged":7}"""),
            Parse("""{"Info":{"Damage":8},"Pool":[]}""")
        );
        Assert.Equal("Sword", result["Info"]!["Name"]!.GetValue<string>());
        Assert.Equal(8, result["Info"]!["Damage"]!.GetValue<int>());
        Assert.Empty(result["Pool"]!.AsArray());
        Assert.False(result.ContainsKey("Unchanged"));
    }

    [Fact]
    public void FailingObjectLeavesNoPartialValueAndRecordsItsGap()
    {
        var gaps = new GapCollector("/private/install");
        var succeeded = gaps.TryRead<JsonObject>(
            "/Game/A",
            "Broken",
            () =>
            {
                _ = Parse("""{"Count":3}""");
                throw new InvalidDataException("Failed /private/install/container");
            },
            out var result
        );
        Assert.False(succeeded);
        Assert.Null(result);
        var gap = Assert.Single(gaps.Records);
        Assert.Equal("/Game/A", gap.Package);
        Assert.Equal("Broken", gap.Object);
        Assert.DoesNotContain("/private/install", gap.Error, StringComparison.Ordinal);
    }

    private static JsonObject Parse(string json) => JsonNode.Parse(json)!.AsObject();
}
