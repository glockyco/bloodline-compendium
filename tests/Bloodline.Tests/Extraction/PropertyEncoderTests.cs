using System.Text.Json.Nodes;
using Bloodline.Extraction;
using Bloodline.Records;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;

namespace Bloodline.Tests.Extraction;

public sealed class PropertyEncoderTests
{
    private const string Guid = "0123456789ABCDEF0123456789ABCDEF";
    private readonly PropertyEncoder encoder = new();

    [Fact]
    public void KeepsNumericWidthsAndSinglePrecisionSerialization()
    {
        Assert.Equal(
            sbyte.MinValue,
            encoder.Value(new Int8Property(sbyte.MinValue))!.GetValue<sbyte>()
        );
        Assert.Equal(
            short.MinValue,
            encoder.Value(new Int16Property(short.MinValue))!.GetValue<short>()
        );
        Assert.Equal(int.MinValue, encoder.Value(new IntProperty(int.MinValue))!.GetValue<int>());
        Assert.Equal(
            long.MinValue,
            encoder.Value(new Int64Property(long.MinValue))!.GetValue<long>()
        );
        Assert.Equal(
            byte.MaxValue,
            encoder.Value(new ByteProperty(byte.MaxValue))!.GetValue<byte>()
        );
        Assert.Equal(
            ushort.MaxValue,
            encoder.Value(new UInt16Property(ushort.MaxValue))!.GetValue<ushort>()
        );
        Assert.Equal(
            uint.MaxValue,
            encoder.Value(new UInt32Property(uint.MaxValue))!.GetValue<uint>()
        );
        Assert.Equal(
            ulong.MaxValue,
            encoder.Value(new UInt64Property(ulong.MaxValue))!.GetValue<ulong>()
        );
        Assert.Equal("0.1", encoder.Value(new FloatProperty(0.1F))!.ToJsonString());
        Assert.Equal(
            0.123456789012345D,
            encoder.Value(new DoubleProperty(0.123456789012345D))!.GetValue<double>()
        );
    }

    [Fact]
    public void KeepsTextSourceNamespaceAndKeyRatherThanLocalizedPresentation()
    {
        var value = encoder.Value(
            new TextProperty(new FText("InventedNamespace", "InventedKey", "Source", "Translation"))
        );

        Assert.Equal("Source", RecordValues.Text(value));
        Assert.Equal("InventedNamespace", RecordValues.String(value![RecordFormat.TextNamespace]));
        Assert.Equal("InventedKey", RecordValues.String(value[RecordFormat.TextKey]));
    }

    [Fact]
    public void KeepsCultureInvariantText()
    {
        var value = encoder.Value(
            new TextProperty(new FText(0, ETextHistoryType.None, new FTextHistory.None()))
        );

        Assert.Equal("", RecordValues.Text(value));
        Assert.False(value!.AsObject().ContainsKey(RecordFormat.TextNamespace));
    }

    [Fact]
    public void UsesFullOuterPathForHardReferencesAndPreservesSoftSubpaths()
    {
        var container = new UObject { Name = "Container" };
        var inner = new UObject { Name = "Inner", Outer = new ResolvedLoadedObject(container) };
        var package = new FixturePackage(container, inner);

        var reference = encoder.Value(new ObjectProperty(new FPackageIndex(package, 2)));
        var soft = encoder.Value(
            new SoftObjectProperty(new FSoftObjectPath("/Game/Invented.Container", "Inner"))
        );

        Assert.Equal("/Game/Invented.Container:Inner", RecordValues.Reference(reference));
        Assert.Equal("/Game/Invented.Container:Inner", RecordValues.SoftReference(soft));
        Assert.Null(encoder.Value(new ObjectProperty(new FPackageIndex())));
        Assert.Null(encoder.Value(new SoftObjectProperty(new FSoftObjectPath())));
    }

    [Fact]
    public void MatchesEnumDisplayNamesByNameRatherThanPositionOrNumericValue()
    {
        var enumeration = InventedEnum();

        var value = encoder.Value(
            new EnumProperty("E_Invented::NewEnumerator8"),
            new FPropertyTagData { Enum = enumeration }
        );
        var entries = encoder.EnumEntries(enumeration);

        Assert.Equal("Second choice", RecordValues.EnumDisplay(value));
        Assert.Equal("First choice", entries[0].Display);
        Assert.Equal("Second choice", entries[1].Display);
        Assert.Equal(42, entries[0].Value);
        Assert.Equal(0, entries[1].Value);
    }

