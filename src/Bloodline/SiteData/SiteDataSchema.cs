using System.CommandLine;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using Bloodline.Output;

namespace Bloodline.SiteData;

/// <summary>
/// Exports JSON Schema for <see cref="SiteDataFiles"/>. Each site-data type becomes a named definition, so
/// generated TypeScript has one type per C# type instead of one copy per use.
/// </summary>
internal static class SiteDataSchema
{
    public static Command CreateCommand()
    {
        var output = new Option<FileInfo>("--out")
        {
            Description = "File to write the schema to.",
            Required = true,
        };
        var command = new Command("schema", "Write the JSON Schema of the site data.") { output };
        command.SetAction(parse => CanonicalJson.Write(parse.GetValue(output)!.FullName, Export()));
        return command;
    }

    public static JsonObject Export()
    {
        var exporterOptions = new JsonSchemaExporterOptions
        {
            TreatNullObliviousAsNonNullable = true,
            TransformSchemaNode = (context, schema) =>
            {
                if (
                    context.TypeInfo.Type.Namespace == typeof(SiteDataFiles).Namespace
                    && schema is JsonObject obj
                )
                {
                    obj["title"] = context.TypeInfo.Type.Name;
                }
                return schema;
            },
        };
        var root = CanonicalJson
            .Options.GetJsonSchemaAsNode(typeof(SiteDataFiles), exporterOptions)
            .AsObject();
        var definitions = new JsonObject();
        foreach (var key in root.Select(pair => pair.Key).ToList())
        {
            root[key] = Hoist(root[key], definitions);
        }
        root["$schema"] = "https://json-schema.org/draft/2020-12/schema";
        root["$defs"] = definitions;
        return root;
    }

    /// <summary>Moves every titled schema below <paramref name="node"/> into <paramref name="definitions"/> and refers to it instead.</summary>
    private static JsonNode? Hoist(JsonNode? node, JsonObject definitions)
    {
        switch (node)
        {
            case JsonArray array:
                for (var i = 0; i < array.Count; i++)
                {
                    array[i] = Hoist(array[i]?.DeepClone(), definitions);
                }
                return array;
            case JsonObject obj:
                foreach (var key in obj.Select(pair => pair.Key).ToList())
                {
                    obj[key] = Hoist(obj[key]?.DeepClone(), definitions);
                }
                if (obj["title"] is not JsonValue title)
                {
                    return obj;
                }
                var name = title.GetValue<string>();
                var nullable = RemoveNull(obj);
                if (definitions[name] is { } existing)
                {
                    if (!JsonNode.DeepEquals(existing, obj))
                    {
                        throw new InvalidOperationException(
                            $"Type {name} has two different schemas."
                        );
                    }
                }
                else
                {
                    definitions[name] = obj;
                }
                var reference = new JsonObject { ["$ref"] = $"#/$defs/{name}" };
                return nullable
                    ? new JsonObject
                    {
                        ["anyOf"] = new JsonArray(reference, new JsonObject { ["type"] = "null" }),
                    }
                    : reference;
            default:
                return node;
        }
    }

    /// <summary>Removes null from a schema's type or enum, and reports whether it allowed null.</summary>
    private static bool RemoveNull(JsonObject schema)
    {
        var removed = false;
        if (schema["type"] is JsonArray types)
        {
            var kept = types.Where(type => type?.GetValue<string>() != "null").ToList();
            removed = kept.Count != types.Count;
            schema["type"] =
                kept.Count == 1
                    ? kept[0]!.DeepClone()
                    : new JsonArray([.. kept.Select(type => type!.DeepClone())]);
        }
        if (schema["enum"] is JsonArray values && values.Any(value => value is null))
        {
            schema["enum"] = new JsonArray([
                .. values.Where(value => value is not null).Select(value => value!.DeepClone()),
            ]);
            removed = true;
        }
        return removed;
    }
}
