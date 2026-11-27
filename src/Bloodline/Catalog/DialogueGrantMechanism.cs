using System.Text.Json.Nodes;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

/// <summary>
/// Conversations that give something: an item through an AddItemDialogue event, or a skill through an UnlockSkill
/// event, with the people who hold the conversation and the lines that lead to the gift.
/// </summary>
internal sealed class DialogueGrantMechanism : ISourceMechanism
{
    /// <summary>A dialogue event class that gives an entity, the field naming it and the field of its amount.</summary>
    private sealed record Grant(
        string EventClass,
        EntityKind Kind,
        string Field,
        string? AmountField
    );

    private static readonly Grant[] Grants =
    [
        new("AddItemDialogue_C", EntityKind.Item, "Item To Add", "Amount to Give"),
        new("UnlockSkill_C", EntityKind.Skill, "Skill to Add", null),
    ];

    public IEnumerable<EntitySource> FindSources(CatalogContext context)
    {
        var dialoguePaths = context
            .Records.Dialogues.Select(dialogue => dialogue.Path.Split('.')[0])
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var placements = context
            .Placements.SelectMany(placement =>
                WorldSourceData
                    .References(placement.Properties)
                    .Select(reference => reference.Split('.')[0])
                    .Where(dialoguePaths.Contains)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(dialogue => (Dialogue: dialogue, Placement: placement))
            )
            .ToLookup(
                entry => entry.Dialogue,
                entry => entry.Placement,
                StringComparer.OrdinalIgnoreCase
            );
        var defaultsByDialogue = context
            .Classes.Values.Where(type => !context.IsExcludedClass(type.Path))
            .SelectMany(type =>
                WorldSourceData
                    .References(type.Properties)
                    .Select(reference => reference.Split('.')[0])
                    .Where(dialoguePaths.Contains)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(dialogue => (Dialogue: dialogue, Type: type))
            )
            .ToLookup(
                entry => entry.Dialogue,
                entry => entry.Type,
                StringComparer.OrdinalIgnoreCase
            );
        foreach (var dialogue in context.Records.Dialogues)
        {
            var grants = dialogue
                .Objects.Select(obj =>
                    (
                        Object: obj,
                        Spec: Grants.FirstOrDefault(spec =>
                            WorldSourceData.IsClass(context, obj.Class, spec.EventClass)
                        )
                    )
                )
                .Where(entry => entry.Spec is not null)
                .ToArray();
            if (grants.Length == 0)
            {
                continue;
            }
            var package = dialogue.Path.Split('.')[0];
            var speaker = RecordValues.Text(dialogue.Properties["Name"]);
            if (string.IsNullOrWhiteSpace(speaker))
                speaker = null;
            var placedUsers = placements[package].ToArray();
            var users =
                new List<(
                    EntityReference Entity,
                    EntityReference? Place,
                    bool Generated,
                    bool UsesDialogue
                )>();
            foreach (var placement in placedUsers)
            {
                var generated = placement.Actor.Class.Contains(
                    "Spawner",
                    StringComparison.OrdinalIgnoreCase
                );
                var activeDialogue = RecordValues.AnyReference(placement.Properties["Dialogue"]);
                var usesDialogue =
                    !generated
                    && string.Equals(
                        activeDialogue?.Split('.')[0],
                        package,
                        StringComparison.OrdinalIgnoreCase
                    );
                // A placement that swaps in another conversation speaks under that conversation's name, as the arena's
                // waitress does (run design-facts). An actor with its own conversation keeps its own name.
                var swapped =
                    usesDialogue
                    && !defaultsByDialogue[package].Any(type => type.Path == placement.Actor.Class);
                users.Add(
                    (
                        WorldSourceData.PlacementEntity(
                            context,
                            placement,
                            EntityKind.Person,
                            swapped ? speaker : null
                        ),
                        context.Places.Get(placement.Level),
                        generated,
                        usesDialogue
                    )
                );
            }
            foreach (var type in defaultsByDialogue[package])
            {
                if (!placedUsers.Any(placement => placement.Actor.Class == type.Path))
                {
                    users.Add(
                        (
                            WorldSourceData.Reference(
                                EntityKind.Person,
                                dialogue.Path,
                                WorldSourceData.Name(type.Properties, type.Path)
                            ),
                            null,
                            type.Path.Contains("Spawner", StringComparison.OrdinalIgnoreCase),
                            false
                        )
                    );
                }
            }
            if (users.Count == 0)
            {
                users.Add(
                    (
                        WorldSourceData.Reference(
                            EntityKind.Person,
                            dialogue.Path,
                            ItemIdentity.Readable(package.Split('/').Last())
                        ),
                        null,
                        false,
                        false
                    )
                );
            }
            var data = RecordValues.Elements(dialogue.Properties["Data"]).ToArray();
            var incoming = data.SelectMany(node =>
                    RecordValues
                        .Elements(RecordValues.At(node, "Links"))
                        .Select(RecordValues.Integer)
                        .OfType<long>()
                        .Select(id => (Id: id, Node: node))
                )
                .ToLookup(entry => entry.Id, entry => entry.Node);
            var objects = dialogue.Objects.ToDictionary(
                obj => obj.Path,
                StringComparer.OrdinalIgnoreCase
            );
            var debits = new Dictionary<string, int?>(StringComparer.OrdinalIgnoreCase);
            foreach (var obj in dialogue.Objects)
            {
                if (WorldSourceData.IsClass(context, obj.Class, "TakePlayerMoney"))
                {
                    debits.Add(
                        obj.Path,
                        WorldSourceData.Integer(EventProperties(context, obj)["Item Cost"])
                    );
                }
                else if (WorldSourceData.IsClass(context, obj.Class, "TakeMoneyBasedOnitemValue"))
                {
                    var chargedItem = RecordValues.AnyReference(
                        EventProperties(context, obj)["Item"]
                    );
                    debits.Add(
                        obj.Path,
                        chargedItem is not null
                        && context.Classes.TryGetValue(chargedItem, out var type)
                            ? WorldSourceData.Integer(
                                RecordValues.At(type.Properties, "ItemInfo.ItemValue")
                            )
                            : null
                    );
                }
            }
            var underUnused = dialogue.Path.Contains(
                "/Dialogue_Unused/",
                StringComparison.OrdinalIgnoreCase
            );
            var rewards = grants
                .Select(entry =>
                {
                    var (grant, spec) = entry;
                    var properties = EventProperties(context, grant);
                    return (
                        Grant: grant,
                        Spec: spec!,
                        Item: RecordValues.AnyReference(properties[spec!.Field]),
                        Amount: spec.AmountField is { } field
                            ? WorldSourceData.Integer(properties[field]) ?? 1
                            : 1
                    );
                })
                .ToArray();
            foreach (var (grant, spec, item, amount) in rewards)
            {
                if (amount <= 0 || item is null)
                {
                    continue;
                }
                var notes = GrantNotes(dialogue).ToList();
                var grantLines = data.Where(node => References(node, "Events", grant.Path))
                    .ToArray();
                var quotes = Quotes(context, grantLines, incoming, objects, debits)
                    .Distinct()
                    .ToArray();
                var price =
                    spec.Kind == EntityKind.Item
                        ? GrantPrice(context, grantLines, incoming, objects, debits)
                        : null;
                var requirements = WorldSourceData.DialogueConditions(context, dialogue, grant);
                var matching = rewards
                    .Where(reward =>
                        reward.Amount == amount
                        && string.Equals(reward.Item, item, StringComparison.OrdinalIgnoreCase)
                    )
                    .Select(reward => reward.Grant.Path)
                    .ToArray();
                if (matching.Length > 1)
                {
                    var together = RecordValues
                        .Elements(dialogue.Properties["Data"])
                        .Any(node =>
                            matching.All(path =>
                                RecordValues
                                    .Elements(RecordValues.At(node, "Events"))
                                    .Any(evt =>
                                        string.Equals(
                                            RecordValues.AnyReference(evt),
                                            path,
                                            StringComparison.OrdinalIgnoreCase
                                        )
                                    )
                            )
                        );
                    notes.Add(
                        together
                            ? $"The same line gives it {matching.Length} times."
                            : $"{matching.Length} lines of this conversation give it."
                    );
                }
                foreach (var (entity, place, generated, usesDialogue) in users)
                {
                    var unconfirmed = underUnused && !usesDialogue;
                    IReadOnlyList<string> sourceNotes = notes;
                    if (unconfirmed)
                    {
                        sourceNotes =
                        [
                            .. sourceNotes,
                            "The game files mark this conversation unused, so it may never take place.",
                        ];
                    }
                    if (generated)
                    {
                        sourceNotes =
                        [
                            .. sourceNotes,
                            "A character the game generates holds this conversation, so who and where is unknown.",
                        ];
                    }
                    yield return new EntitySource(
                        item,
                        new Source(
                            SourceKind.Dialogue,
                            entity.Name,
                            place is null ? [] : [place],
                            price,
                            spec.AmountField is null ? null : new Amount(amount, amount),
                            [],
                            sourceNotes
                        )
                        {
                            Entity = entity,
                            Quotes = quotes,
                            Conditions = requirements.Shown,
                            Unconfirmed = unconfirmed,
                        }
                    )
                    {
                        Kind = spec.Kind,
                        Unresolved = requirements.Unresolved,
                    };
                }
            }
        }
    }

