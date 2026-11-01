using System.CommandLine;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using Bloodline.Output;

namespace Bloodline.SiteData;

/// <summary>
/// Exports JSON Schema for <see cref="SiteDataFiles"/> and for <see cref="KnownIssueRegistry"/>. Each site-data type
/// becomes a named definition, so generated TypeScript has one type per C# type instead of one copy per use.
/// </summary>
internal static class SiteDataSchema
{
    public static Command CreateCommand()
    {
        var output = new Option<FileInfo>("--out")
        {
            Description = "File to write the site data schema to.",
            Required = true,
        };
        var registry = new Option<FileInfo?>("--registry")
        {
            Description = "File to write the known issue registry's schema to.",
        };
        var command = new Command(
            "schema",
            "Write the JSON Schema of the site data and of the known issue registry."
        )
        {
            output,
            registry,
        };
        command.SetAction(parse =>
        {
            CanonicalJson.Write(parse.GetValue(output)!.FullName, Export(typeof(SiteDataFiles)));
            if (parse.GetValue(registry) is { } file)
            {
                CanonicalJson.Write(file.FullName, Export(typeof(KnownIssueRegistry)));
            }
        });
        return command;
    }

    public static JsonObject Export(Type type)
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
        var root = CanonicalJson.Options.GetJsonSchemaAsNode(type, exporterOptions).AsObject();
        var definitions = new JsonObject();
        var original = root.DeepClone().AsObject();
        var active = new HashSet<string>(StringComparer.Ordinal);
        foreach (var key in root.Select(pair => pair.Key).ToList())
        {
            root[key] = Hoist(root[key], definitions, original, active);
        }
        root["$schema"] = "https://json-schema.org/draft/2020-12/schema";
        root["$defs"] = definitions;
        return root;
    }

    /// <summary>Moves every titled schema below <paramref name="node"/> into <paramref name="definitions"/> and refers to it instead.</summary>
    private static JsonNode? Hoist(
        JsonNode? node,
        JsonObject definitions,
        JsonObject original,
        HashSet<string> active
    )
    {
        switch (node)
        {
            case JsonArray array:
                for (var i = 0; i < array.Count; i++)
                {
                    array[i] = Hoist(array[i]?.DeepClone(), definitions, original, active);
                }
                return array;
            case JsonObject obj:
                // The exporter reuses schemas by JSON pointer, including nullable property schemas.
                // Resolve before hoisting so those pointers do not outlive their original locations.
                if (obj["$ref"] is JsonValue pointer)
                {
                    JsonNode? target = original;
                    foreach (
                        var segment in pointer.GetValue<string>() == "#"
                            ? Array.Empty<string>()
                            : pointer.GetValue<string>()[2..].Split('/')
                    )
                    {
                        target = target![segment.Replace("~1", "/").Replace("~0", "~")];
                    }
                    return Hoist(target!.DeepClone(), definitions, original, active);
                }
                var name = (obj["title"] as JsonValue)?.GetValue<string>();
                var nullable = name is not null && RemoveNull(obj);
                var reference = name is null
                    ? null
                    : new JsonObject { ["$ref"] = $"#/$defs/{name}" };
                JsonNode NamedReference() =>
                    nullable
                        ? new JsonObject
                        {
                            ["anyOf"] = new JsonArray(
                                reference,
                                new JsonObject { ["type"] = "null" }
                            ),
                        }
                        : reference!;
                if (name is not null && !active.Add(name))
                    return NamedReference();
                foreach (var key in obj.Select(pair => pair.Key).ToList())
                {
                    obj[key] = Hoist(obj[key]?.DeepClone(), definitions, original, active);
                }
                if (name is null)
                {
                    return obj;
                }
                active.Remove(name);
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
                return NamedReference();
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
