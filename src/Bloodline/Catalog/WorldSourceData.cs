using System.Globalization;
using System.Text.Json.Nodes;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal static class WorldSourceData
{
    private static readonly string[] NameFields = ["Full Name", "Name", "ItemInfo.ItemName"];

    public static bool IsClass(CatalogContext context, string path, string name) =>
        ItemIdentity.ClassName(path) == ItemIdentity.ClassName(name)
        || (
            context.Classes.TryGetValue(path, out var type)
            && type.Ancestors.Any(parent =>
                ItemIdentity.ClassName(parent) == ItemIdentity.ClassName(name)
            )
        );

    public static string Name(JsonObject properties, string path)
    {
        foreach (var field in NameFields)
        {
            var value = RecordValues.At(properties, field);
            var name = RecordValues.Text(value) ?? RecordValues.String(value);
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
        }
        return ItemIdentity.Readable(ItemIdentity.ClassName(path));
    }

    public static bool IsTable(TableRecord table, string name) =>
        table.Path.Split('/').Last().Split('.')[0] == name;

    public static int? Integer(JsonNode? value) =>
        RecordValues.Integer(value) is { } number && number is >= int.MinValue and <= int.MaxValue
            ? (int)number
            : null;

    public static int? EnumIndex(CatalogContext context, JsonNode? value)
    {
        var name = RecordValues.EnumValue(value);
        if (name is null)
        {
            return Integer(value);
        }
        var separator = name.IndexOf("::", StringComparison.Ordinal);
        if (separator < 0)
        {
            return null;
        }
        var enumName = name[..separator];
        var entryName = name[(separator + 2)..];
        var entry = context
            .Records.Enums.Where(type => ItemIdentity.ClassName(type.Path) == enumName)
            .SelectMany(type => type.Entries)
            .FirstOrDefault(entry => entry.Name == name || entry.Name == entryName);
        return entry?.Value is >= int.MinValue and <= int.MaxValue ? (int)entry.Value : null;
    }

    public static IEnumerable<(string Item, Weight Weight)> Pool(JsonNode? pool)
    {
        var entries = RecordValues.MapEntries(pool).ToArray();
        var total = entries.Sum(pair => RecordValues.Number(pair.Value) ?? 0);
        foreach (var (key, value) in entries)
        {
            if (
                RecordValues.AnyReference(key) is { } item
                && RecordValues.Number(value) is > 0 and var weight
            )
            {
                yield return (item, new Weight(weight, total));
            }
        }
    }

    public static IReadOnlyList<string> Notes(JsonObject properties, params string[] fields) =>
        fields
            .Where(field => properties[field] is not null)
            .Select(field =>
                field
                + ": "
                + (
                    RecordValues.Number(properties[field])?.ToString(CultureInfo.InvariantCulture)
                    ?? RecordValues.Text(properties[field])
                    ?? RecordValues.String(properties[field])
                    ?? RecordValues.EnumDisplay(properties[field])
                    ?? RecordValues.EnumValue(properties[field])
                    ?? properties[field]!.ToJsonString()
                )
            )
            .ToArray();

    public static IEnumerable<string> References(JsonNode? node)
    {
        if (RecordValues.AnyReference(node) is { } reference)
        {
            yield return reference;
        }
        else if (node is JsonObject obj)
        {
            foreach (var child in obj.SelectMany(pair => References(pair.Value)))
            {
                yield return child;
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var child in array.SelectMany(References))
            {
                yield return child;
            }
        }
    }
}
