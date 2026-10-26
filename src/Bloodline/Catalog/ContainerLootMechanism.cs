using System.Globalization;
using System.Text.Json.Nodes;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed class ContainerLootMechanism : ISourceMechanism
{
    internal const string Root =
        "/Game/BloodlinesAlpha/Interactables/LootableObject.LootableObject_C";
    internal const string LooseRoot =
        "/Game/Overworld/Villages/VillageBuildings/GenericDungeon_StructuresAndEvents/Lootables/Dungeon_Generic_Lootable_Item_Placer.Dungeon_Generic_Lootable_Item_Placer_C";

    public IEnumerable<ItemSource> FindSources(CatalogContext context)
    {
        var enumValues = context
            .Records.Enums.SelectMany(type => type.Entries)
            .ToDictionary(entry => entry.Name, entry => entry.Value, StringComparer.Ordinal);
        foreach (
            var (path, properties, places) in LootRecords.Instances(
                context,
                type => type.Path == Root || type.Ancestors.Contains(Root)
            )
        )
        {
            if (RecordValues.Number(properties["Spawn Chance"]) is <= 0)
                continue;
            if (RecordValues.Boolean(properties["Use Entire Data Table?"]) == true)
                continue;
            var notes = new List<string>();
            var name = LootRecords.Name(properties, "Object Name", path, notes);
            if (RecordValues.Number(properties["Spawn Chance"]) is { } spawn)
                notes.Add(
                    $"Container spawn chance: {spawn.ToString(CultureInfo.InvariantCulture)}."
                );
            if (
                RecordValues.Integer(properties["Min amount of loot items"]) is { } min
                && RecordValues.Integer(properties["Max amount of loot items"]) is { } max
            )
                notes.Add(
                    $"Loot count settings: {min} to {max}. The consumer rolls from zero through a newly sampled inclusive bound."
                );
            var pool = SelectPool(properties, notes, enumValues);
            if (RecordValues.Boolean(properties["Use Specific Loot Table?"]) == true)
            {
                var rowName = RecordValues.String(properties["Loot Table Name"]);
                var row = context
                    .Records.Tables.Where(table =>
                        ItemIdentity.ClassName(table.Path) == "PresetLootTables_DT"
                    )
                    .SelectMany(table => table.Rows)
                    .FirstOrDefault(row => row.Name == rowName);
                if (row is null)
                    continue;
                pool = row.Values["ContainerLootTable"];
                if (
                    RecordValues.Text(row.Values["ContainerName"]) is { } presetName
                    && !string.IsNullOrWhiteSpace(presetName)
                )
                {
                    name = presetName;
                    notes.Remove(LootRecords.UnnamedNote);
                }
                notes.Add($"Preset loot table: {row.Name}.");
            }
            foreach (
                var source in LootRecords.Pool(
                    context,
                    pool,
                    SourceKind.ContainerLoot,
                    name,
                    places,
                    notes
                )
            )
                yield return source;
        }
        foreach (
            var (path, properties, places) in LootRecords.Instances(
                context,
                type => type.Path == LooseRoot || type.Ancestors.Contains(LooseRoot)
            )
        )
        {
            if (RecordValues.Integer(properties["Max Spawn Amount"]) is <= 0)
                continue;
            var preset = RecordValues.String(properties["Preset Table Name"]);
            var name = "Dungeon loose loot";
            var notes = new List<string>
            {
                "Appears as loose loot rather than inside a container.",
            };
            if (RecordValues.Integer(properties["Min Spawn Amount"]) is { } min)
                notes.Add(
                    $"Minimum spawn amount setting: {min}. The consumer uses the maximum setting for its loop bound."
                );
            if (RecordValues.Integer(properties["Max Spawn Amount"]) is { } max)
                notes.Add($"Maximum spawn amount: {max}.");
            JsonNode? pool = properties["Loot Table"];
            if (preset is not null and not "None" and not "Generic")
            {
                var row = context
                    .Records.Tables.Where(table =>
                        ItemIdentity.ClassName(table.Path) == "PresetLootTables_DT"
                    )
                    .SelectMany(table => table.Rows)
                    .FirstOrDefault(row => row.Name == preset);
                if (row is null)
                    continue;
                pool = row.Values["ContainerLootTable"];
                if (
                    RecordValues.Text(row.Values["ContainerName"]) is { } presetName
                    && !string.IsNullOrWhiteSpace(presetName)
                )
                    name = presetName;
                notes.Add($"Preset loot table: {row.Name}.");
            }
            else if (
                RecordValues.Boolean(properties["Use Preset Table?"]) == true
                && preset == "Generic"
            )
            {
                pool = properties["Generic Loot Table"];
                notes.Add("Uses the generic loot pool.");
            }
            foreach (
                var source in LootRecords.Pool(
                    context,
                    pool,
                    SourceKind.ContainerLoot,
                    name,
                    places,
                    notes
                )
            )
                yield return source;
        }
    }

    private static JsonNode? SelectPool(
        JsonObject properties,
        List<string> notes,
        IReadOnlyDictionary<string, long> enumValues
    )
    {
        if (RecordValues.Boolean(properties["Use Custom Loot Table"]) == true)
        {
            notes.Add("Uses the custom loot pool.");
            return properties["Custom Loot Table"];
        }
        var lootType = EnumNumber(properties["Loot Type"]);
        var field = lootType switch
        {
            0 => "Loot Table - Towns",
            1 => "Loot Table - Dungeon",
            3 => "Loot Table - Minolurk Fort",
            6 => "Loot Table - Barrel DRY",
            8 => "Coffin Loot",
            9 => "Urn Loot",
            10 => "Bandit Loot Table",
            // The rare generator sets Sword first. Later enabled weapon flags overwrite its pool.
            5 => EnumNumber(properties["Randomized Loot Style"]) switch
            {
                0 when RecordValues.Boolean(properties["Spear"]) == true =>
                    "Spear Loot Table - DWARVEN",
                0 when RecordValues.Boolean(properties["Hammer or Mace"]) == true =>
                    "Hammer or Mace Loot Table Dwarven Dungeon RARE",
                0 when RecordValues.Boolean(properties["Dagger"]) == true =>
                    "Dagger Loot Table - Dwarven Dungeon RARE",
                0 when RecordValues.Boolean(properties["Axe"]) == true =>
                    "Axe Loot Table -Dwarven RARE",
                0 => "Sword Loot Table - Dwarven Dungeon RARE",
                3 when RecordValues.Boolean(properties["Hammer or Mace"]) == true =>
                    "Undead Mace Dungeon Loot Table",
                3 => "Undead Sword Loot Table Undead Dungeon",
                _ => null,
            },
            _ => null,
        };
        var pool = field is null ? properties["Selected Loot Table"] : properties[field];
        notes.Add(
            field is null
                ? "Uses the stored selected loot pool. Pool selection is not recorded."
                : $"Selected pool: {field}."
        );
        if (lootType == 5 || RecordValues.Boolean(properties["Add Custom Loot to Table"]) != true)
            return pool;
        notes.Add("Custom entries replace matching entries in the selected pool.");
        var entries = RecordValues.MapEntries(pool).ToList();
        foreach (var entry in RecordValues.MapEntries(properties["Custom Loot Table"]))
        {
            entries.RemoveAll(existing => JsonNode.DeepEquals(existing.Key, entry.Key));
            entries.Add(entry);
        }
        return new JsonObject
        {
            ["$map"] = new JsonArray(
                entries
                    .Select(entry =>
                        (JsonNode)new JsonArray(entry.Key?.DeepClone(), entry.Value?.DeepClone())
                    )
                    .ToArray()
            ),
        };

        long? EnumNumber(JsonNode? node) =>
            RecordValues.EnumValue(node) is { } name && enumValues.TryGetValue(name, out var value)
                ? value
                : null;
    }
}

