using Bloodline.Records;

namespace Bloodline.Catalog;

/// <summary>
/// The conversations that set each dialogue tag through AddSpecialTagDialogue, and who holds each conversation, so a
/// tag check can read as the conversation a player has to have first.
/// </summary>
internal sealed class DialogueTags
{
    private readonly Dictionary<string, HashSet<string>> setters = new(
        StringComparer.OrdinalIgnoreCase
    );
    private readonly Dictionary<string, SortedSet<string>> speakers = new(
        StringComparer.OrdinalIgnoreCase
    );

    public DialogueTags(CatalogContext context)
    {
        foreach (var dialogue in context.Records.Dialogues)
        foreach (
            var setter in dialogue.Objects.Where(obj =>
                ItemIdentity.ClassName(obj.Class) == "AddSpecialTagDialogue"
            )
        )
        {
            if (RecordValues.String(setter.Properties["Special Tag"]) is not { Length: > 0 } tag)
                continue;
            if (!setters.TryGetValue(tag, out var set))
                setters.Add(tag, set = new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            set.Add(Package(dialogue.Path));
        }
        var dialogues = context
            .Records.Dialogues.Select(dialogue => Package(dialogue.Path))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        void Add(IEnumerable<string> references, string name)
        {
            foreach (var dialogue in references.Select(Package).Where(dialogues.Contains))
            {
                if (!speakers.TryGetValue(dialogue, out var names))
                    speakers.Add(dialogue, names = new SortedSet<string>(StringComparer.Ordinal));
                names.Add(name);
            }
        }
        foreach (var placement in context.Placements)
            Add(
                WorldSourceData.References(placement.Properties),
                WorldSourceData.Name(placement.Properties, placement.Actor.Class)
            );
        foreach (
            var type in context.Classes.Values.Where(type => !context.IsExcludedClass(type.Path))
        )
            Add(
                WorldSourceData.References(type.Properties),
                WorldSourceData.Name(type.Properties, type.Path)
            );
    }

    /// <summary>The conversations that set the tag, as dialogue packages.</summary>
    public IReadOnlySet<string> Setters(string tag) =>
        setters.TryGetValue(tag, out var set) ? set : new HashSet<string>();

    /// <summary>Who holds a conversation, by name.</summary>
    public IReadOnlyCollection<string> Speakers(string dialogue) =>
        speakers.TryGetValue(Package(dialogue), out var names) ? names : [];

    public static string Package(string path) => path.Split('.')[0];
}