    [Fact]
    public void ResolvesEnumMetadataFromNamesWhenTaggedMetadataHasNoLoadedEnum()
    {
        var enumeration = InventedEnum();
        var resolving = new PropertyEncoder(resolveType: name =>
            name == "E_Invented" ? enumeration : null
        );

        var value = resolving.Value(new EnumProperty("E_Invented::NewEnumerator8"));

        Assert.Equal("Second choice", RecordValues.EnumDisplay(value));
    }

    [Fact]
    public void EncodesDecodedNativeStructFieldsWithoutComputedProperties()
    {
        var value = encoder
            .Value(new StructProperty(new FScriptStruct(new FVector(0.1F, 2F, 3F))))!
            .AsObject();

        Assert.Equal(3, value.Count);
        Assert.Equal("0.1", value["X"]!.ToJsonString());
        Assert.Equal(2F, value["Y"]!.GetValue<float>());
        Assert.Equal(3F, value["Z"]!.GetValue<float>());
    }

    [Fact]
    public void EncodesArraysSetsAndOrderedMapEntries()
    {
        var array = encoder.Value(
            new ArrayProperty(
                new UScriptArray([new IntProperty(2), new IntProperty(1)], "IntProperty")
            )
        );
        var set = encoder.Value(
            new SetProperty(new UScriptSet([new NameProperty("B"), new NameProperty("A")]))
        );
        var map = encoder.Value(
            new MapProperty(
                new UScriptMap(
                    new Dictionary<FPropertyTagType, FPropertyTagType?>
                    {
                        [new NameProperty("B")] = new IntProperty(2),
                        [new NameProperty("A")] = null,
                    }
                )
            )
        );

        Assert.Equal([2L, 1L], RecordValues.Elements(array).Select(RecordValues.Integer));
        Assert.Equal(["B", "A"], RecordValues.Elements(set).Select(RecordValues.String));
        var entries = RecordValues.MapEntries(map).ToArray();
        Assert.Equal("B", RecordValues.String(entries[0].Key));
        Assert.Equal(2, RecordValues.Integer(entries[0].Value));
        Assert.Equal("A", RecordValues.String(entries[1].Key));
        Assert.Null(entries[1].Value);
    }

    [Fact]
    public void InitializesOnlyDeclaredBlueprintFieldsAndKeepsNativeDefaultsUnknown()
    {
        var declaration = new UUserDefinedStruct
        {
            Name = "InventedStruct",
            ChildProperties =
            [
                new FIntProperty { Name = $"Count_1_{Guid}" },
                new FBoolProperty { Name = $"Enabled_2_{Guid}" },
                new FArrayProperty { Name = $"Values_3_{Guid}" },
                new FObjectProperty { Name = $"Target_4_{Guid}" },
            ],
            DefaultProperties = [Tag($"Count_1_{Guid}", new IntProperty(7))],
        };

        var defaults = encoder.StructDefaults(declaration);

        Assert.Equal(7, RecordValues.Integer(defaults["Count"]));
        Assert.False(RecordValues.Boolean(defaults["Enabled"]));
        Assert.Empty(RecordValues.Elements(defaults["Values"]));
        Assert.True(defaults.ContainsKey("Target"));
        Assert.Null(defaults["Target"]);
        Assert.False(defaults.ContainsKey("NativeOnly"));
        Assert.Empty(
            encoder.StructDefaults(
                new UStruct
                {
                    Name = "NativeStruct",
                    ChildProperties = [new FIntProperty { Name = "NativeOnly" }],
                }
            )
        );
    }

    [Fact]
    public void ZeroEnumUsesTheMemberWhoseStoredValueIsZero()
    {
        var enumeration = InventedEnum();
        var package = new FixturePackage(enumeration);

        var value = encoder.Zero(new FEnumProperty { Enum = new FPackageIndex(package, 1) });
        var byteValue = encoder.Zero(new FByteProperty { Enum = new FPackageIndex(package, 1) });

        Assert.Equal("E_Invented::NewEnumerator8", RecordValues.EnumValue(value));
        Assert.Equal("Second choice", RecordValues.EnumDisplay(value));
        Assert.True(JsonNode.DeepEquals(value, byteValue));
    }

    [Theory]
    [InlineData("Vector", "X")]
    [InlineData("Rotator", "Yaw")]
    [InlineData("Color", "R")]
    [InlineData("Guid", "A")]
    public void InitializesKnownNativeStructsDeclaredOnBlueprints(string name, string component)
    {
        var package = new FixturePackage(new UStruct { Name = name });

        var value = encoder.Zero(new FStructProperty { Struct = new FPackageIndex(package, 1) });

        Assert.Equal(0, RecordValues.Number(value![component]));
    }

