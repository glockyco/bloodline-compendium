using System.Text.Json.Nodes;

namespace Bloodline.Records;

/// <summary>
/// Unreal stores class defaults, placed actors and instanced objects as overrides of their archetype.
/// Overlaying merges structs field by field and replaces every other value as a whole, so an explicit
/// zero, false, null or empty array in the overlay wins over the base value.
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
        return overlay?.DeepClone();
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
