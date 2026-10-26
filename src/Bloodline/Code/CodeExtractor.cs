using CUE4Parse.UE4.AssetRegistry.Objects;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Objects.UObject;

namespace Bloodline.Code;

internal sealed class CodeExtractor(FAssetData[] assets)
{
    private readonly HashSet<string> tables = assets
        .Where(asset => asset.AssetClass.Text == "DataTable")
        .Select(asset => asset.ObjectPath)
        .ToHashSet(StringComparer.Ordinal);
    private readonly HashSet<string> structs = assets
        .Where(asset => asset.AssetClass.Text == "UserDefinedStruct")
        .Select(asset => asset.ObjectPath)
        .ToHashSet(StringComparer.Ordinal);
    private readonly Dictionary<string, string> rowStructs = new(StringComparer.Ordinal);
    private readonly HashSet<string> rowTypes = new(StringComparer.Ordinal);
    private readonly HashSet<string> packages = new(StringComparer.Ordinal);
    private readonly List<CodeFunction> functions = [];
    private readonly List<CodeGap> gaps = [];

    public void RegisterTable(string path, string? rowStruct)
    {
        if (rowStruct is not null)
        {
            rowStructs[path] = rowStruct;
            rowTypes.Add(rowStruct);
        }
    }

    public void ReadPackage(IPackage package, string path, bool widget)
    {
        if (!packages.Add(path))
            return;
        var scripts = new Dictionary<string, ScriptFunction>(StringComparer.Ordinal);
        for (var index = 0; index < package.ExportsLazy.Length; index++)
        {
            try
            {
                var info = package.ResolvePackageIndex(new FPackageIndex(package, index + 1));
                if (info?.Class?.Name.Text != "Function")
                    continue;
                if (package.ExportsLazy[index].Value is not UFunction function)
                    continue;
                var statements =
                    function.ScriptBytecode?.Select(ScriptExpressions.Encode).ToArray() ?? [];
                if (function.ScriptBytecode is { Length: 0 })
                    statements =
                    [
                        new System.Text.Json.Nodes.JsonObject
                        {
                            ["Token"] = "UNSUPPORTED_CUE4Parse",
                            ["StatementIndex"] = 0,
                        },
                    ];
                var script = new ScriptFunction(
                    path,
                    function.Outer?.GetPathName() ?? path,
                    function.Name,
                    widget,
                    statements
                );
                scripts.Add(script.Path, script);
            }
            catch (Exception error) when (error is not OutOfMemoryException)
            {
                gaps.Add(new CodeGap(path, $"Export {index}: {error.GetType().Name}"));
            }
        }
        var analysis = new FunctionAnalysis(scripts, tables, structs, structs);
        foreach (
            var function in scripts.Values.OrderBy(
                function => function.Name,
                StringComparer.Ordinal
            )
        )
            functions.Add(analysis.Analyze(function));
    }

    public CodeIndex Complete() =>
        CodeIndex.Create(
            functions.Select(function =>
                function with
                {
                    Reads = function
                        .Reads.Select(target =>
                            target.Kind == CodeTargetKind.RowStruct
                            && !rowTypes.Contains(target.Path)
                                ? target with
                                {
                                    Kind = CodeTargetKind.Object,
                                }
                                : target
                        )
                        .Concat(
                            function
                                .Reads.Where(target =>
                                    target.Kind == CodeTargetKind.DataTable
                                    && rowStructs.ContainsKey(target.Path)
                                )
                                .Select(target => new CodeTarget(
                                    CodeTargetKind.RowStruct,
                                    rowStructs[target.Path]
                                ))
                        )
                        .Distinct()
                        .OrderBy(target => target.Path, StringComparer.Ordinal)
                        .ToArray(),
                }
            ),
            gaps.OrderBy(gap => gap.Package, StringComparer.Ordinal)
                .ThenBy(gap => gap.Error, StringComparer.Ordinal)
                .ToArray()
        );
}
