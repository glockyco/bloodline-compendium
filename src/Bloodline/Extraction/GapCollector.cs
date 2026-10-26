using Bloodline.Records;

namespace Bloodline.Extraction;

internal sealed class GapCollector(params string[] privatePaths)
{
    private readonly List<GapRecord> records = [];

    public IReadOnlyList<GapRecord> Records =>
        records
            .Distinct()
            .OrderBy(gap => gap.Package, StringComparer.Ordinal)
            .ThenBy(gap => gap.Object, StringComparer.Ordinal)
            .ThenBy(gap => gap.Error, StringComparer.Ordinal)
            .ToArray();

    public bool TryRead<T>(string package, string? name, Func<T> read, out T? value)
    {
        try
        {
            value = read();
            return true;
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            Add(package, name, error);
            value = default;
            return false;
        }
    }

    public void Add(string package, string? name, Exception error)
    {
        var reasons = new List<string>();
        for (Exception? current = error; current != null; current = current.InnerException)
        {
            reasons.Add($"{current.GetType().Name}: {current.Message}");
        }
        Add(package, name, string.Join(" | ", reasons));
    }

    public void Add(string package, string? name, string reason)
    {
        foreach (var path in privatePaths.OrderByDescending(path => path.Length))
        {
            reason = reason.Replace(path, "<local>", StringComparison.OrdinalIgnoreCase);
        }
        records.Add(new GapRecord(package, name, reason));
    }
}
