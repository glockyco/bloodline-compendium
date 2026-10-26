using System.Text.Json.Nodes;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed class Places
{
    private readonly Dictionary<string, Place> places;
    private readonly HashSet<string> named = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyCollection<Place> All => places.Values;
    public int NamedCount => named.Count;

    public bool HasName(string path) => named.Contains(path);

    public Place Get(string path) =>
        places.GetValueOrDefault(path)
        ?? new Place(path, ItemIdentity.Readable(ItemIdentity.ClassName(path)));

    public Places(CatalogRecords records, IReadOnlySet<string> excludedLevels)
    {
        places = records.Levels.ToDictionary(
            level => level.Path,
            level => new Place(
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
        void Read(JsonObject properties)
        {
            var display = new[] { "Location Name", "Display Name", "LocationName", "DisplayName" }
                .Select(field => RecordValues.Text(properties[field]))
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
            if (display is not null)
            {
                foreach (
                    var field in new[]
                    {
                        "City Name",
                        "Village Map",
                        "Level To Load",
                        "Level to Load",
                        "Level Name",
                        "LevelName",
                        "Map",
                    }
                )
                {
                    var target =
                        RecordValues.AnyReference(properties[field])
                        ?? RecordValues.String(properties[field]);
                    if (target is null)
                    {
                        continue;
                    }
                    var matches = places.ContainsKey(target)
                        ?
                        [
                            records.Levels.First(level =>
                                level.Path.Equals(target, StringComparison.OrdinalIgnoreCase)
                            ),
                        ]
                        : byName.GetValueOrDefault(ItemIdentity.ClassName(target)) ?? [];
                    foreach (var level in matches)
                    {
                        places[level.Path] = new Place(level.Path, display);
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
                var properties = classes.TryGetValue(actor.Class, out var type)
                    ? RecordMerge.OverlayProperties(type.Properties, actor.Properties)
                    : actor.Properties;
                Read(properties);
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
                    places[level.Path] = new Place(level.Path, parents[0]);
                    named.Add(level.Path);
                    changed = true;
                }
            }
        } while (changed);
    }
}
