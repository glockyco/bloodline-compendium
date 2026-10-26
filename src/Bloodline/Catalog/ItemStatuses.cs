using System.Text.Json.Nodes;
using Bloodline.Code;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

/// <summary>
/// Whether a player can get an item. An item without a source is out of normal play when every record that names
/// it lies in an excluded test level and no game code names it, and has no known source otherwise.
/// </summary>
internal sealed class ItemStatuses
{
    private readonly Dictionary<string, HashSet<string>> origins = new(
        StringComparer.OrdinalIgnoreCase
    );
    private readonly IReadOnlySet<string> excludedLevels;

    public ItemStatuses(CatalogRecords records, IReadOnlySet<string> excludedLevels)
    {
        this.excludedLevels = excludedLevels;
        foreach (var level in records.Levels)
        foreach (var actor in level.Actors)
            Collect(actor.Properties, level.Path);
        foreach (var type in records.Classes)
            Collect(type.Properties, type.Path);
        foreach (var table in records.Tables)
        foreach (var row in table.Rows)
            Collect(row.Values, table.Path);
        foreach (var dialogue in records.Dialogues)
        {
            Collect(dialogue.Properties, dialogue.Path);
            foreach (var inner in dialogue.Objects)
                Collect(inner.Properties, dialogue.Path);
        }
        foreach (
            var reference in (records.Code?.References ?? []).Where(reference =>
                reference.Target.Kind == CodeTargetKind.Object
            )
        )
        foreach (var reader in reference.Readers)
            Add(reference.Target.Path, reader.Split(':')[0]);
    }

    public ItemStatus Of(string item, int sources)
    {
        if (sources > 0)
            return ItemStatus.Obtainable;
        // The item's own class and its own code name it without making it obtainable.
        var package = item.Split('.')[0];
        var others = origins.TryGetValue(item, out var found)
            ? found
                .Where(origin =>
                    !string.Equals(origin, item, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(origin, package, StringComparison.OrdinalIgnoreCase)
                )
                .ToArray()
            : [];
        return others.Length > 0 && others.All(excludedLevels.Contains)
            ? ItemStatus.NotInNormalPlay
            : ItemStatus.NoKnownSource;
    }

    private void Collect(JsonNode? node, string origin)
    {
        switch (node)
        {
            case JsonObject value:
                if (
                    value["$ref"] is JsonValue reference
                    && reference.TryGetValue<string>(out var path)
                )
                    Add(path, origin);
                foreach (var (_, child) in value)
                    Collect(child, origin);
                break;
            case JsonArray values:
                foreach (var child in values)
                    Collect(child, origin);
                break;
        }
    }

    private void Add(string path, string origin)
    {
        if (!origins.TryGetValue(path, out var set))
            origins.Add(path, set = new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        set.Add(origin);
    }
}