    private static JsonObject EventProperties(CatalogContext context, InnerObjectRecord obj) =>
        context.Classes.TryGetValue(obj.Class, out var defaults)
            ? RecordMerge.OverlayProperties(defaults.Properties, obj.Properties)
            : obj.Properties;

    private static bool References(JsonNode? node, string field, string path) =>
        RecordValues
            .Elements(RecordValues.At(node, field))
            .Any(reference =>
                string.Equals(
                    RecordValues.AnyReference(reference),
                    path,
                    StringComparison.OrdinalIgnoreCase
                )
            );

    private static IEnumerable<JsonNode?> Parents(
        JsonNode? node,
        ILookup<long, JsonNode?> incoming
    ) => RecordValues.Integer(RecordValues.At(node, "id")) is { } id ? incoming[id] : [];

    private static IEnumerable<Quote> Quotes(
        CatalogContext context,
        JsonNode?[] grantLines,
        ILookup<long, JsonNode?> incoming,
        IReadOnlyDictionary<string, InnerObjectRecord> objects,
        IReadOnlyDictionary<string, int?> debits
    )
    {
        bool HasClass(JsonNode? node, string field, string type) =>
            RecordValues
                .Elements(RecordValues.At(node, field))
                .Any(reference =>
                    RecordValues.AnyReference(reference) is { } path
                    && objects.TryGetValue(path, out var obj)
                    && WorldSourceData.IsClass(context, obj.Class, type)
                );

        bool Charges(JsonNode? node) =>
            RecordValues
                .Elements(RecordValues.At(node, "Events"))
                .Any(evt => RecordValues.AnyReference(evt) is { } path && debits.ContainsKey(path));

        foreach (var node in grantLines)
        {
            var hasPayment =
                Charges(node)
                || HasClass(node, "Conditions", "PlayerHasEnoughMoney")
                || HasClass(node, "Conditions", "PlayerHasEnoughMoneyForSpecifiedItem");
            foreach (
                var paymentLine in hasPayment ? [node] : Parents(node, incoming).Where(Charges)
            )
            {
                // The offer is quoted dialogue. A gold check need not deduct anything.
                foreach (
                    var offer in Parents(paymentLine, incoming)
                        .Where(offer =>
                            RecordValues.Boolean(RecordValues.At(offer, "isPlayer")) == false
                        )
                )
                {
                    if (Line(offer) is { } quotedOffer)
                        yield return quotedOffer;
                }
                if (paymentLine != node && Line(paymentLine) is { } quotedReply)
                    yield return quotedReply;
            }
            if (Line(node) is { } grantLine)
                yield return grantLine;
        }
    }

