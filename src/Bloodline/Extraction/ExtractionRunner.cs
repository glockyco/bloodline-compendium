using System.Diagnostics;
using System.Text.Json.Nodes;
using Bloodline.Configuration;
using Bloodline.Output;
using Bloodline.Records;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.AssetRegistry;
using CUE4Parse.UE4.AssetRegistry.Objects;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace Bloodline.Extraction;

internal sealed class ExtractionRunner
{
    internal const string ItemBase =
        "/Game/BloodlinesAlpha/Interactables/BP_BaseItemAsset.BP_BaseItemAsset_C";
    private readonly DefaultFileProvider provider;
    private readonly GapCollector gaps;
    private readonly PropertyEncoder encoder;
    private readonly Dictionary<string, FAssetData> classes;
    private readonly Dictionary<string, FAssetData[]> types;
    private readonly Dictionary<string, UObject> typeCache = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ClassRecord> merged = new(StringComparer.Ordinal);
    private readonly HashSet<string> loading = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Exception> failed = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> unsupported = new(StringComparer.Ordinal);

    private ExtractionRunner(DefaultFileProvider provider, FAssetData[] assets, GapCollector gaps)
    {
        this.provider = provider;
        this.gaps = gaps;
        classes = assets
            .Where(asset => Tag(asset, "GeneratedClass") != null)
            .ToDictionary(
                asset => Reference(Tag(asset, "GeneratedClass")!),
                StringComparer.Ordinal
            );
        types = assets
            .Where(asset => asset.AssetClass.Text is "UserDefinedStruct" or "UserDefinedEnum")
            .GroupBy(asset => asset.AssetName.Text, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        ConfigureMapStructTypes(assets);
        encoder = new PropertyEncoder(resolveType: ResolveType);
    }

    public static void Run(LocalConfiguration configuration)
    {
        var clock = Stopwatch.StartNew();
        using var memory = new PeakMemorySample();
        var snapshot = InstallationSnapshot.Capture(configuration);
        var build = snapshot.Manifest.Build;
        var layout = new RecordLayout(configuration.OutputDirectory, build.BuildId);
        using var output = new AtomicRecordsDirectory(layout.RecordsDirectory);
        var root = output.DirectoryPath;
        var gaps = new GapCollector(configuration.SteamLibrary, configuration.OutputDirectory);
        CUE4Parse.Globals.FatalObjectSerializationErrors = true;
        using var provider = new DefaultFileProvider(
            snapshot.PaksDirectory,
            SearchOption.TopDirectoryOnly,
            new VersionContainer(EGame.GAME_UE4_27),
            StringComparer.OrdinalIgnoreCase
        );
        provider.Initialize();
        provider.ReadScriptData = false;
        provider.Mount();
        provider.PostMount();
        provider.LoadVirtualPaths();
        var registry = provider.Files.Single(pair =>
            pair.Key.EndsWith("/AssetRegistry.bin", StringComparison.OrdinalIgnoreCase)
        );
        using var registryReader = registry.Value.CreateReader();
        var assets = new FAssetRegistryState(registryReader).PreallocatedAssetDataBuffers;
        var runner = new ExtractionRunner(provider, assets, gaps);
        var counts = runner.WriteRecords(root, assets);
        var icons = Icons.IconExtractor.Extract(
            provider,
            ItemIconTexturePaths(runner.merged.Values),
            Path.Combine(root, "icons")
        );
        CanonicalJson.Write(Path.Combine(root, "textures.json"), icons.Textures);
        foreach (var gap in icons.Gaps)
            gaps.Add(gap.Package, gap.Object, gap.Error);
        counts["textures"] = icons.Textures.Count;
        CanonicalJson.Write(Path.Combine(root, "build.json"), build);
        CanonicalJson.Write(Path.Combine(root, "gaps.json"), gaps.Records);
        snapshot.AssertUnchanged();
        output.Commit();
        var bytes = Directory
            .EnumerateFiles(layout.RecordsDirectory, "*", SearchOption.AllDirectories)
            .Sum(file => new FileInfo(file).Length);
        Console.WriteLine(
            $"Steam build {build.BuildId}: {string.Join(", ", counts.Select(pair => $"{pair.Key}={pair.Value}"))}"
        );
        Console.WriteLine(
            $"Item descendants={runner.merged.Values.Count(record => record.Ancestors.Contains(ItemBase, StringComparer.Ordinal))}"
        );
        foreach (
            var reason in gaps
                .Records.GroupBy(gap => gap.Error.Split(':', 2)[0])
                .OrderBy(group => group.Key, StringComparer.Ordinal)
        )
        {
            Console.WriteLine($"Gap {reason.Key}: {reason.Count()}");
        }
        foreach (var type in runner.unsupported.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            Console.WriteLine($"Unsupported {type.Key}: {type.Value}");
        }
        Console.WriteLine(
            $"Records {bytes:N0} bytes. Duration {clock.Elapsed.TotalSeconds:F2}s. Peak sampled memory {memory.Bytes:N0} bytes."
        );
    }

    private Dictionary<string, int> WriteRecords(string root, FAssetData[] assets)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["classes"] = 0,
            ["tables"] = 0,
            ["enums"] = 0,
            ["levels"] = 0,
            ["dialogues"] = 0,
        };
        foreach (var path in classes.Keys.Order(StringComparer.Ordinal))
        {
            if (gaps.TryRead(path.Split('.')[0], path, () => Class(path), out var record))
            {
                Write("classes", path.Split('.')[0], record!);
            }
        }
        foreach (var asset in assets.OrderBy(asset => asset.ObjectPath, StringComparer.Ordinal))
        {
            var kind = asset.AssetClass.Text switch
            {
                "DataTable" => "tables",
                "UserDefinedEnum" => "enums",
                "World" => "levels",
                "Dialogue" => "dialogues",
                _ => null,
            };
            if (kind == null)
                continue;
            var packageName = asset.PackageName.Text;
            if (
                !gaps.TryRead(
                    packageName,
                    null,
                    () => provider.LoadPackage(packageName),
                    out var package
                )
            )
                continue;
            if (
                gaps.TryRead<object>(
                    packageName,
                    asset.ObjectPath,
                    () =>
                        kind switch
                        {
                            "tables" => Table(
                                package!.GetExport<UDataTable>(asset.AssetName.Text),
                                asset.ObjectPath
                            ),
                            "enums" => new EnumRecord(
                                asset.ObjectPath,
                                encoder.EnumEntries(
                                    package!.GetExport<UUserDefinedEnum>(asset.AssetName.Text)
                                )
                            ),
                            "levels" => Level(package!, asset),
                            "dialogues" => Dialogue(package!, asset),
                            _ => throw new InvalidOperationException(kind),
                        },
                    out var record
                )
            )
                Write(kind, packageName, record!);
            if (kind == "levels")
                GC.Collect();
        }
        return counts;

