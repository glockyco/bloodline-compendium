using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using Bloodline.Extraction;
using Bloodline.Records;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;

namespace Bloodline.Tests.Extraction;

public sealed class TextEncoderTests
{
    [Fact]
    public void FormatsNamedArgumentsUsingSourcesAndRetainsTheirIdentities()
    {
        var text = Text<FTextHistory.NamedFormat>(
            ETextHistoryType.NamedFormat,
            (
                "SourceFmt",
                new FText("Names", "CaptainKey", "Captain {Name}, level {Level}", "Translation")
            ),
            (
                "Arguments",
                new Dictionary<string, FFormatArgumentValue>
                {
                    ["Name"] = Argument(
                        EFormatArgumentType.Text,
                        new FText("Names", "NameKey", "Ayla", "Wrong")
                    ),
                    ["Level"] = Argument(EFormatArgumentType.Float, 1.5F),
                }
            )
        );
        var encoded = TextEncoder.Encode(text);
        Assert.Equal("Captain Ayla, level 1.5", RecordValues.Text(encoded));
        Assert.Equal("Captain {Name}, level {Level}", RecordValues.String(encoded["format"]));
        Assert.Equal("CaptainKey", RecordValues.String(encoded["key"]));
        Assert.Equal("NameKey", RecordValues.String(encoded["arguments"]!["Name"]!["key"]));
        Assert.Equal("1.5", encoded["arguments"]!["Level"]!.ToJsonString());
    }

    [Fact]
    public void FormatsArgumentDataAndOrderedArguments()
    {
        var data = Text<FTextHistory.ArgumentFormat>(
            ETextHistoryType.ArgumentFormat,
            ("SourceFmt", new FText("{First} {Last}")),
            (
                "Arguments",
                new[]
                {
                    Create<FFormatArgumentData>(
                        ("ArgumentName", "First"),
                        ("ArgumentValue", Argument(EFormatArgumentType.Text, new FText("Rich")))
                    ),
                    Create<FFormatArgumentData>(
                        ("ArgumentName", "Last"),
                        ("ArgumentValue", Argument(EFormatArgumentType.Text, new FText("Memford")))
                    ),
                }
            )
        );
        var ordered = Text<FTextHistory.OrderedFormat>(
            ETextHistoryType.OrderedFormat,
            ("SourceFmt", new FText("{0}: {1}")),
            (
                "Arguments",
                new[]
                {
                    Argument(EFormatArgumentType.Int, 42L),
                    Argument(EFormatArgumentType.Float, 0.1F),
                }
            )
        );
        Assert.Equal("Rich Memford", RecordValues.Text(TextEncoder.Encode(data)));
        var encoded = TextEncoder.Encode(ordered);
        Assert.Equal("42: 0.1", RecordValues.Text(encoded));
        Assert.Equal(42, RecordValues.Integer(encoded["arguments"]!["0"]));
    }

    [Fact]
    public void LeavesUnresolvableArgumentsVisibleAndDoesNotReformatInsertedText()
    {
        var text = Text<FTextHistory.NamedFormat>(
            ETextHistoryType.NamedFormat,
            ("SourceFmt", new FText("`{Literal`} {First} {Unknown} {Missing}")),
            (
                "Arguments",
                new Dictionary<string, FFormatArgumentValue>
                {
                    ["First"] = Argument(EFormatArgumentType.Text, new FText("{Missing}")),
                    ["Unknown"] = Argument(EFormatArgumentType.Gender, new object()),
                }
            )
        );
        var encoded = TextEncoder.Encode(text);
        Assert.Equal("{Literal} {Missing} {Unknown} {Missing}", RecordValues.Text(encoded));
        Assert.Equal(
            "TextArgument.Gender",
            RecordValues.String(encoded["arguments"]!["Unknown"]![RecordFormat.Unsupported])
        );
    }

