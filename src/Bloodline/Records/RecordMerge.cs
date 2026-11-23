using System.Text.Json.Nodes;

namespace Bloodline.Records;

/// <summary>
/// Unreal stores class defaults, placed actors and instanced objects as overrides of their archetype.
/// Overlaying merges structs field by field and applies a map that carries removed keys as a delta of the base map, as
/// Unreal loads it. It replaces every other value as a whole, so an explicit zero, false, null or empty array in the
/// overlay wins over the base value.
/// </summary>
internal static class RecordMerge
{
    /// <summary>A new value with <paramref name="overlay"/> applied over <paramref name="baseValue"/>.</summary>
    public static JsonNode? Overlay(JsonNode? baseValue, JsonNode? overlay)
    {
        if (RecordValues.IsStruct(baseValue) && RecordValues.IsStruct(overlay))
        {
            return OverlayProperties(baseValue!.AsObject(), overlay!.AsObject());
        }
        if (
            overlay is JsonObject delta
            && delta[RecordFormat.RemovedKeys] is JsonArray removed
            && baseValue is JsonObject baseMap
            && baseMap[RecordFormat.Map] is JsonArray baseEntries
        )
        {
            return MapDelta(baseMap, baseEntries, delta, removed);
        }
        return overlay?.DeepClone();
    }

    /// <summary>
    /// The base map without the removed keys, with each entry of the delta replacing the base entry of its key in place
    /// or following the base entries. A base that is itself a delta stays one, keeping both sets of removed keys.
    /// </summary>
    private static JsonObject MapDelta(
        JsonObject baseMap,
        JsonArray baseEntries,
        JsonObject delta,
        JsonArray removed
    )
    {
        static string Key(JsonNode? entry) => entry?[0]?.ToJsonString() ?? "null";
        var changes = (delta[RecordFormat.Map] as JsonArray ?? [])
            .GroupBy(Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);
        var dropped = removed
            .Select(key => key?.ToJsonString() ?? "null")
            .ToHashSet(StringComparer.Ordinal);
        var entries = new JsonArray();
        var placed = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in baseEntries)
        {
            var key = Key(entry);
            if (changes.TryGetValue(key, out var change))
            {
                entries.Add(change!.DeepClone());
                placed.Add(key);
            }
            else if (!dropped.Contains(key))
            {
                entries.Add(entry?.DeepClone());
            }
        }
        foreach (var (key, change) in changes)
        {
            if (!placed.Contains(key))
                entries.Add(change!.DeepClone());
        }
        var merged = new JsonObject { [RecordFormat.Map] = entries };
        if (baseMap[RecordFormat.RemovedKeys] is JsonArray baseRemoved)
        {
            var kept = entries.Select(Key).ToHashSet(StringComparer.Ordinal);
            merged[RecordFormat.RemovedKeys] = new JsonArray([
                .. baseRemoved
                    .Concat(removed)
                    .Where(key => !kept.Contains(key?.ToJsonString() ?? "null"))
                    .DistinctBy(key => key?.ToJsonString() ?? "null", StringComparer.Ordinal)
                    .Select(key => key?.DeepClone()),
            ]);
        }
        return merged;
    }

    /// <summary>A new property set with every property of <paramref name="overlay"/> applied over a copy of <paramref name="baseProperties"/>.</summary>
    public static JsonObject OverlayProperties(JsonObject baseProperties, JsonObject overlay)
    {
        var merged = (JsonObject)baseProperties.DeepClone();
        foreach (var (name, value) in overlay)
        {
            merged[name] = Overlay(merged[name], value);
        }
        return merged;
    }
}
