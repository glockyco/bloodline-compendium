using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bloodline.Records;

/// <summary>Typed reads of values encoded as <see cref="RecordFormat"/> describes.</summary>
internal static class RecordValues
{
    /// <summary>The value at a dot-separated path of struct fields, such as <c>ItemInfo.ItemName</c>.</summary>
    public static JsonNode? At(JsonNode? node, string path)
    {
        foreach (var field in path.Split('.'))
        {
            if (!IsStruct(node))
            {
                return null;
            }
            node = node!.AsObject()[field];
        }
        return node;
    }

    /// <summary>Whether a value is a struct: an object without a tag key.</summary>
    public static bool IsStruct(JsonNode? node) =>
        node is JsonObject obj && !obj.Any(pair => RecordFormat.IsTag(pair.Key));

    public static string? Reference(JsonNode? node) => Tagged(node, RecordFormat.Reference);

    public static string? SoftReference(JsonNode? node) => Tagged(node, RecordFormat.SoftReference);

    /// <summary>The referenced path of a hard or soft reference.</summary>
    public static string? AnyReference(JsonNode? node) => Reference(node) ?? SoftReference(node);

    /// <summary>The source string of a text, or null when the value is not text.</summary>
    public static string? Text(JsonNode? node) => Tagged(node, RecordFormat.Text);

    /// <summary>The enum value name, such as <c>E_ItemType::NewEnumerator3</c>.</summary>
    public static string? EnumValue(JsonNode? node) => Tagged(node, RecordFormat.Enum);

    /// <summary>The display name of a user-defined enum value.</summary>
    public static string? EnumDisplay(JsonNode? node) =>
        node is JsonObject obj && obj.ContainsKey(RecordFormat.Enum)
            ? String(obj[RecordFormat.EnumDisplay])
            : null;

    /// <summary>The entries of a map, in stored order. Empty when the value is not a map.</summary>
    public static IEnumerable<(JsonNode? Key, JsonNode? Value)> MapEntries(JsonNode? node)
    {
        if (node is not JsonObject obj || obj[RecordFormat.Map] is not JsonArray entries)
        {
            yield break;
        }
        foreach (var entry in entries)
        {
            var pair = entry!.AsArray();
            yield return (pair[0], pair[1]);
        }
    }

    /// <summary>The elements of an array. Empty when the value is not an array.</summary>
    public static IEnumerable<JsonNode?> Elements(JsonNode? node) =>
        node is JsonArray array ? array : [];

    public static string? String(JsonNode? node) =>
        node is JsonValue value && value.GetValueKind() == JsonValueKind.String
            ? value.GetValue<string>()
            : null;

    /// <summary>
    /// A number of any width. Values built in code hold their CLR type, such as int or float, and
    /// <c>GetValue&lt;double&gt;</c> does not convert those, so the JSON text is parsed instead.
    /// </summary>
    public static double? Number(JsonNode? node) =>
        node is JsonValue value && value.GetValueKind() == JsonValueKind.Number
            ? double.Parse(value.ToJsonString(), NumberStyles.Float, CultureInfo.InvariantCulture)
            : null;

    public static long? Integer(JsonNode? node)
    {
        var number = Number(node);
        return number is { } n && Math.Floor(n) == n ? (long)n : null;
    }

    public static bool? Boolean(JsonNode? node) =>
        node is JsonValue value && value.GetValueKind() is JsonValueKind.True or JsonValueKind.False
            ? value.GetValue<bool>()
            : null;

    private static string? Tagged(JsonNode? node, string tag) =>
        node is JsonObject obj && obj.ContainsKey(tag) ? String(obj[tag]) : null;
}
