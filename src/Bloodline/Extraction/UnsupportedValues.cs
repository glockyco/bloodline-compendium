using System.Globalization;
using System.Text.Json.Nodes;
using Bloodline.Records;

namespace Bloodline.Extraction;

internal sealed record UnsupportedValue(string Path, string Type);

internal static class UnsupportedValues
{
    public static IReadOnlyList<UnsupportedValue> Find(JsonNode? value)
    {
        var result = new List<UnsupportedValue>();
        var path = new List<string>();
        Visit(value);
        return result;

        void Visit(JsonNode? node)
        {
            if (node is JsonObject obj)
            {
                if (RecordValues.String(obj[RecordFormat.Unsupported]) is { } type)
                {
                    var pointer =
                        path.Count == 0 ? "" : "/" + string.Join('/', path.Select(Escape));
                    result.Add(new UnsupportedValue(pointer, type));
                    return;
                }
                foreach (var (name, child) in obj)
                {
                    path.Add(name);
                    Visit(child);
                    path.RemoveAt(path.Count - 1);
                }
            }
            else if (node is JsonArray array)
            {
                for (var index = 0; index < array.Count; index++)
                {
                    path.Add(index.ToString(CultureInfo.InvariantCulture));
                    Visit(array[index]);
                    path.RemoveAt(path.Count - 1);
                }
            }
        }
    }

    private static string Escape(string segment) =>
        segment
            .Replace("~", "~0", StringComparison.Ordinal)
            .Replace("/", "~1", StringComparison.Ordinal);
}
