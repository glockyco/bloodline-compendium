using System.Text.Json.Nodes;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal static class WorldSourceData
{
    private static readonly string[] NameFields = ["Full Name", "Name", "ItemInfo.ItemName"];

    public static EntityReference Reference(EntityKind kind, string? key, string name) =>
        new(kind, key, name, null, null);

    /// <summary>Limits a source to a completed settlement building, naming the building so pages can link it.</summary>
    public static RelationCondition BuildingCondition(EntityReference building) =>
        new(
            ConditionKind.Building,
            building,
            null,
            "Requires the completed " + building.Name + "."
        );

    public static EntityReference PlacementEntity(
        CatalogContext context,
        Placement placement,
        EntityKind kind,
        string? name = null
    )
    {
        var className = ItemIdentity.ClassName(placement.Actor.Class);
        // Shared actor classes do not identify their individual speakers.
        var shared =
            kind is EntityKind.Person or EntityKind.Merchant
            && (
                className is "ShopKeeper_Base" or "Civilian_Spawner" or "Guard_Spawner"
                || context.SharedPeopleClasses.Contains(placement.Actor.Class)
            );
        var key = shared
            ? RecordValues.AnyReference(placement.Properties["Dialogue"])
                ?? placement.Level + ":PersistentLevel." + placement.Actor.Name
            : placement.Actor.Class;
        return Reference(kind, key, name ?? Name(placement.Properties, placement.Actor.Class));
    }

    public static EntityReference TableEntity(
        CatalogContext context,
        EntityKind kind,
        string tableName,
        string? rowName,
        string? label = null,
        string nameField = "DisplayName"
    )
    {
        var match = context
            .Records.Tables.Where(table => IsTable(table, tableName))
            .SelectMany(table => table.Rows.Select(row => (Table: table, Row: row)))
            .FirstOrDefault(entry =>
                string.Equals(entry.Row.Name, rowName, StringComparison.OrdinalIgnoreCase)
            );
        return Reference(
            kind,
            match.Row is null ? null : match.Table.Path + ":" + match.Row.Name,
            match.Row is null
                ? label ?? (rowName is null ? "Unknown" : ItemIdentity.Readable(rowName))
                : RecordValues.Text(match.Row.Values[nameField])
                    ?? RecordValues.String(match.Row.Values[nameField])
                    ?? label
                    ?? ItemIdentity.Readable(match.Row.Name)
        );
    }

    public static IReadOnlyList<RelationCondition> Conditions(
        CatalogContext context,
        JsonObject properties,
        string? level = null
    )
    {
        var conditions = new List<RelationCondition>();
        if (
            RecordValues
                .Elements(properties["Tags"])
                .Any(tag => RecordValues.String(tag) == "Halloween Decor")
        )
        {
            // Woodhurst_C:ExecuteUbergraph_Woodhurst destroys tagged actors unless either calendar window is active.
            conditions.Add(
                level == "/Game/Cities/Woodhurst/Woodhurst"
                    ? new(
                        ConditionKind.CalendarEvent,
                        Reference(EntityKind.CalendarEvent, null, "Hallowed Eve"),
                        null,
                        "Available during Hallowed Eve: Truill in the game calendar or October in the real-world calendar."
                    )
                    : new(
                        ConditionKind.Unknown,
                        null,
                        null,
                        "Availability of content tagged \"Halloween Decor\" is unknown."
                    )
            );
        }
        return conditions;
    }

    public static DialogueRequirements DialogueConditions(
        CatalogContext context,
        AssetRecord dialogue,
        InnerObjectRecord grant
    )
    {
        var result = new List<RelationCondition>();
        var unresolved = new List<string>();
        var data = RecordValues.Elements(dialogue.Properties["Data"]).ToArray();
        var nodes = data.Where(node =>
                RecordValues
                    .Elements(RecordValues.At(node, "Events"))
                    .Any(evt =>
                        string.Equals(
                            RecordValues.AnyReference(evt),
                            grant.Path,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
            )
            .ToArray();
        var eventIds = nodes
            .Select(node => RecordValues.Integer(RecordValues.At(node, "id")))
            .OfType<long>()
            .ToHashSet();
        var visited = new HashSet<long>(eventIds);
        var pending = new Queue<long>(eventIds);
        var predecessors = new Dictionary<long, List<JsonNode?>>();
        foreach (var node in data)
        foreach (var link in RecordValues.Elements(RecordValues.At(node, "Links")))
        {
            if (RecordValues.Integer(link) is not { } id)
                continue;
            if (!predecessors.TryGetValue(id, out var previous))
                predecessors.Add(id, previous = []);
            previous.Add(node);
        }
        while (pending.TryDequeue(out var id))
        {
            if (!predecessors.TryGetValue(id, out var previous))
                continue;
            foreach (var node in previous)
            {
                if (
                    RecordValues.Integer(RecordValues.At(node, "id")) is not { } parent
                    || !visited.Add(parent)
                )
                    continue;
                pending.Enqueue(parent);
                if (RecordValues.Elements(RecordValues.At(node, "Conditions")).Any())
                    unresolved.Add(
                        "Earlier lines of the conversation have checks whose path is not read."
                    );
            }
        }
        foreach (var node in nodes)
        {
            if (RecordValues.At(node, "Conditions") is not null and not JsonArray)
                unresolved.Add("The line's checks could not be read.");
        }
        foreach (var node in nodes)
        foreach (var condition in RecordValues.Elements(RecordValues.At(node, "Conditions")))
        {
            var reference = RecordValues.AnyReference(condition);
            var instance = dialogue.Objects.FirstOrDefault(obj =>
                string.Equals(obj.Path, reference, StringComparison.OrdinalIgnoreCase)
            );
            if (instance is null)
            {
                unresolved.Add("A check of the line could not be read: " + reference);
                continue;
            }
            var properties = context.Classes.TryGetValue(instance.Class, out var defaults)
                ? RecordMerge.OverlayProperties(defaults.Properties, instance.Properties)
                : instance.Properties;
            var type = ItemIdentity.ClassName(instance.Class);
            if (type == "PlayerHasEnoughMoney")
            {
                var money = RecordValues.Number(properties["Required Money"]);
                if (money is <= 0)
                    continue;
                result.Add(
                    new(
                        money is null ? ConditionKind.Unknown : ConditionKind.GameState,
                        null,
                        money,
                        money is { } value
                            ? FormattableString.Invariant($"Requires at least {value} gold.")
                            : "Requires enough gold. The amount is unknown."
                    )
                );
            }
            else if (
                type
                is "Quest_is_Active"
                    or "Quest_is_NOT_Active"
                    or "Quest_is_COMPLETED"
                    or "Quest_is_NOT_completed"
            )
            {
                var quest = TableEntity(
                    context,
                    EntityKind.Quest,
                    "DT_UQC_Quests",
                    RecordValues.String(properties["Quest Needed"])
                );
                var state = type switch
                {
                    "Quest_is_Active" => "active",
                    "Quest_is_NOT_Active" => "not active",
                    "Quest_is_COMPLETED" => "completed",
                    _ => "not completed",
                };
                result.Add(
                    new(
                        ConditionKind.Quest,
                        quest,
                        null,
                        $"Requires the quest \"{quest.Name}\" to be {state}."
                    )
                );
            }
            else if (type is "CheckIfDialogueTagIsValid" or "CheckIfDialogueTagIsNOTValid")
            {
                var tag = RecordValues.String(properties["Tag to Find"]);
                var (resolved, shown) = TagCondition(
                    context,
                    dialogue,
                    tag,
                    type == "CheckIfDialogueTagIsValid"
                );
                if (shown is not null)
                    result.Add(shown);
                else if (!resolved)
                    unresolved.Add(
                        $"Dialogue tag {tag ?? "without a name"} is set by no conversation that is read."
                    );
            }
            else if (type is "ObjectivesAreCompleted" or "ObjectivesAreNOTCompleted")
            {
                var quest = TableEntity(
                    context,
                    EntityKind.Quest,
                    "DT_UQC_Quests",
                    RecordValues.String(properties["Quest"])
                );
                var amount = RecordValues.Number(properties["Required Amount"]);
                var objective = RecordValues.String(properties["Objective"]) ?? "unknown";
                result.Add(
                    new(
                        amount is null || objective == "unknown"
                            ? ConditionKind.Unknown
                            : ConditionKind.QuestStage,
                        quest,
                        amount,
                        $"Requires objective \"{objective}\" of \"{quest.Name}\" to be {(type == "ObjectivesAreCompleted" ? "completed" : "incomplete")}."
                    )
                );
            }
            else if (type is "EqualOrGreaterFishingLevel" or "EXACT_FishingLevel")
            {
                var level = RecordValues.Number(properties["Required Fishing Level"]);
                result.Add(
                    new(
                        level is null ? ConditionKind.Unknown : ConditionKind.ProficiencyLevel,
                        null,
                        level,
                        level is null
                            ? "The required fishing level is unknown."
                            : FormattableString.Invariant(
                                $"Requires fishing level {(type == "EXACT_FishingLevel" ? "exactly " : "at least ")}{level}."
                            )
                    )
                );
            }
            else if (type == "ChapterRankCheck")
            {
                var rank = RecordValues.Number(properties["Required Rank"]);
                result.Add(
                    new(
                        rank is null ? ConditionKind.Unknown : ConditionKind.GuildRank,
                        null,
                        rank,
                        rank is null
                            ? "The required chapter rank is unknown."
                            : FormattableString.Invariant($"Requires chapter rank {rank}.")
                    )
                );
            }
            else if (type == "CheckIfIsHallowedEve")
            {
                var required = RecordValues.Boolean(properties["Required Result"]);
                result.Add(
                    required is null
                        ? new(
                            ConditionKind.Unknown,
                            null,
                            null,
                            "The required Hallowed Eve calendar state is unknown."
                        )
                        : new(
                            ConditionKind.CalendarEvent,
                            Reference(EntityKind.CalendarEvent, null, "Hallowed Eve"),
                            required.Value ? 1 : 0,
                            required.Value
                                ? "Available during Hallowed Eve."
                                : "Available outside Hallowed Eve."
                        )
                );
            }
            else
            {
                unresolved.Add("Unread dialogue check " + type + ".");
            }
        }
        return new(result.Distinct().ToArray(), unresolved.Distinct().ToArray());
    }

    // A tag check names the conversation that sets the tag. A conversation that sets its own tag meets a check
    // that it is set on the way, and a check that it is unset is how it gives something only once.
    private static (bool Resolved, RelationCondition? Shown) TagCondition(
        CatalogContext context,
        AssetRecord dialogue,
        string? tag,
        bool set
    )
    {
        if (tag is null)
            return (false, null);
        var setters = context.Tags.Setters(tag);
        if (setters.Count == 0)
            return (false, null);
        if (setters.Contains(DialogueTags.Package(dialogue.Path)))
            return set
                ? (true, null)
                : (true, new(ConditionKind.GameState, null, null, "Given only once."));
        var names = setters
            .SelectMany(context.Tags.Speakers)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (names.Length == 0)
            return (false, null);
        var who =
            names.Length <= 2 ? string.Join(" or ", names) : names[0] + " or another character";
        return (
            true,
            new(
                ConditionKind.GameState,
                null,
                null,
                set ? $"Requires talking to {who} first." : $"Only until you talk to {who}."
            )
        );
    }

    public static bool IsClass(CatalogContext context, string path, string name) =>
        ItemIdentity.ClassName(path) == ItemIdentity.ClassName(name)
        || (
            context.Classes.TryGetValue(path, out var type)
            && type.Ancestors.Any(parent =>
                ItemIdentity.ClassName(parent) == ItemIdentity.ClassName(name)
            )
        );

    public static string Name(JsonObject properties, string path)
    {
        foreach (var field in NameFields)
        {
            var value = RecordValues.At(properties, field);
            var name = RecordValues.Text(value) ?? RecordValues.String(value);
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
        }
        return ItemIdentity.ClassName(path) switch
        {
            "ShopKeeper_Base" => "Shopkeeper",
            "Civilian_Spawner" => "Civilian",
            "Guard_Spawner" => "Guard",
            "CompanyOfThievesScoutSpawner" or "CompanyOfTheives_Thief_Scout" =>
                "Company of Thieves scout",
            var name => ItemIdentity.Readable(name),
        };
    }

    public static bool IsTable(TableRecord table, string name) =>
        table.Path.Split('/').Last().Split('.')[0] == name;

    public static int? Integer(JsonNode? value) =>
        RecordValues.Integer(value) is { } number && number is >= int.MinValue and <= int.MaxValue
            ? (int)number
            : null;

    public static int? EnumIndex(CatalogContext context, JsonNode? value)
    {
        var name = RecordValues.EnumValue(value);
        if (name is null)
        {
            return Integer(value);
        }
        var separator = name.IndexOf("::", StringComparison.Ordinal);
        if (separator < 0)
        {
            return null;
        }
        var enumName = name[..separator];
        var entryName = name[(separator + 2)..];
        var entry = context
            .Records.Enums.Where(type => ItemIdentity.ClassName(type.Path) == enumName)
            .SelectMany(type => type.Entries)
            .FirstOrDefault(entry => entry.Name == name || entry.Name == entryName);
        return entry?.Value is >= int.MinValue and <= int.MaxValue ? (int)entry.Value : null;
    }

    public static IEnumerable<(string Item, Weight Weight)> Pool(JsonNode? pool)
    {
        var entries = RecordValues.MapEntries(pool).ToArray();
        var total = WeightTotals.Sum(entries.Select(pair => pair.Value));
        foreach (var (key, value) in entries)
        {
            if (
                RecordValues.AnyReference(key) is { } item
                && RecordValues.Number(value) is > 0 and var weight
            )
            {
                yield return (item, new Weight(weight, total));
            }
        }
    }

    public static IEnumerable<string> References(JsonNode? node)
    {
        if (RecordValues.AnyReference(node) is { } reference)
        {
            yield return reference;
        }
        else if (node is JsonObject obj)
        {
            foreach (var child in obj.SelectMany(pair => References(pair.Value)))
            {
                yield return child;
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var child in array.SelectMany(References))
            {
                yield return child;
            }
        }
    }
}
