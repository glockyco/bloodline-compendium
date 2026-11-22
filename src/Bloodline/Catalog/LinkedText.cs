using System.Text.Json;
using System.Text.RegularExpressions;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

/// <summary>
/// Text written for the site marks a reference as a Markdown link with a typed target, such as
/// <c>[Blood Levels](mechanic:blood-levels)</c>. A target is a kind and the slug of an entity's name. The catalog
/// resolves every target to one reference and fails on an unknown or ambiguous one, so a renamed entity cannot leave
/// a dead link. The site renders a reference to a kind without pages as its text.
/// </summary>
internal static partial class LinkedText
{
    [GeneratedRegex(@"\[(?<text>[^\]]+)\]\((?<target>[a-zA-Z]+:[a-z0-9-]+)\)")]
    public static partial Regex Link();

    /// <summary>The target an entity answers to, such as <c>discipline:blood-magic</c>.</summary>
    public static string Target(EntityReference reference) =>
        JsonNamingPolicy.CamelCase.ConvertName(reference.Kind.ToString())
        + ":"
        + ItemIdentity.Slug(reference.Name);

    /// <summary>
    /// The targets of every entity, each to its reference. Two entities of one kind and slug make the target
    /// ambiguous, which only fails when text links to it.
    /// </summary>
    public static IReadOnlyDictionary<string, EntityReference?> Targets(
        IEnumerable<EntityReference> references
    )
    {
        var targets = new Dictionary<string, EntityReference?>(StringComparer.Ordinal);
        foreach (var reference in references)
        {
            var target = Target(reference);
            if (targets.TryGetValue(target, out var existing) && existing?.Key != reference.Key)
                targets[target] = null;
            else
                targets.TryAdd(target, reference);
        }
        return targets;
    }

    /// <summary>Resolves every link of a text, adding each target to <paramref name="used"/>.</summary>
    public static void Resolve(
        string owner,
        string text,
        IReadOnlyDictionary<string, EntityReference?> targets,
        IDictionary<string, EntityReference> used
    )
    {
        foreach (Match link in Link().Matches(text))
        {
            var target = link.Groups["target"].Value;
            if (!targets.TryGetValue(target, out var reference))
                throw new InvalidDataException(
                    $"{owner} links to {target}, which names no entity."
                );
            used[target] =
                reference
                ?? throw new InvalidDataException(
                    $"{owner} links to {target}, which several entities share."
                );
        }
    }
}