    private static Price? GrantPrice(
        CatalogContext context,
        IReadOnlyList<JsonNode?> grantLines,
        ILookup<long, JsonNode?> incoming,
        IReadOnlyDictionary<string, InnerObjectRecord> objects,
        IReadOnlyDictionary<string, int?> debits
    )
    {
        (bool Found, int? Cost) Debit(JsonNode? node)
        {
            var found = false;
            long total = 0;
            foreach (var evt in RecordValues.Elements(RecordValues.At(node, "Events")))
            {
                if (
                    RecordValues.AnyReference(evt) is not { } path
                    || !debits.TryGetValue(path, out var cost)
                )
                    continue;
                found = true;
                if (cost is not >= 0)
                    return (true, null);
                total += cost.Value;
            }
            return (found, total <= int.MaxValue ? (int)total : null);
        }

        int? LineCost(JsonNode? node)
        {
            var (directDebit, cost) = Debit(node);
            if (directDebit)
                return cost;
            if (debits.Count == 0)
            {
                // A zero-gold acceptance check does not debit gold, even when the previous line quotes a price.
                return RecordValues
                    .Elements(RecordValues.At(node, "Conditions"))
                    .Any(check =>
                        RecordValues.AnyReference(check) is { } path
                        && objects.TryGetValue(path, out var obj)
                        && WorldSourceData.IsClass(context, obj.Class, "PlayerHasEnoughMoney")
                        && WorldSourceData.Integer(EventProperties(context, obj)["Required Money"])
                            == 0
                    )
                    ? 0
                    : null;
            }
            // Some replies debit the gold, then immediately advance to the line that grants the item.
            int? earlierCost = null;
            foreach (var previous in Parents(node, incoming))
            {
                var (paid, amount) = Debit(previous);
                if (!paid || amount is null || (earlierCost is not null && earlierCost != amount))
                    return null;
                earlierCost = amount;
            }
            return earlierCost;
        }

        int? price = null;
        foreach (var line in grantLines)
        {
            var cost = LineCost(line);
            if (cost is null || (price is not null && price != cost))
                return null;
            price = cost;
        }
        return price is { } amount ? new Price(amount, null) : null;
    }

    private static Quote? Line(JsonNode? node) =>
        RecordValues.Text(RecordValues.At(node, "Text")) is { } text
        && !string.IsNullOrWhiteSpace(text)
            ? new Quote(
                text.Trim(),
                RecordValues.Boolean(RecordValues.At(node, "isPlayer")) == true
            )
            : null;

    private static IEnumerable<string> GrantNotes(AssetRecord dialogue)
    {
        if (dialogue.Path.Contains("_SoulfaringNight.", StringComparison.OrdinalIgnoreCase))
        {
            yield return "From the Soulfaring Night conversation.";
        }
    }
}
