using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json.Nodes;
using CUE4Parse.UE4.Kismet;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace Bloodline.Code;

internal static class ScriptExpressions
{
    private static readonly ConcurrentDictionary<Type, FieldInfo[]> Fields = new();

    public static JsonObject Encode(KismetExpression expression) => (JsonObject)Value(expression)!;

    private static JsonNode? Value(object? value)
    {
        switch (value)
        {
            case null:
                return null;
            case bool scalar:
                return JsonValue.Create(scalar);
            case int scalar:
                return JsonValue.Create(scalar);
            case uint scalar:
                return JsonValue.Create(scalar);
            case long scalar:
                return JsonValue.Create(scalar);
            case ulong scalar:
                return JsonValue.Create(scalar);
            case float scalar:
                return JsonValue.Create(scalar);
            case double scalar:
                return JsonValue.Create(scalar);
            case byte scalar:
                return JsonValue.Create(scalar);
            case short scalar:
                return JsonValue.Create(scalar);
            case ushort scalar:
                return JsonValue.Create(scalar);
            case FName name:
                return JsonValue.Create(name.Text);
            case FPackageIndex index:
                return new JsonObject { ["$object"] = index.ResolvedObject?.GetPathName() };
            case FKismetPropertyPointer pointer:
                return pointer.New is { } field
                    ? new JsonObject
                    {
                        ["$field"] =
                            (field.ResolvedOwner?.ResolvedObject?.GetPathName() ?? "?")
                            + ":"
                            + string.Join(".", field.Path.Select(name => name.Text)),
                        ["$fieldKind"] = field.ResolvedOwner?.ResolvedObject?.Class?.Name.Text
                            is "ScriptStruct"
                                or "UserDefinedStruct"
                            ? "struct"
                            : "property",
                        ["$local"] =
                            field.ResolvedOwner?.ResolvedObject?.Class?.Name.Text == "Function",
                    }
                    : new JsonObject { ["$field"] = pointer.Old?.ResolvedObject?.GetPathName() };
            case string text:
                return JsonValue.Create(text);
            case Enum enumeration:
                return JsonValue.Create(enumeration.ToString());
            case IEnumerable elements:
                return new JsonArray(elements.Cast<object?>().Select(Value).ToArray());
        }
        var type = value.GetType();
        if (type.IsPrimitive || value is decimal)
            return JsonNode.Parse(JsonConvert.SerializeObject(value));
        var result = new JsonObject();
        if (value is KismetExpression expression)
            result["Token"] = expression.Token.ToString();
        foreach (
            var field in Fields.GetOrAdd(
                type,
                type =>
                    type.GetFields(BindingFlags.Instance | BindingFlags.Public)
                        .OrderBy(field => field.Name, StringComparer.Ordinal)
                        .ToArray()
            )
        )
        {
            // These are diagnostic names or serialized sizes, not executable operands.
            if (field.Name is "ObjectName" or "LineNumber" or "StructSize")
                continue;
            result[field.Name] = Value(field.GetValue(value));
        }
        if (result.Count == 0)
            return JsonNode.Parse(JsonConvert.SerializeObject(value));
        return result;
    }

    public static IEnumerable<JsonObject> Walk(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            yield return obj;
            foreach (var child in obj.SelectMany(pair => Walk(pair.Value)))
                yield return child;
        }
        else if (node is JsonArray array)
        {
            foreach (var child in array.SelectMany(Walk))
                yield return child;
        }
    }

    public static string? Text(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    public static int? Integer(JsonNode? node)
    {
        if (node is not JsonValue value)
            return null;
        if (value.TryGetValue<int>(out var number))
            return number;
        return value.TryGetValue<uint>(out var address) && address <= int.MaxValue
            ? (int)address
            : null;
    }
}
