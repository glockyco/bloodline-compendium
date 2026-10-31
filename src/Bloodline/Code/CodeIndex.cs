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

    /// <summary>
    /// Names the function assigns right after it compares a value with a class, in code order, such as the recipe
    /// row that <c>Crafting:Get Correct Item</c> picks for each item class.
    /// </summary>
    public IReadOnlyList<ClassName> ClassNames { get; init; } = [];

    /// <summary>Actors created by this entry and the Damage value assigned to each.</summary>
    public IReadOnlyList<SpawnedDamage>? SpawnedDamage { get; init; }
}

/// <summary>A class the code compares a value with and the name it assigns when they are equal.</summary>
internal sealed record ClassName(string Class, string Name);

internal sealed record SpawnedDamage(string Class, bool Assigned, double? Damage);

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