    [Fact]
    public void InitializesTransformIdentityAndSoftReferences()
    {
        var package = new FixturePackage(new UStruct { Name = "Transform" });

        var value = encoder.Zero(new FStructProperty { Struct = new FPackageIndex(package, 1) });

        Assert.Equal(1, RecordValues.Number(RecordValues.At(value, "Rotation.W")));
        Assert.Equal(1, RecordValues.Number(RecordValues.At(value, "Scale3D.X")));
        Assert.Equal(0, RecordValues.Number(RecordValues.At(value, "Translation.X")));
        Assert.Null(encoder.Zero(new FSoftObjectProperty()));
        Assert.Null(encoder.Zero(new FSoftClassProperty()));
    }

    [Fact]
    public void MergesStructDefaultsThenParentAndChildOverridesWithoutResettingInheritedFields()
    {
        var structure = new UUserDefinedStruct
        {
            Name = "InventedInfo",
            ChildProperties =
            [
                new FIntProperty { Name = $"Damage_1_{Guid}" },
                new FIntProperty { Name = $"Weight_2_{Guid}" },
                new FBoolProperty { Name = $"Enabled_3_{Guid}" },
            ],
            DefaultProperties =
            [
                Tag($"Damage_1_{Guid}", new IntProperty(3)),
                Tag($"Weight_2_{Guid}", new IntProperty(4)),
                Tag($"Enabled_3_{Guid}", new BoolProperty(true)),
            ],
        };
        var package = new FixturePackage(structure);
        var initial = encoder.Zero(new FStructProperty { Struct = new FPackageIndex(package, 1) });
        var metadata = new FPropertyTagData { Struct = structure, StructType = structure.Name };
        var parent = encoder.Value(Struct(Tag($"Damage_1_{Guid}", new IntProperty(8))), metadata);
        var child = encoder.Value(
            Struct(
                Tag($"Weight_2_{Guid}", new IntProperty(0)),
                Tag($"Enabled_3_{Guid}", new BoolProperty(false))
            ),
            metadata
        );

        var merged = RecordMerge.Overlay(RecordMerge.Overlay(initial, parent), child);

        Assert.Equal(8, RecordValues.Integer(merged!["Damage"]));
        Assert.Equal(0, RecordValues.Integer(merged["Weight"]));
        Assert.False(RecordValues.Boolean(merged["Enabled"]));
        Assert.False(parent!.AsObject().ContainsKey("Weight"));
        Assert.False(child!.AsObject().ContainsKey("Damage"));
    }

    [Fact]
    public void MergesStoredNestedDefaultsFieldByFieldOverDeclaredInitialValues()
    {
        var nested = new UUserDefinedStruct
        {
            Name = "InventedNested",
            ChildProperties =
            [
                new FIntProperty { Name = "Count" },
                new FIntProperty { Name = "Weight" },
            ],
            DefaultProperties =
            [
                Tag("Count", new IntProperty(3)),
                Tag("Weight", new IntProperty(4)),
            ],
        };
        var package = new FixturePackage(nested);
        var outer = new UUserDefinedStruct
        {
            Name = "InventedOuter",
            ChildProperties =
            [
                new FStructProperty { Name = "Info", Struct = new FPackageIndex(package, 1) },
            ],
            DefaultProperties =
            [
                Tag(
                    "Info",
                    Struct(Tag("Count", new IntProperty(0))),
                    new FPropertyTagData { Struct = nested }
                ),
            ],
        };

        var value = encoder.StructDefaults(outer);

        Assert.Equal(0, RecordValues.Integer(RecordValues.At(value, "Info.Count")));
        Assert.Equal(4, RecordValues.Integer(RecordValues.At(value, "Info.Weight")));
    }

    [Fact]
    public void FillsStructDefaultsInsideReplacementContainers()
    {
        var structure = new UUserDefinedStruct
        {
            Name = "InventedElement",
            ChildProperties =
            [
                new FIntProperty { Name = "Count" },
                new FIntProperty { Name = "Weight" },
            ],
            DefaultProperties =
            [
                Tag("Count", new IntProperty(3)),
                Tag("Weight", new IntProperty(4)),
            ],
        };
        var array = new ArrayProperty(
            new UScriptArray(
                [Struct(Tag("Count", new IntProperty(0)))],
                "StructProperty",
                new FPropertyTagData { Struct = structure }
            )
        );

        var element = Assert.Single(RecordValues.Elements(encoder.Value(array)));

        Assert.Equal(0, RecordValues.Integer(element!["Count"]));
        Assert.Equal(4, RecordValues.Integer(element["Weight"]));
    }