    [Theory]
    [InlineData(ETextHistoryType.AsNumber, 0.125, "0.125")]
    [InlineData(ETextHistoryType.AsPercent, 0.125, "12.5%")]
    [InlineData(ETextHistoryType.AsCurrency, 19.5, "USD 19.5")]
    public void FormatsScalarHistoriesInvariantly(
        ETextHistoryType history,
        double number,
        string expected
    )
    {
        var text = Text<FTextHistory.FormatNumber>(
            history,
            ("SourceValue", Argument(EFormatArgumentType.Double, number)),
            ("CurrencyCode", "USD")
        );
        var encoded = TextEncoder.Encode(text);
        Assert.Equal(expected, RecordValues.Text(encoded));
        Assert.Equal(number, RecordValues.Number(encoded["arguments"]!["value"]));
    }

    [Fact]
    public void AppliesAuthoredNumberRoundingAndMinimumDigits()
    {
        var text = Text<FTextHistory.FormatNumber>(
            ETextHistoryType.AsNumber,
            ("SourceValue", Argument(EFormatArgumentType.Double, 1.125)),
            (
                "FormatOptions",
                new FNumberFormattingOptions
                {
                    AlwaysSign = true,
                    UseGrouping = false,
                    MinimumIntegralDigits = 2,
                    MinimumFractionalDigits = 2,
                    MaximumFractionalDigits = 2,
                    RoundingMode = ERoundingMode.HalfFromZero,
                }
            )
        );
        Assert.Equal("+01.13", RecordValues.Text(TextEncoder.Encode(text)));
    }

    [Theory]
    [InlineData(ETransformType.ToLower, "mixed name")]
    [InlineData(ETransformType.ToUpper, "MIXED NAME")]
    public void AppliesCaseTransformsToTheSource(ETransformType transform, string expected)
    {
        var text = Text<FTextHistory.Transform>(
            ETextHistoryType.Transform,
            ("SourceText", new FText("Mixed Name", "Translation")),
            ("TransformType", transform)
        );
        Assert.Equal(expected, RecordValues.Text(TextEncoder.Encode(text)));
    }

    [Fact]
    public void RendersStoredDatesWithoutMachineTimeZoneOrCulture()
    {
        var ticks = new DateTime(2026, 10, 26, 14, 35, 9).Ticks;
        var date = MemoryMarshal.Read<FDateTime>(BitConverter.GetBytes(ticks));
        var day = Text<FTextHistory.AsDate>(
            ETextHistoryType.AsDate,
            ("SourceDateTime", date),
            ("DateStyle", EDateTimeStyle.Long),
            ("TimeZone", "UTC")
        );
        var time = Text<FTextHistory.AsTime>(
            ETextHistoryType.AsTime,
            ("SourceDateTime", date),
            ("TimeStyle", EDateTimeStyle.Short),
            ("TimeZone", "UTC")
        );
        var both = Text<FTextHistory.AsDateTime>(
            ETextHistoryType.AsDateTime,
            ("SourceDateTime", date),
            ("DateStyle", EDateTimeStyle.Full),
            ("TimeStyle", EDateTimeStyle.Long)
        );
        Assert.Equal("2026-10-26", RecordValues.Text(TextEncoder.Encode(day)));
        Assert.Equal("14:35:09", RecordValues.Text(TextEncoder.Encode(time)));
        var encoded = TextEncoder.Encode(both);
        Assert.Equal("2026-10-26 14:35:09", RecordValues.Text(encoded));
        Assert.Equal(ticks, RecordValues.Integer(encoded["arguments"]!["value"]));
    }

