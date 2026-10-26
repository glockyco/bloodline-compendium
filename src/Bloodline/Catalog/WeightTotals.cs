using System.Text.Json.Nodes;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

/// <summary>An item's raw weight and the total weight of the pool it is drawn from.</summary>
internal sealed record Weight(double Value, double Total);

internal static class WeightTotals
{
    public static double Sum(IEnumerable<JsonNode?> values)
    {
        decimal total = 0;
        foreach (var node in values)
        {
            // Parsed JSON retains decimal weights exactly. Summing doubles adds noise to their displayed total.
            total +=
                node is JsonValue value && value.TryGetValue<decimal>(out var number)
                    ? number
                    : (decimal)(RecordValues.Number(node) ?? 0);
        }
        return (double)total;
    }

    public static double? Chance(Weight weight) =>
        weight.Total > 0 && weight.Value >= 0 && weight.Value <= weight.Total
            ? weight.Value / weight.Total
            : null;
}