    [Fact]
    public void UsesDeclaredCollisionScopeEvenWhenAnOverrideContainsOnlyOneField()
    {
        var first = $"Value_1_{Guid}";
        var second = $"Value_2_{Guid}";
        var declaration = new UUserDefinedStruct
        {
            Name = "InventedCollision",
            ChildProperties =
            [
                new FIntProperty { Name = first },
                new FIntProperty { Name = second },
            ],
        };

        var value = encoder.Value(
            Struct(Tag(first, new IntProperty(9))),
            new FPropertyTagData { Struct = declaration }
        );

        Assert.True(value!.AsObject().ContainsKey(first));
        Assert.False(value.AsObject().ContainsKey("Value"));
        Assert.False(value.AsObject().ContainsKey(second));
    }

    [Fact]
    public void ReportsUnknownAndNonFiniteValuesWithoutLosingTheirFields()
    {
        var reports = new List<(string Type, string Detail)>();
        var reporting = new PropertyEncoder((type, detail) => reports.Add((type, detail)));

        var properties = reporting.Properties([
            Tag("Unknown", new InventedProperty()),
            Tag("Infinite", new FloatProperty(float.PositiveInfinity)),
            new FPropertyTag { Name = "Undecoded", PropertyType = "InventedUndecodedProperty" },
        ]);

        Assert.Equal(
            "InventedProperty",
            RecordValues.String(properties["Unknown"]![RecordFormat.Unsupported])
        );
        Assert.Equal(
            "FloatProperty",
            RecordValues.String(properties["Infinite"]![RecordFormat.Unsupported])
        );
        Assert.Equal(
            "InventedUndecodedProperty",
            RecordValues.String(properties["Undecoded"]![RecordFormat.Unsupported])
        );
        Assert.Equal(3, reports.Count);
        Assert.All(reports, report => Assert.NotEmpty(report.Detail));
    }

    private static UUserDefinedEnum InventedEnum()
    {
        var result = new UUserDefinedEnum
        {
            Name = "E_Invented",
            Names =
            [
                (new FName("E_Invented::NewEnumerator3"), 42),
                (new FName("E_Invented::NewEnumerator8"), 0),
            ],
        };
        result.Properties.Add(
            Tag(
                "DisplayNameMap",
                new MapProperty(
                    new UScriptMap(
                        new Dictionary<FPropertyTagType, FPropertyTagType?>
                        {
                            [new NameProperty("NewEnumerator8")] = new TextProperty(
                                new FText("Second choice")
                            ),
                            [new NameProperty("NewEnumerator3")] = new TextProperty(
                                new FText("First choice")
                            ),
                        }
                    )
                )
            )
        );
        return result;
    }

    private static FPropertyTag Tag(
        string name,
        FPropertyTagType value,
        FPropertyTagData? metadata = null
    ) =>
        new()
        {
            Name = name,
            PropertyType = value.GetType().Name,
            Tag = value,
            TagData = metadata,
        };

    private static StructProperty Struct(params FPropertyTag[] fields) =>
        new(new FScriptStruct(new FStructFallback(fields.ToList())));

    private sealed class InventedProperty : FPropertyTagType
    {
        public override object? GenericValue => null;

        public override string ToString() => "InventedProperty";
    }

    private sealed class FixturePackage : AbstractUePackage
    {
        public FixturePackage(params UObject[] exports)
            : base("/Game/Invented", null)
        {
            ExportsLazy = exports.Select(value => new Lazy<UObject>(() => value)).ToArray();
            IsFullyLoaded = true;
            foreach (var value in exports)
            {
                value.Outer ??= new ResolvedPackageObject(this);
            }
        }

        public override FPackageFileSummary Summary => null!;
        public override FNameEntrySerialized[] NameMap => [];
        public override int ImportMapLength => 0;
        public override int ExportMapLength => ExportsLazy.Length;

        public override int GetExportIndex(
            string name,
            StringComparison comparisonType = StringComparison.Ordinal
        ) => Array.FindIndex(ExportsLazy, value => value.Value.Name.Equals(name, comparisonType));

        public override ResolvedObject? ResolvePackageIndex(FPackageIndex? index) =>
            index is { IsExport: true } && index.Index <= ExportsLazy.Length
                ? new ResolvedLoadedObject(ExportsLazy[index.Index - 1].Value)
                : null;
    }
}
