using System.Text.Json.Nodes;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed class Places
{
    private static readonly string[] DisplayFields =
    [
        "Location Name",
        "Village Name",
        "Castle Name",
        "Display Name",
        "LocationName",
        "DisplayName",
    ];
    private static readonly string[] DestinationFields =
    [
        "City Name",
        "Village Map",
        "Level To Load",
        "Level to Load",
        "Level Name",
        "LevelName",
        "Map",
    ];
    private readonly Dictionary<string, EntityReference> places;
    private readonly HashSet<string> named = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyCollection<EntityReference> All => places.Values;
    public int NamedCount => named.Count;

    public bool HasName(string path) => named.Contains(path);

    public EntityReference Get(string path)
    {
        if (!places.TryGetValue(path, out var place))
        {
            place = WorldSourceData.Reference(
                EntityKind.Place,
                path,
                ItemIdentity.Readable(ItemIdentity.ClassName(path))
            );
            places.Add(path, place);
        }
        return place;
    }

    public Places(CatalogRecords records, IReadOnlySet<string> excludedLevels)
    {
        places = records.Levels.ToDictionary(
            level => level.Path,
            level =>
                WorldSourceData.Reference(
                    EntityKind.Place,
                    level.Path,
                    ItemIdentity.Readable(ItemIdentity.ClassName(level.Path))
                ),
            StringComparer.OrdinalIgnoreCase
        );
        var byName = records
            .Levels.GroupBy(
                level => ItemIdentity.ClassName(level.Path),
                StringComparer.OrdinalIgnoreCase
            )
            .ToDictionary(
                group => group.Key,
                group => group.ToArray(),
                StringComparer.OrdinalIgnoreCase
            );
        var classes = records.Classes.ToDictionary(
            item => item.Path,
            StringComparer.OrdinalIgnoreCase
        );
        void Read(JsonObject properties, JsonObject? defaults = null)
        {
            JsonNode? Value(string field) =>
                properties.TryGetPropertyValue(field, out var value) ? value : defaults?[field];
            var display = DisplayFields
                .Select(field => RecordValues.Text(Value(field)))
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
            if (display is not null)
            {
                foreach (var field in DestinationFields)
                {
                    var target =
                        RecordValues.AnyReference(Value(field))
                        ?? RecordValues.String(Value(field));
                    if (target is null)
                    {
                        continue;
                    }
                    if (places.TryGetValue(target, out var known))
                    {
                        places[target] = known with { Name = display };
                        named.Add(target);
                        continue;
                    }
                    var matches = byName.GetValueOrDefault(ItemIdentity.ClassName(target)) ?? [];
                    foreach (var level in matches)
                    {
                        places[level.Path] = WorldSourceData.Reference(
                            EntityKind.Place,
                            level.Path,
                            display
                        );
                        named.Add(level.Path);
                    }
                }
            }
        }
        foreach (var item in records.Classes.OrderBy(item => item.Path, StringComparer.Ordinal))
        {
            Read(item.Properties);
        }
        foreach (var table in records.Tables)
        {
            foreach (var row in table.Rows)
            {
                Read(row.Values);
            }
        }
        foreach (var level in records.Levels.Where(level => !excludedLevels.Contains(level.Path)))
        {
            foreach (var actor in level.Actors)
            {
                Read(actor.Properties, classes.GetValueOrDefault(actor.Class)?.Properties);
            }
        }
        // Streamed sublevels share the containing location when every named parent agrees.
        bool changed;
        do
        {
            changed = false;
            foreach (var level in records.Levels.Where(level => !named.Contains(level.Path)))
            {
                var parents = records
                    .Levels.Where(parent =>
                        named.Contains(parent.Path)
                        && parent.Streaming.Contains(level.Path, StringComparer.OrdinalIgnoreCase)
                    )
                    .Select(parent => places[parent.Path].Name)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
                if (parents.Length == 1)
                {
                    places[level.Path] = WorldSourceData.Reference(
                        EntityKind.Place,
                        level.Path,
                        parents[0]
                    );
                    named.Add(level.Path);
                    changed = true;
                }
            }
        } while (changed);
    }
}
