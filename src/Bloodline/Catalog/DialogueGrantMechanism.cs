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
                        Place: (Place?)context.Places.Get(placement.Level)
                    )
                )
                .ToList();
            foreach (var type in defaultsByDialogue[dialogue.Path.Split('.')[0]])
            {
                if (!placedUsers.Any(placement => placement.Actor.Class == type.Path))
                {
                    users.Add((WorldSourceData.Name(type.Properties, type.Path), null));
                }
            }
            if (users.Count == 0)
            {
                users.Add(
                    (ItemIdentity.Readable(dialogue.Path.Split('/').Last().Split('.')[0]), null)
                );
            }
            foreach (var grant in grants)
            {
                var properties = context.Classes.TryGetValue(grant.Class, out var defaults)
                    ? RecordMerge.OverlayProperties(defaults.Properties, grant.Properties)
                    : grant.Properties;
                var amount = WorldSourceData.Integer(properties["Amount to Give"]) ?? 1;
                if (
                    amount <= 0
                    || RecordValues.AnyReference(properties["Item To Add"]) is not { } item
                )
                {
                    continue;
                }
                foreach (var (name, place) in users)
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
                            ["Dialogue: " + dialogue.Path, "Event: " + grant.Path]
                        )
                    );
                }
            }
        }
    }
}
