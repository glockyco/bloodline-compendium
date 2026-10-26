using Bloodline.Extraction;

namespace Bloodline.Code;

internal enum CodeParseStatus
{
    Complete,
    Partial,
}

internal enum CodeTargetKind
{
    Property,
    StructField,
    DataTable,
    RowStruct,
    Object,
}

internal sealed record CodeTarget(CodeTargetKind Kind, string Path)
{
    public string Label
    {
        get
        {
            var parts = Path.Split(':', 2);
            var owner = parts[0].Split('/')[^1].Split('.')[0];
            return parts.Length == 1 ? owner : owner + "." + FieldNames.StripSuffix(parts[1]);
        }
    }
}

internal sealed record CodeFunction(
    string Package,
    string Owner,
    string Name,
    bool Widget,
    string Fingerprint,
    CodeParseStatus ParseStatus,
    IReadOnlyList<CodeTarget> Reads,
    IReadOnlyList<CodeTarget> Writes,
    IReadOnlyList<string> Calls,
    IReadOnlyList<string> Errors
)
{
    public string Citation => Package + ":" + Name;
}

internal sealed record CodeReference(
    CodeTarget Target,
    IReadOnlyList<string> Readers,
    IReadOnlyList<string> Writers
);

internal sealed record CodeGap(string Package, string Error);

internal sealed record CodeIndex(
    IReadOnlyList<CodeFunction> Functions,
    IReadOnlyList<CodeReference> References,
    IReadOnlyList<CodeGap> Gaps
)
{
    public static CodeIndex Create(
        IEnumerable<CodeFunction> functions,
        IReadOnlyList<CodeGap>? gaps = null
    )
    {
        var ordered = functions
            .OrderBy(function => function.Citation, StringComparer.Ordinal)
            .ToArray();
        var references = ordered
            .SelectMany(function =>
                function
                    .Reads.Select(target =>
                        (Target: target, Function: function.Citation, Write: false)
                    )
                    .Concat(
                        function.Writes.Select(target =>
                            (Target: target, Function: function.Citation, Write: true)
                        )
                    )
            )
            .GroupBy(entry => entry.Target)
            .OrderBy(group => group.Key.Kind)
            .ThenBy(group => group.Key.Path, StringComparer.Ordinal)
            .Select(group => new CodeReference(
                group.Key,
                group
                    .Where(entry => !entry.Write)
                    .Select(entry => entry.Function)
                    .Distinct()
                    .Order(StringComparer.Ordinal)
                    .ToArray(),
                group
                    .Where(entry => entry.Write)
                    .Select(entry => entry.Function)
                    .Distinct()
                    .Order(StringComparer.Ordinal)
                    .ToArray()
            ))
            .ToArray();
        return new CodeIndex(ordered, references, gaps ?? []);
    }
}
