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
                .Select(entry =>
                {
                    var (grant, spec) = entry;
                    var properties = context.Classes.TryGetValue(grant.Class, out var defaults)
                        ? RecordMerge.OverlayProperties(defaults.Properties, grant.Properties)
                        : grant.Properties;
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
                var quotes = Quotes(dialogue, grant).Distinct().ToArray();
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
                foreach (var (name, place, generated, entity) in users)
                {
                    yield return new EntitySource(
                        item,
                        new Source(
                            SourceKind.Dialogue,
                            name,
                            place is null ? [] : [place],
                            null,
                            spec.AmountField is null ? null : new Amount(amount, amount),
                            [],
                            generated
                                ?
                                [
                                    .. notes,
                                    "A character the game generates holds this conversation, so who and where is unknown.",
                                ]
                                : notes
                        )
                        {
                            Entity = entity,
                            Quotes = quotes,
                            Conditions = requirements.Shown,
                            Unconfirmed = dialogue.Path.Contains(
                                "/Dialogue_Unused/",
                                StringComparison.OrdinalIgnoreCase
                            ),
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

    private static IEnumerable<Quote> Quotes(AssetRecord dialogue, InnerObjectRecord grant)
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
                yield return new Quote(
                    text.Trim(),
                    RecordValues.Boolean(RecordValues.At(node, "isPlayer")) == true
                );
            }
        }
    }

    private static IEnumerable<string> GrantNotes(AssetRecord dialogue)
    {
        if (dialogue.Path.Contains("/Dialogue_Unused/", StringComparison.OrdinalIgnoreCase))
        {
            yield return "The game files mark this conversation unused, so it may never take place.";
        }
        if (dialogue.Path.Contains("_SoulfaringNight.", StringComparison.OrdinalIgnoreCase))
        {
            yield return "From the Soulfaring Night conversation.";
        }
    }
}