        void Write(string kind, string path, object record)
        {
            ReportUnsupported(path, record);
            CanonicalJson.Write(RecordLayout.PackageFile(Path.Combine(root, kind), path), record);
            counts[kind]++;
        }
    }

    private ClassRecord Class(string path)
    {
        if (merged.TryGetValue(path, out var found))
            return found;
        if (failed.TryGetValue(path, out var failure))
            throw new InvalidDataException($"Class defaults unavailable for {path}", failure);
        if (!loading.Add(path))
            throw new InvalidDataException($"Blueprint parent cycle at {path}");
        try
        {
            var asset =
                classes.GetValueOrDefault(path)
                ?? throw new InvalidDataException($"Blueprint class not in asset registry: {path}");
            var package = provider.LoadPackage(asset.PackageName.Text);
            var generated = package.GetExport<UClass>(path.Split('.')[^1]);
            MapStructMetadata.Register(generated, provider.Versions);
            var parent = generated.SuperStruct.ResolvedObject?.GetPathName();
            var ancestors = new List<string>();
            var inherited = new JsonObject();
            if (parent != null)
            {
                ancestors.Add(parent);
                if (!parent.StartsWith("/Script/", StringComparison.Ordinal))
                {
                    var parentRecord = Class(parent);
                    inherited = parentRecord.Properties;
                    ancestors.AddRange(parentRecord.Ancestors);
                }
            }
            var declarations =
                generated
                    .ChildProperties?.OfType<FProperty>()
                    .Where(property => !PropertyEncoder.IsEventBinding(property))
                    .ToArray()
                ?? [];
            var fieldNames = FieldNames.Resolve(
                declarations.Select(property => property.Name.Text)
            );
            var zero = new JsonObject();
            foreach (var property in declarations)
                zero[fieldNames[property.Name.Text]] = encoder.Zero(property);
            var cdo =
                generated.ClassDefaultObject.ResolvedObject?.Load()
                ?? throw new InvalidDataException($"Missing class default object for {path}");
            var properties = RecordMerge.OverlayProperties(
                inherited,
                DefaultChain.Merge([
                    new DefaultLayer(zero, encoder.Properties(cdo.Properties, generated)),
                ])
            );
            var record = new ClassRecord(path, ancestors, properties);
            merged.Add(path, record);
            return record;
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            failed[path] = error;
            throw;
        }
        finally
        {
            loading.Remove(path);
        }
    }

    private TableRecord Table(UDataTable table, string path)
    {
        if (table.RowMap == null)
            throw new InvalidDataException($"DataTable {path} did not deserialize its rows");
        var pointer = table.GetOrDefault<FPackageIndex?>("RowStruct");
        var scriptStruct = pointer?.ResolvedObject?.Load() as UStruct;
        var defaults =
            scriptStruct == null ? new JsonObject() : encoder.StructDefaults(scriptStruct);
        return new TableRecord(
            path,
            pointer?.ResolvedObject?.GetPathName() ?? table.RowStructName,
            table
                .RowMap.Select(row => new TableRow(
                    row.Key.Text,
                    RecordMerge.OverlayProperties(
                        defaults,
                        scriptStruct == null
                            ? encoder.Properties(row.Value.Properties)
                            : encoder.Properties(row.Value.Properties, scriptStruct)
                    )
                ))
                .ToArray()
        );
    }

    private LevelRecord Level(IPackage package, FAssetData asset)
    {
        var world = package.GetExport<UWorld>(asset.AssetName.Text);
        var streaming = new List<string>();
        foreach (var reference in world.StreamingLevels ?? [])
        {
            var name = reference.ResolvedObject?.GetPathName() ?? reference.Name;
            if (
                gaps.TryRead(
                    asset.PackageName.Text,
                    name,
                    () => reference.ResolvedObject?.Load(),
                    out var value
                )
                && value != null
            )
            {
                var properties = encoder.Properties(value.Properties);
                var target =
                    properties["WorldAsset"]?[RecordFormat.SoftReference]?.GetValue<string>()
                    ?? properties["PackageName"]?.GetValue<string>();
                if (!string.IsNullOrEmpty(target) && target != "None")
                    streaming.Add(target.Split('.')[0]);
            }
        }
        var actors = new List<ActorRecord>();
        for (var i = 0; i < package.ExportsLazy.Length; i++)
        {
            var info = package.ResolvePackageIndex(new FPackageIndex(package, i + 1));
            if (info?.Outer?.Name.Text != "PersistentLevel")
                continue;
            var classPath = info.Class?.GetPathName();
            if (classPath == null || !classes.ContainsKey(classPath))
                continue;
            if (
                gaps.TryRead(
                    asset.PackageName.Text,
                    info.GetPathName(),
                    () =>
                    {
                        var actor = package.ExportsLazy[i].Value;
                        var defaults = Class(classPath).Properties;
                        var declaration =
                            actor.Class?.Load() as UStruct
                            ?? throw new InvalidDataException(
                                $"Missing Blueprint declaration for {classPath}"
                            );
                        return new ActorRecord(
                            actor.Name,
                            classPath,
                            DefaultChain.Placement(
                                defaults,
                                encoder.Properties(actor.Properties, declaration)
                            )
                        );
                    },
                    out var record
                )
            )
                actors.Add(record!);
        }
        return new LevelRecord(asset.PackageName.Text, streaming, actors);
    }

    private AssetRecord Dialogue(IPackage package, FAssetData asset)
    {
        var dialogue = package.GetExport(asset.AssetName.Text);
        var objects = new List<InnerObjectRecord>();
        for (var i = 0; i < package.ExportsLazy.Length; i++)
        {
            var info = package.ResolvePackageIndex(new FPackageIndex(package, i + 1));
            if (info == null || info.Name.Text == asset.AssetName.Text)
                continue;
            var outer = info.Outer;
            while (outer != null && outer.Name.Text != asset.AssetName.Text)
                outer = outer.Outer;
            if (outer == null)
                continue;
            var classPath =
                info.Class?.GetPathName()
                ?? throw new InvalidDataException($"Missing class for {info.GetPathName()}");
            if (
                gaps.TryRead(
                    asset.PackageName.Text,
                    info.GetPathName(),
                    () =>
                    {
                        var inner = package.ExportsLazy[i].Value;
                        var defaults = classes.ContainsKey(classPath)
                            ? Class(classPath).Properties
                            : new JsonObject();
                        return new InnerObjectRecord(
                            info.GetPathName(),
                            classPath,
                            DefaultChain.Placement(
                                defaults,
                                inner.Class?.Load() is UStruct declaration
                                    ? encoder.Properties(inner.Properties, declaration)
                                    : encoder.Properties(inner.Properties)
                            )
                        );
                    },
                    out var record
                )
            )
                objects.Add(record!);
        }
        return new AssetRecord(
            asset.ObjectPath,
            dialogue.Class?.GetPathName() ?? dialogue.ExportType,
            encoder.Properties(dialogue.Properties),
            objects
        );
    }

    private void ConfigureMapStructTypes(IEnumerable<FAssetData> assets)
    {
        foreach (var asset in assets.Where(asset => asset.AssetClass.Text == "UserDefinedStruct"))
        {
            if (
                gaps.TryRead(
                    asset.PackageName.Text,
                    asset.ObjectPath,
                    () =>
                        provider
                            .LoadPackage(asset.PackageName.Text)
                            .GetExport<UUserDefinedStruct>(asset.AssetName.Text),
                    out var declaration
                )
            )
            {
                MapStructMetadata.Register(declaration!, provider.Versions);
            }
        }
    }

    private UObject? ResolveType(string name)
    {
        if (typeCache.TryGetValue(name, out var found))
            return found;
        var shortName = name.Split('.').Last();
        if (!types.TryGetValue(shortName, out var candidates))
            return null;
        var matches = name.StartsWith('/')
            ? candidates.Where(asset => asset.ObjectPath == name).ToArray()
            : candidates;
        if (matches.Length != 1)
            throw new InvalidDataException($"Ambiguous or missing property type {name}");
        var asset = matches[0];
        var value = provider.LoadPackage(asset.PackageName.Text).GetExport(asset.AssetName.Text);
        typeCache.Add(name, value);
        return value;
    }

    private void ReportUnsupported(string package, object record)
    {
        switch (record)
        {
            case ClassRecord generated:
                Properties(generated.Path, generated.Properties);
                break;
            case TableRecord table:
                for (var index = 0; index < table.Rows.Count; index++)
                    Properties(table.Path, table.Rows[index].Values, $"/rows/{index}/values");
                break;
            case LevelRecord level:
                foreach (var actor in level.Actors)
                    Properties(actor.Name, actor.Properties);
                break;
            case AssetRecord asset:
                Properties(asset.Path, asset.Properties);
                foreach (var inner in asset.Objects)
                    Properties(inner.Path, inner.Properties);
                break;
        }

        void Properties(string name, JsonObject values, string prefix = "")
        {
            foreach (var value in UnsupportedValues.Find(values))
            {
                unsupported[value.Type] = unsupported.GetValueOrDefault(value.Type) + 1;
                gaps.Add(package, name, $"Unsupported property {value.Type}: {prefix}{value.Path}");
            }
        }
    }

    internal static IReadOnlyCollection<string> ItemIconTexturePaths(
        IEnumerable<ClassRecord> records
    ) =>
        records
            .Where(record =>
                record.Path == ItemBase
                || record.Ancestors.Contains(ItemBase, StringComparer.Ordinal)
            )
            .Select(record =>
                record
                    .Properties["ItemInfo"]
                    ?["ItemImage"]?[RecordFormat.Reference]?.GetValue<string>()
            )
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

    private static string? Tag(FAssetData asset, string name) =>
        asset
            .TagsAndValues.Where(pair => pair.Key.Text == name)
            .Select(pair => pair.Value)
            .SingleOrDefault();

    private static string Reference(string value) =>
        value.Contains('\'') ? value.Split('\'')[1] : value;
}
