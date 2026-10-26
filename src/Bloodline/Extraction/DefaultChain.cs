using System.Text.Json.Nodes;
using Bloodline.Records;

namespace Bloodline.Extraction;

internal sealed record DefaultLayer(JsonObject DeclaredZeroValues, JsonObject Overrides);

internal static class DefaultChain
{
    public static JsonObject Merge(IEnumerable<DefaultLayer> oldestFirst)
    {
        var result = new JsonObject();
        foreach (var layer in oldestFirst)
        {
            result = RecordMerge.OverlayProperties(result, layer.DeclaredZeroValues);
            result = RecordMerge.OverlayProperties(result, layer.Overrides);
        }
        return result;
    }

    public static JsonObject Placement(JsonObject defaults, JsonObject overrides)
    {
        var result = new JsonObject();
        foreach (var (key, value) in overrides)
        {
            result[key] = RecordMerge.Overlay(defaults[key], value);
        }
        return result;
    }
}
