using Bloodline.Extraction;
using Bloodline.Records;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace Bloodline.Tests.Extraction;

public sealed class MapStructMetadataTests
{
    [Fact]
    public void DecodesUntaggedNativeStructMapValuesFromTheirDeclaration()
    {
        var versions = new VersionContainer(EGame.GAME_UE4_27);
        var native = new UStruct { Name = "LinearColor" };
        var package = new MetadataPackage(native);
        var declaration = new UStruct
        {
            Name = "InventedPalette",
            ChildProperties =
            [
                new FMapProperty
                {
                    Name = "Palette",
                    KeyProp = new FIntProperty(),
                    ValueProp = new FStructProperty { Struct = new FPackageIndex(package, 1) },
                },
            ],
        };
        versions.MapStructTypes["Palette"] = new KeyValuePair<string, string>(null!, "LinearColor");
        MapStructMetadata.Register(declaration, versions);
        using var bytes = new MemoryStream();
        using (var writer = new BinaryWriter(bytes, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(0);
            writer.Write(1);
            writer.Write(7);
            writer.Write(0.1F);
            writer.Write(0.2F);
            writer.Write(0.3F);
            writer.Write(1F);
        }
        using var input = new FByteArchive("InventedPalette", bytes.ToArray(), versions);
        using var archive = new FAssetArchive(input, null);
        var map = new UScriptMap(
            archive,
            new FPropertyTagData
            {
                Name = "Palette",
                InnerType = "IntProperty",
                ValueType = "StructProperty",
            },
            ReadType.NORMAL
        );
        var value = new PropertyEncoder().Value(
            new CUE4Parse.UE4.Assets.Objects.Properties.MapProperty(map)
        );
        var entry = Assert.Single(RecordValues.MapEntries(value));
        Assert.Equal(7, RecordValues.Integer(entry.Key));
        Assert.Equal(0.1F, entry.Value!["R"]!.GetValue<float>());
        Assert.Equal(1F, entry.Value["A"]!.GetValue<float>());
        Assert.Equal(archive.Length, archive.Position);
    }

    private sealed class MetadataPackage : AbstractUePackage
    {
        public MetadataPackage(UObject value)
            : base("/Game/Invented", null)
        {
            value.Outer = new ResolvedPackageObject(this);
            ExportsLazy = [new Lazy<UObject>(() => value)];
        }

        public override FPackageFileSummary Summary => null!;
        public override FNameEntrySerialized[] NameMap => [];
        public override int ImportMapLength => 0;
        public override int ExportMapLength => 1;

        public override int GetExportIndex(
            string name,
            StringComparison comparisonType = StringComparison.Ordinal
        ) => 0;

        public override ResolvedObject? ResolvePackageIndex(FPackageIndex? index) =>
            index is { Index: 1 } ? new ResolvedLoadedObject(ExportsLazy[0].Value) : null;
    }
}
