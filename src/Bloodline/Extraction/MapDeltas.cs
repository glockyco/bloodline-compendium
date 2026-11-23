using System.Reflection;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Readers;

namespace Bloodline.Extraction;

/// <summary>
/// The keys that an object's map properties remove from its archetype's maps. Unreal saves a map that differs from
/// its archetype's as the keys it removes, then the entries it adds or changes, and CUE4Parse reads the removed keys
/// without keeping them, so this reads them again from the package's bytes. It reads the top-level properties of an
/// export, so a map inside a struct keeps the entries alone.
/// </summary>
internal sealed class MapDeltas(IFileProvider provider)
{
    private static readonly MethodInfo ReadValue =
        typeof(FPropertyTagType).GetMethod(
            "ReadPropertyTagType",
            BindingFlags.NonPublic | BindingFlags.Static
        ) ?? throw new MissingMethodException("FPropertyTagType.ReadPropertyTagType is gone.");

    /// <summary>The removed keys of every map property of each export of <paramref name="package"/>, by property name and array index.</summary>
    public Func<int, IReadOnlyDictionary<(string Name, int Index), FPropertyTagType?[]>> For(
        IPackage package
    )
    {
        if (package is not IoPackage io)
        {
            throw new InvalidDataException(
                $"{package.Name} is not an IoStore package, so its map deltas cannot be read."
            );
        }
        // A package's name is its virtual path, such as /Game/Maps/Level, which the provider maps to its file.
        if (
            !provider.TryGetGameFile(package.Name + ".umap", out var file)
            && !provider.TryGetGameFile(package.Name + ".uasset", out file)
        )
        {
            throw new InvalidDataException($"No package file for {package.Name}.");
        }
        var bytes = file.Read();
        var starts = ExportStarts(io, bytes, file.Path);
        var cache = new Dictionary<int, IReadOnlyDictionary<(string, int), FPropertyTagType?[]>>();
        return index =>
        {
            if (!cache.TryGetValue(index, out var removed))
            {
                removed = starts.TryGetValue(index, out var start)
                    ? Read(io, bytes, file.Path, start)
                    : new Dictionary<(string, int), FPropertyTagType?[]>();
                cache[index] = removed;
            }
            return removed;
        };
    }

    // Unreal 4.26 and 4.27 lay the exports out one after another in export bundle order after the graph data.
    private Dictionary<int, long> ExportStarts(IoPackage package, byte[] bytes, string name)
    {
        var header = new FByteArchive(name, bytes, provider.Versions);
        var summary = header.Read<FPackageSummary>();
        header.Position = summary.ExportBundlesOffset;
        var remaining = (summary.GraphDataOffset - summary.ExportBundlesOffset) / 8;
        var found = 0;
        var bundles = new List<FExportBundleHeader>();
        while (found < remaining)
        {
            remaining--;
            var bundle = new FExportBundleHeader(header);
            found += (int)bundle.EntryCount;
            bundles.Add(bundle);
        }
        var entries = header.ReadArray<FExportBundleEntry>(found);
        var starts = new Dictionary<int, long>();
        long offset = summary.GraphDataOffset + summary.GraphDataSize;
        foreach (var bundle in bundles)
        {
            for (var i = 0u; i < bundle.EntryCount; i++)
            {
                var entry = entries[bundle.FirstEntryIndex + i];
                if (entry.CommandType != EExportCommandType.ExportCommandType_Serialize)
                    continue;
                starts[(int)entry.LocalExportIndex] = offset;
                offset += (long)package.ExportMap[entry.LocalExportIndex].CookedSerialSize;
            }
        }
        return starts;
    }

    private Dictionary<(string, int), FPropertyTagType?[]> Read(
        IoPackage package,
        byte[] bytes,
        string name,
        long start
    )
    {
        var archive = new FAssetArchive(new FByteArchive(name, bytes, provider.Versions), package)
        {
            Position = start,
        };
        var removed = new Dictionary<(string, int), FPropertyTagType?[]>();
        while (true)
        {
            var tag = new FPropertyTag(archive, false);
            if (tag.Name.IsNone)
                break;
            var value = archive.Position;
            if (tag.PropertyType.Text is "MapProperty" && tag.TagData?.InnerType is { } keyType)
            {
                var count = archive.Read<int>();
                var keys = new FPropertyTagType?[count];
                for (var i = 0; i < count; i++)
                {
                    keys[i] = (FPropertyTagType?)
                        ReadValue.Invoke(
                            null,
                            [archive, keyType, tag.TagData.InnerTypeData, ReadType.MAP, 0]
                        );
                }
                removed[(tag.Name.Text, tag.ArrayIndex)] = keys;
            }
            archive.Position = value + tag.Size;
        }
        return removed;
    }
}