internal static class LootRecords
{
    internal const string UnnamedNote =
        "No display name is recorded. The source name is derived from its class.";

    internal static IEnumerable<(
        string Path,
        JsonObject Properties,
        IReadOnlyList<Place> Places
    )> Instances(CatalogContext context, Func<ClassRecord, bool> matches)
    {
        var byClass = context.Placements.ToLookup(
            placement => placement.Actor.Class,
            StringComparer.OrdinalIgnoreCase
        );
        foreach (
            var type in context.Records.Classes.Where(type =>
                matches(type) && !context.IsExcludedClass(type.Path)
            )
        )
        {
            var placements = byClass[type.Path];
            if (!placements.Any())
                yield return (type.Path, type.Properties, []);
            foreach (var placement in placements)
                yield return (
                    type.Path,
                    placement.Properties,
                    [context.Places.Get(placement.Level)]
                );
        }
    }

    internal static string Name(
        JsonObject properties,
        string field,
        string path,
        List<string> notes
    )
    {
        var name = RecordValues.Text(properties[field]) ?? RecordValues.String(properties[field]);
        if (!string.IsNullOrWhiteSpace(name))
            return name;
        notes.Add(UnnamedNote);
        return ItemIdentity.Readable(ItemIdentity.ClassName(path));
    }

    internal static IEnumerable<ItemSource> Pool(
        CatalogContext context,
        JsonNode? pool,
        SourceKind kind,
        string name,
        IReadOnlyList<Place> places,
        IReadOnlyList<string> notes
    )
    {
        var entries = RecordValues.MapEntries(pool).ToArray();
        var total = entries.Sum(entry => RecordValues.Number(entry.Value) ?? 0);
        var noDrop = entries
            .Where(entry => entry.Key is null)
            .Sum(entry => RecordValues.Number(entry.Value) ?? 0);
        if (noDrop > 0)
            notes =
            [
                .. notes,
                $"No-drop outcome weight: {noDrop.ToString(CultureInfo.InvariantCulture)}.",
            ];
        foreach (var (key, value) in entries)
        {
            if (
                RecordValues.AnyReference(key) is not { } item
                || RecordValues.Number(value) is not > 0
                || total <= 0
            )
                continue;
            yield return new ItemSource(
                item,
                new Source(
                    kind,
                    name,
                    places,
                    null,
                    Quantity(context, item),
                    new Weight(RecordValues.Number(value)!.Value, total),
                    null,
                    [],
                    notes
                )
            );
        }
    }

    internal static Amount? Quantity(CatalogContext context, string item)
    {
        var quantity = context.Classes.TryGetValue(item, out var type)
            ? RecordValues.Integer(RecordValues.At(type.Properties, "ItemInfo.Quantity"))
            : null;
        return quantity is > 0 and <= int.MaxValue
            ? new Amount((int)quantity.Value, (int)quantity.Value)
            : null;
    }
}
