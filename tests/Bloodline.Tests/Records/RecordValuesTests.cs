using System.Text.Json.Nodes;
using Bloodline.Records;

namespace Bloodline.Tests.Records;

public sealed class RecordValuesTests
{
    [Fact]
    public void ReadsNumbersBuiltInCodeAsWellAsParsedOnes()
    {
        Assert.Equal(3, RecordValues.Integer(JsonValue.Create(3)));
        Assert.Equal(3, RecordValues.Integer(JsonValue.Create((byte)3)));
        Assert.Equal(0.1, RecordValues.Number(JsonValue.Create(0.1f)));
        Assert.Equal(2.5, RecordValues.Number(JsonNode.Parse("2.5")));
    }

    [Fact]
    public void RejectsFractionsAsIntegers()
    {
        Assert.Null(RecordValues.Integer(JsonValue.Create(2.5)));
    }
}
