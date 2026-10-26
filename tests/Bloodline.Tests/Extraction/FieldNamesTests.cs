using Bloodline.Extraction;

namespace Bloodline.Tests.Extraction;

public sealed class FieldNamesTests
{
    private const string FirstGuid = "0123456789ABCDEF0123456789ABCDEF";
    private const string SecondGuid = "FEDCBA9876543210FEDCBA9876543210";

    [Theory]
    [InlineData("Display_Name_12_0123456789ABCDEF0123456789ABCDEF", "Display_Name")]
    [InlineData("Display_0_0123456789abcdef0123456789abcdef", "Display")]
    [InlineData("Display_12", "Display_12")]
    [InlineData(
        "Display_x_0123456789ABCDEF0123456789ABCDEF",
        "Display_x_0123456789ABCDEF0123456789ABCDEF"
    )]
    [InlineData(
        "Display_1_0123456789ABCDEF0123456789ABCDEG",
        "Display_1_0123456789ABCDEF0123456789ABCDEG"
    )]
    [InlineData("_0123456789ABCDEF0123456789ABCDEF", "_0123456789ABCDEF0123456789ABCDEF")]
    [InlineData("_1_0123456789ABCDEF0123456789ABCDEF", "_1_0123456789ABCDEF0123456789ABCDEF")]
    [InlineData("", "")]
    public void StripsOnlyCompleteGeneratedSuffixes(string name, string expected)
    {
        Assert.Equal(expected, FieldNames.StripSuffix(name));
    }

    [Fact]
    public void KeepsBothOriginalNamesWhenStrippedNamesCollide()
    {
        var first = $"Damage_1_{FirstGuid}";
        var second = $"Damage_2_{SecondGuid}";

        var names = FieldNames.Resolve([first, second, $"Speed_3_{FirstGuid}"]);

        Assert.Equal(first, names[first]);
        Assert.Equal(second, names[second]);
        Assert.Equal("Speed", names[$"Speed_3_{FirstGuid}"]);
    }

    [Fact]
    public void AccountsForUnsuffixedNamesAndDuplicateDeclarations()
    {
        var generated = $"Name_1_{FirstGuid}";

        var names = FieldNames.Resolve(["Name", generated, generated, $"Count_2_{FirstGuid}"]);

        Assert.Equal("Name", names["Name"]);
        Assert.Equal(generated, names[generated]);
        Assert.Equal("Count", names[$"Count_2_{FirstGuid}"]);
    }

    [Fact]
    public void ResolvesCollisionsIntroducedByKeepingOriginalNames()
    {
        var first = $"Name_1_{FirstGuid}";
        var second = $"Name_2_{SecondGuid}";
        var nested = $"{first}_3_{SecondGuid}";

        var names = FieldNames.Resolve([first, second, nested]);

        Assert.Equal(first, names[first]);
        Assert.Equal(second, names[second]);
        Assert.Equal(nested, names[nested]);
        Assert.Equal(names.Count, names.Values.Distinct(StringComparer.Ordinal).Count());
    }
}
