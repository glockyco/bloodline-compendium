using System.Text.Json.Nodes;
using Bloodline.Records;

namespace Bloodline.Tests.Records;

public sealed class RecordMergeTests
{
    private static JsonObject Parse(string json) => JsonNode.Parse(json)!.AsObject();

    [Fact]
    public void KeepsInheritedValuesThatTheOverlayDoesNotSet()
    {
        var merged = RecordMerge.OverlayProperties(
            Parse("""{"Value": 10, "Name": {"$text": "Sword"}}"""),
            Parse("""{"Value": 12}""")
        );

        Assert.Equal(12, RecordValues.Integer(merged["Value"]));
        Assert.Equal("Sword", RecordValues.Text(merged["Name"]));
    }

    [Fact]
    public void MergesStructsFieldByField()
    {
        var merged = RecordMerge.OverlayProperties(
            Parse(
                """{"Info": {"Damage": 5, "Speed": {"$enum": "AttackSpeeds::NewEnumerator5"}}}"""
            ),
            Parse("""{"Info": {"Damage": 8}}""")
        );

        Assert.Equal(8, RecordValues.Integer(RecordValues.At(merged, "Info.Damage")));
        Assert.Equal(
            "AttackSpeeds::NewEnumerator5",
            RecordValues.EnumValue(RecordValues.At(merged, "Info.Speed"))
        );
    }

    [Fact]
    public void ReplacesArraysMapsAndTaggedValuesWhole()
    {
        var merged = RecordMerge.OverlayProperties(
            Parse(
                """{"Stock": [1, 2, 3], "Pool": {"$map": [["a", 1], ["b", 2]]}, "Name": {"$text": "Old", "key": "K"}}"""
            ),
            Parse("""{"Stock": [4], "Pool": {"$map": [["c", 3]]}, "Name": {"$text": "New"}}""")
        );

        Assert.Equal([4L], RecordValues.Elements(merged["Stock"]).Select(RecordValues.Integer));
        Assert.Single(RecordValues.MapEntries(merged["Pool"]));
        Assert.False(merged["Name"]!.AsObject().ContainsKey("key"));
    }

    [Fact]
    public void ExplicitEmptyValuesOverrideTheBase()
    {
        var merged = RecordMerge.OverlayProperties(
            Parse(
                """{"Count": 3, "Enabled": true, "Target": {"$ref": "/Game/A.A_C"}, "Stock": [1]}"""
            ),
            Parse("""{"Count": 0, "Enabled": false, "Target": null, "Stock": []}""")
        );

        Assert.Equal(0, RecordValues.Integer(merged["Count"]));
        Assert.False(RecordValues.Boolean(merged["Enabled"]));
        Assert.True(merged.ContainsKey("Target"));
        Assert.Null(merged["Target"]);
        Assert.Empty(RecordValues.Elements(merged["Stock"]));
    }

    [Fact]
    public void LeavesTheBaseUnchanged()
    {
        var baseProperties = Parse("""{"Info": {"Damage": 5}}""");

        RecordMerge.OverlayProperties(baseProperties, Parse("""{"Info": {"Damage": 8}}"""));

        Assert.Equal(5, RecordValues.Integer(RecordValues.At(baseProperties, "Info.Damage")));
    }
}