    [Theory]
    [InlineData("Ancient Gate", "Ancient Gate")]
    [InlineData(null, "{InventedTable:Gate}")]
    public void KeepsStringTableSourcesAndUnresolvedKeys(string? source, string expected)
    {
        var text = Text<FTextHistory.StringTableEntry>(
            ETextHistoryType.StringTableEntry,
            ("SourceString", source),
            ("TableId", new FName("InventedTable")),
            ("Key", "Gate"),
            ("LocalizedString", "Translation")
        );
        var encoded = TextEncoder.Encode(text);
        Assert.Equal(expected, RecordValues.Text(encoded));
        Assert.Equal("InventedTable", RecordValues.String(encoded["table"]));
        Assert.Equal("Gate", RecordValues.String(encoded["key"]));
    }

    [Fact]
    public void LeavesDelegateBindingsOutWithoutDiagnostics()
    {
        var diagnostics = new List<string>();
        var encoder = new PropertyEncoder((type, _) => diagnostics.Add(type));
        var properties = encoder.Properties(
            new[]
            {
                new FPropertyTag { Name = "Plain", PropertyType = "DelegateProperty" },
                new FPropertyTag { Name = "Multi", PropertyType = "MulticastDelegateProperty" },
                new FPropertyTag
                {
                    Name = "Inline",
                    PropertyType = "MulticastInlineDelegateProperty",
                },
                new FPropertyTag
                {
                    Name = "Sparse",
                    PropertyType = "MulticastSparseDelegateProperty",
                },
            }
        );
        var defaults = encoder.StructDefaults(
            new UUserDefinedStruct
            {
                Name = "InventedBindings",
                ChildProperties =
                [
                    new FDelegateProperty { Name = "Plain" },
                    new FMulticastDelegateProperty { Name = "Multi" },
                    new FMulticastInlineDelegateProperty { Name = "Inline" },
                ],
            }
        );
        Assert.Empty(properties);
        Assert.Empty(defaults);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void FormatsEnumDisplayTextsThroughTheSameSourceHistoryEncoder()
    {
        var display = Text<FTextHistory.NamedFormat>(
            ETextHistoryType.NamedFormat,
            ("SourceFmt", new FText("High {Title}")),
            (
                "Arguments",
                new Dictionary<string, FFormatArgumentValue>
                {
                    ["Title"] = Argument(
                        EFormatArgumentType.Text,
                        new FText("King", "Translation")
                    ),
                }
            )
        );
        var enumeration = new UUserDefinedEnum
        {
            Name = "E_Invented",
            Names = [(new FName("E_Invented::NewEnumerator0"), 0)],
        };
        enumeration.Properties.Add(
            new FPropertyTag
            {
                Name = "DisplayNameMap",
                PropertyType = "MapProperty",
                Tag = new CUE4Parse.UE4.Assets.Objects.Properties.MapProperty(
                    new UScriptMap(
                        new Dictionary<
                            CUE4Parse.UE4.Assets.Objects.Properties.FPropertyTagType,
                            CUE4Parse.UE4.Assets.Objects.Properties.FPropertyTagType?
                        >
                        {
                            [
                                new CUE4Parse.UE4.Assets.Objects.Properties.NameProperty(
                                    "NewEnumerator0"
                                )
                            ] = new CUE4Parse.UE4.Assets.Objects.Properties.TextProperty(display),
                        }
                    )
                ),
            }
        );
        Assert.Equal(
            "High King",
            Assert.Single(new PropertyEncoder().EnumEntries(enumeration)).Display
        );
    }

    private static FText Text<T>(
        ETextHistoryType type,
        params (string Name, object? Value)[] fields
    )
        where T : FTextHistory => new(0, type, Create<T>(fields));

    private static FFormatArgumentValue Argument(EFormatArgumentType type, object value) =>
        Create<FFormatArgumentValue>(("Type", type), ("Value", value));

    private static T Create<T>(params (string Name, object? Value)[] fields)
        where T : class
    {
        // CUE4Parse's history constructors require archives. These fixtures supply their decoded invented values.
        var value = (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
        foreach (var (name, field) in fields)
            typeof(T)
                .GetField(name, BindingFlags.Instance | BindingFlags.Public)!
                .SetValue(value, field);
        return value;
    }
}
