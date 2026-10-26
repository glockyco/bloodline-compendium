using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed class DialogueGrantMechanism : ISourceMechanism
{
    public IEnumerable<ItemSource> FindSources(CatalogContext context)
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
                .Objects.Where(obj =>
                    WorldSourceData.IsClass(context, obj.Class, "AddItemDialogue_C")
                )
                .ToArray();
            if (grants.Length == 0)
            {
                continue;
            }
            var placedUsers = placements[dialogue.Path.Split('.')[0]].ToArray();
            var users = placedUsers
                .Select(placement =>
                    (
                        Name: WorldSourceData.Name(placement.Properties, placement.Actor.Class),
                        Place: (EntityReference?)context.Places.Get(placement.Level),
                        Generated: placement.Actor.Class.Contains(
                            "Spawner",
                            StringComparison.OrdinalIgnoreCase
                        ),
                        Entity: WorldSourceData.PlacementEntity(
                            context,
                            placement,
                            EntityKind.Person
                        )
                    )
                )
                .ToList();
            foreach (var type in defaultsByDialogue[dialogue.Path.Split('.')[0]])
            {
                if (!placedUsers.Any(placement => placement.Actor.Class == type.Path))
                {
                    users.Add(
                        (
                            WorldSourceData.Name(type.Properties, type.Path),
                            null,
                            type.Path.Contains("Spawner", StringComparison.OrdinalIgnoreCase),
                            WorldSourceData.Reference(
                                EntityKind.Person,
                                dialogue.Path,
                                WorldSourceData.Name(type.Properties, type.Path)
                            )
                        )
                    );
                }
            }
            if (users.Count == 0)
            {
                users.Add(
                    (
                        ItemIdentity.Readable(dialogue.Path.Split('/').Last().Split('.')[0]),
                        null,
                        false,
                        WorldSourceData.Reference(
                            EntityKind.Person,
                            dialogue.Path,
                            ItemIdentity.Readable(dialogue.Path.Split('/').Last().Split('.')[0])
                        )
                    )
                );
            }
            var rewards = grants
                .Select(grant =>
                {
                    var properties = context.Classes.TryGetValue(grant.Class, out var defaults)
                        ? RecordMerge.OverlayProperties(defaults.Properties, grant.Properties)
                        : grant.Properties;
                    return (
                        Grant: grant,
                        Item: RecordValues.AnyReference(properties["Item To Add"]),
                        Amount: WorldSourceData.Integer(properties["Amount to Give"]) ?? 1
                    );
                })
                .ToArray();
            foreach (var (grant, item, amount) in rewards)
            {
                if (amount <= 0 || item is null)
                {
                    continue;
                }
                var notes = GrantNotes(context, dialogue, grant).Distinct().ToList();
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
                        $"This conversation includes {matching.Length} separate grants of {amount} of this item. "
                            + (
                                together
                                    ? "These grants share the same dialogue line."
                                    : "Whether they occur together is unknown."
                            )
                    );
                }
                foreach (var (name, place, generated, entity) in users)
                {
                    yield return new ItemSource(
                        item,
                        new Source(
                            SourceKind.Dialogue,
                            name,
                            place is null ? [] : [place],
                            null,
                            new Amount(amount, amount),
                            null,
                            null,
                            [],
                            generated
                                ?
                                [
                                    .. notes,
                                    "This conversation is assigned to a generated character. The exact speaker and availability are unknown.",
                                ]
                                : notes
                        )
                        {
                            Entity = entity,
                            Conditions =
                            [
                                .. WorldSourceData.DialogueConditions(context, dialogue, grant),
                                new RelationCondition(
                                    ConditionKind.Unknown,
                                    null,
                                    null,
                                    "Other dialogue choices and requirements are not fully known."
                                ),
                            ],
                        }
                    );
                }
            }
        }
    }

    private static IEnumerable<string> GrantNotes(
        CatalogContext context,
        AssetRecord dialogue,
        InnerObjectRecord grant
    )
    {
        var nodes = RecordValues
            .Elements(dialogue.Properties["Data"])
            .Where(node =>
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
        // Dialogue wording is context, not a substitute for typed relation conditions.
        foreach (var node in nodes)
        {
            var text = RecordValues.Text(RecordValues.At(node, "Text"));
            if (!string.IsNullOrWhiteSpace(text))
            {
                var player = RecordValues.Boolean(RecordValues.At(node, "isPlayer")) == true;
                yield return player
                    ? $"Dialogue choice: \"{text.Trim()}\""
                    : $"During dialogue: \"{text.Trim()}\"";
            }
        }
        if (dialogue.Path.Contains("/Dialogue_Unused/", StringComparison.OrdinalIgnoreCase))
        {
            yield return "This conversation is marked unused in the game data. Availability is uncertain.";
        }
        if (dialogue.Path.Contains("_SoulfaringNight.", StringComparison.OrdinalIgnoreCase))
        {
            yield return "From the Soulfaring Night conversation.";
        }
    }
}
