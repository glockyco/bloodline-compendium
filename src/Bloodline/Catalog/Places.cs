using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed partial class Places
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

    /// <summary>
    /// The map the game opens when the player makes camp on the world map, by the terrain tag nearest the camp, as
    /// <c>/Game/TopDownCharacter_Working:Get Location Tag Maps</c> chooses it. A player knows such a map by its
    /// terrain, so it is named after the tag.
    /// </summary>
    private static readonly Dictionary<string, string> CampMaps = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["Generic_PineForest_Area"] = "Pine Forest camp",
        ["Generic_PineForest_Area2"] = "Pine Forest camp",
        ["Generic_GrassyPlains"] = "Grassy Plains camp",
        ["Generic_GrassyPlains_02"] = "Grassy Plains camp",
        ["Generic_BirchForest_Area"] = "Birch Forest camp",
        ["Generic_BirchForest_Area_02"] = "Birch Forest camp",
        ["Generic_RockyHills_Area"] = "Rocky Hills camp",
        ["Generic_SnowPineForest_Area"] = "Snowy Pine Forest camp",
        ["Generic_SnowBirchForest_Area"] = "Snowy Birch Forest camp",
        ["Generic_AutumnBirch_Area"] = "Autumnal Birch Forest camp",
        ["Generic_SnowyPlains"] = "Snowy Plains camp",
        ["Generic_AutumnPlains"] = "Autumnal Plains camp",
        ["Generic_Birch_LumberMill_01"] = "Hostile Logging Camp",
        ["Generic_Dunes"] = "Dunes camp",
        ["Generic_PalmDunes"] = "Palm Dunes camp",
        ["Generic_DesertButtes_Dry"] = "Dry Buttes camp",
        ["Generic_DesertButtes"] = "Palm Buttes camp",
        ["Generic_TundraPlains_Area"] = "Tundra Plains camp",
        ["Generic_RockyTundra_Area"] = "Rocky Tundra camp",
        ["Generic_TundraPineForest_Area"] = "Snowy Forest camp",
        ["Generic_SnowyCliffside_Area"] = "Snowy Cliffside camp",
        ["Generic_SnowyRocks_Area"] = "Snowy Rocks camp",
    };
    private readonly Dictionary<string, EntityReference> places;
    private readonly HashSet<string> named = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyCollection<EntityReference> All => places.Values;
    public int NamedCount => named.Count;

    public bool HasName(string path) => named.Contains(path);

    public EntityReference Get(string path)
    {
        if (!places.TryGetValue(path, out var place))
        {
            place = WorldSourceData.Reference(EntityKind.Place, path, Fallback(path));
            places.Add(path, place);
        }
        return place;
    }

    /// <summary>
    /// The name of a level that nothing in the game names, read from its level name: an event map's "Generic" and
    /// numbering are dropped, and small words are lower case as in the names the game shows, such as "Tomb of the
    /// Betrayer".
    /// </summary>
    internal static string Fallback(string path)
    {
        var name = ItemIdentity.ClassName(path);
        if (name.StartsWith("Generic_", StringComparison.OrdinalIgnoreCase))
        {
            name = TrailingNumber().Replace(name["Generic_".Length..], "");
        }
        var words = ItemIdentity.Readable(name).Split(' ');
        return string.Join(
            ' ',
            words.Select(
                (word, index) =>
                    index > 0 && SmallWords.Contains(word) ? word.ToLowerInvariant() : word
            )
        );
    }

    private static readonly HashSet<string> SmallWords = new(StringComparer.Ordinal)
    {
        "Of",
        "The",
        "And",
        "In",
        "On",
        "At",
        "To",
    };

    [GeneratedRegex(@"_?\d+$")]
    private static partial Regex TrailingNumber();

    public Places(CatalogRecords records, IReadOnlySet<string> excludedLevels)
    {
        places = records.Levels.ToDictionary(
            level => level.Path,
            level => WorldSourceData.Reference(EntityKind.Place, level.Path, Fallback(level.Path)),
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
        foreach (var level in records.Levels)
        {
            if (CampMaps.TryGetValue(ItemIdentity.ClassName(level.Path), out var camp))
            {
                places[level.Path] = WorldSourceData.Reference(EntityKind.Place, level.Path, camp);
                named.Add(level.Path);
            }
        }
        // A part of a place carries the place's level name and an underscore before its own, such as
        // TheMagesCollege_MainHall in TheMagesCollege, and is named after the place and then the part. A colon joins
        // them, because pages list places with commas.
        var byClass = records
            .Levels.GroupBy(
                level => ItemIdentity.ClassName(level.Path),
                StringComparer.OrdinalIgnoreCase
            )
            .ToDictionary(
                group => group.Key,
                group => group.First().Path,
                StringComparer.OrdinalIgnoreCase
            );
        foreach (var level in records.Levels.Where(level => !named.Contains(level.Path)))
        {
            var name = ItemIdentity.ClassName(level.Path);
            var parent = byClass
                .Keys.Where(other =>
                    other.Length < name.Length
                    && name.StartsWith(other, StringComparison.OrdinalIgnoreCase)
                    && name[other.Length] == '_'
                )
                .OrderByDescending(other => other.Length)
                .FirstOrDefault();
            if (parent is null)
            {
                continue;
            }
            var part = Fallback(name[parent.Length..].TrimStart('_'));
            places[level.Path] = WorldSourceData.Reference(
                EntityKind.Place,
                level.Path,
                places[byClass[parent]].Name + ": " + part
            );
            named.Add(level.Path);
        }
    }
}
