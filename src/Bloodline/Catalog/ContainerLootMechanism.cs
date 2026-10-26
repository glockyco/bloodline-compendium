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
            if (name.Equals("Loot", StringComparison.OrdinalIgnoreCase))
            {
                name = LootRecords.Name(
                    context.Classes[path].Properties,
                    "Object Name",
                    path,
                    notes
                );
                if (name.Equals("Loot", StringComparison.OrdinalIgnoreCase))
                {
                    name = "Loot container";
                    notes.Add(LootRecords.UnnamedNote);
                }
            }
            var details = new ContainerDetails(
                RecordValues.Number(properties["Spawn Chance"]),
                RecordValues.Integer(properties["Min amount of loot items"]) is { } min
                && RecordValues.Integer(properties["Max amount of loot items"]) is { } max
                    ? new Amount(checked((int)min), checked((int)max))
                    : null
            );
            notes.AddRange(details.Notes);
            var pool = SelectPool(properties, notes, enumValues);
            if (RecordValues.Boolean(properties["Use Specific Loot Table?"]) == true)
            {
                var rowName = RecordValues.String(properties["Loot Table Name"]);
                var row = context
                    .Records.Tables.Where(table =>
                        ItemIdentity.ClassName(table.Path) == "PresetLootTables_DT"
                    )
                    .SelectMany(table => table.Rows)
                    .FirstOrDefault(row =>
                        string.Equals(row.Name, rowName, StringComparison.OrdinalIgnoreCase)
                    );
                if (row is null)
                    continue;
                pool = row.Values["ContainerLootTable"];
                if (
                    RecordValues.Text(row.Values["ContainerName"]) is { } presetName
                    && !string.IsNullOrWhiteSpace(presetName)
                )
                {
                    name = LootRecords.DisplayName(presetName);
                    notes.Remove(LootRecords.UnnamedNote);
                }
                notes.Add($"{LootLabel(row.Name)} loot.");
            }
            foreach (
                var source in LootRecords.Pool(
                    context,
                    pool,
                    SourceKind.ContainerLoot,
                    name,
                    places,
                    notes,
                    "Chance per pick",
                    details.Picks
                )
            )
                yield return source with
                {
                    Container = details,
                };
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
            var maximum = RecordValues.Integer(properties["Max Spawn Amount"]);
            var details = maximum is >= 0 and <= int.MaxValue
                ? new ContainerDetails(
                    null,
                    new Amount((int)maximum.Value, (int)maximum.Value),
                    Loose: true
                )
                : null;
            if (details is not null)
            {
                notes.AddRange(details.Notes);
            }
            else
            {
                notes.Add("The number of loose loot picks is unknown.");
            }
            JsonNode? pool = properties["Loot Table"];
            if (
                preset is not null
                && !preset.Equals("None", StringComparison.OrdinalIgnoreCase)
                && !preset.Equals("Generic", StringComparison.OrdinalIgnoreCase)
            )
            {
                var row = context
                    .Records.Tables.Where(table =>
                        ItemIdentity.ClassName(table.Path) == "PresetLootTables_DT"
                    )
                    .SelectMany(table => table.Rows)
                    .FirstOrDefault(row =>
                        string.Equals(row.Name, preset, StringComparison.OrdinalIgnoreCase)
                    );
                if (row is null)
                    continue;
                pool = row.Values["ContainerLootTable"];
                if (
                    RecordValues.Text(row.Values["ContainerName"]) is { } presetName
                    && !string.IsNullOrWhiteSpace(presetName)
                )
                    name = LootRecords.DisplayName(presetName);
                notes.Add($"{LootLabel(row.Name)} loot.");
            }
            else if (
                RecordValues.Boolean(properties["Use Preset Table?"]) == true
                && string.Equals(preset, "Generic", StringComparison.OrdinalIgnoreCase)
            )
            {
                pool = properties["Generic Loot Table"];
                notes.Add("General dungeon loot.");
            }
            foreach (
                var source in LootRecords.Pool(
                    context,
                    pool,
                    SourceKind.ContainerLoot,
                    name,
                    places,
                    notes,
                    "Chance per pick",
                    details?.Picks
                )
            )
                yield return source with
                {
                    Container = details,
                };
        }
    }

    private static string LootLabel(string name)
    {
        var label = name switch
        {
            "WeaponCrateASKIL" => "Askillian weapon crate",
            "NecroScriptPile" => "Necropolis script pile",
            "GeneralBasicWeapons" => "Basic weapons",
            _ => ItemIdentity.Readable(name),
        };
        return char.ToUpperInvariant(label[0]) + label[1..].ToLowerInvariant();
    }

    private static JsonNode? SelectPool(
        JsonObject properties,
        List<string> notes,
        IReadOnlyDictionary<string, long> enumValues
    )
    {
        if (RecordValues.Boolean(properties["Use Custom Loot Table"]) == true)
        {
            notes.Add("Loot chosen specifically for this container.");
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
            field switch
            {
                "Loot Table - Towns" => "Town loot.",
                "Loot Table - Dungeon" => "Dungeon loot.",
                "Loot Table - Minolurk Fort" => "Minolurk fort loot.",
                "Loot Table - Barrel DRY" => "Dry barrel loot.",
                "Coffin Loot" => "Coffin loot.",
                "Urn Loot" => "Urn loot.",
                "Bandit Loot Table" => "Bandit loot.",
                "Spear Loot Table - DWARVEN" => "Rare dwarven spear loot.",
                "Hammer or Mace Loot Table Dwarven Dungeon RARE" =>
                    "Rare dwarven hammer or mace loot.",
                "Dagger Loot Table - Dwarven Dungeon RARE" => "Rare dwarven dagger loot.",
                "Axe Loot Table -Dwarven RARE" => "Rare dwarven axe loot.",
                "Sword Loot Table - Dwarven Dungeon RARE" => "Rare dwarven sword loot.",
                "Undead Mace Dungeon Loot Table" => "Undead dungeon mace loot.",
                "Undead Sword Loot Table Undead Dungeon" => "Undead dungeon sword loot.",
                _ =>
                    "The possible contents are known, but why this container uses this set of items is unknown.",
            }
        );
        if (lootType == 5 || RecordValues.Boolean(properties["Add Custom Loot to Table"]) != true)
            return pool;
        notes.Add("Loot chosen specifically for this container replaces matching standard loot.");
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
        "The exact name is unknown. A descriptive name is shown instead.";

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
        var className = ItemIdentity.ClassName(path);
        var length = className.Length;
        while (length > 0 && char.IsAsciiDigit(className[length - 1]))
            length--;
        className = className[..length];
        if (
            className.EndsWith("_Base", StringComparison.Ordinal)
            || className.EndsWith("_Idle", StringComparison.Ordinal)
        )
            className = className[..^5];
        if (className.StartsWith("UneadZombie", StringComparison.Ordinal))
            className = "UndeadZombie" + className["UneadZombie".Length..];
        return ItemIdentity.Readable(className);
    }

    internal static string DisplayName(string name) =>
        name.Any(char.IsWhiteSpace) ? name : ItemIdentity.Readable(name);

    internal static IEnumerable<ItemSource> Pool(
        CatalogContext context,
        JsonNode? pool,
        SourceKind kind,
        string name,
        IReadOnlyList<Place> places,
        IReadOnlyList<string> notes,
        string? chanceBasis = null,
        Amount? picks = null
    )
    {
        var entries = RecordValues.MapEntries(pool).ToArray();
        var total = WeightTotals.Sum(entries.Select(entry => entry.Value));
        var noDrop = WeightTotals.Sum(
            entries.Where(entry => entry.Key is null).Select(entry => entry.Value)
        );
        if (noDrop > 0)
            notes =
            [
                .. notes,
                "A pick may give nothing. Empty outcomes are included when comparing the chances.",
            ];
        foreach (var (key, value) in entries)
        {
            if (
                RecordValues.AnyReference(key) is not { } item
                || RecordValues.Number(value) is not > 0
                || total <= 0
            )
                continue;
            var weight = new Weight(RecordValues.Number(value)!.Value, total);
            var chance = chanceBasis is null ? null : WeightTotals.Chance(weight);
            yield return new ItemSource(
                item,
                new Source(
                    kind,
                    name,
                    places,
                    null,
                    Quantity(context, item),
                    weight,
                    chance,
                    [],
                    notes
                )
                {
                    ChanceBasis = chance is null ? null : chanceBasis,
                    Picks = picks,
                }
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
