using System.Text.RegularExpressions;
using Bloodline.Records;

namespace Bloodline.Catalog;

internal static partial class ItemIdentity
{
    public const string BaseItem =
        "/Game/BloodlinesAlpha/Interactables/BP_BaseItemAsset.BP_BaseItemAsset_C";
    public const string Interactable =
        "/Game/BloodlinesAlpha/Interactables/InteractableObject.InteractableObject_C";

    public static bool IsInventoryItem(ClassRecord item) =>
        item.Ancestors.Contains(BaseItem, StringComparer.OrdinalIgnoreCase)
        && !item.Path.Equals(Interactable, StringComparison.OrdinalIgnoreCase)
        && !item.Ancestors.Contains(Interactable, StringComparer.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(
            RecordValues.Text(RecordValues.At(item.Properties, "ItemInfo.ItemName"))
        );

    public static Dictionary<string, string> Slugs(IEnumerable<ClassRecord> classes)
    {
        var facts = new ItemFacts([]);
        return Identities(classes.Select(item => facts.Resolve(item, "")))
            .ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Slug,
                StringComparer.OrdinalIgnoreCase
            );
    }

    public static Dictionary<string, (string Slug, string? Qualifier)> Identities(
        IEnumerable<Bloodline.SiteData.ItemPage> items
    )
    {
        var groups = items.GroupBy(item => Slug(item.Name), StringComparer.Ordinal).ToArray();
        var result = new Dictionary<string, (string Slug, string? Qualifier)>(
            StringComparer.OrdinalIgnoreCase
        );
        var reserved = groups.Select(group => group.Key).ToHashSet(StringComparer.Ordinal);
        foreach (var group in groups.OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            var members = group.OrderBy(item => item.Id, StringComparer.Ordinal).ToArray();
            for (var index = 0; index < members.Length; index++)
            {
                var item = members[index];
                string? qualifier = null;
                var slug = group.Key;
                if (members.Length > 1)
                {
                    var candidates = new[] { item.Type, item.Category.Name }.Concat(
                        item.Facts.Where(fact => fact.Value is not null && fact.Label != "Value")
                            .Select(fact => fact.Value + " " + fact.Label)
                    );
                    qualifier = candidates.FirstOrDefault(candidate =>
                        !string.IsNullOrWhiteSpace(candidate)
                        && candidate != "Unknown"
                        && members.Count(other =>
                            other.Type == candidate
                            || other.Category.Name == candidate
                            || other.Facts.Any(fact => fact.Value + " " + fact.Label == candidate)
                        ) == 1
                    );
                    qualifier ??= (index + 1).ToString(
                        System.Globalization.CultureInfo.InvariantCulture
                    );
                    slug += "-" + Slug(qualifier);
                    var ordinal = 2;
                    var basis = slug;
                    while (!reserved.Add(slug))
                    {
                        qualifier +=
                            " "
                            + ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        slug = basis + "-" + ordinal++;
                    }
                }
                result.Add(item.Id, (slug, qualifier));
            }
        }
        return result;
    }

    public static string DisplayName(Bloodline.SiteData.ItemPage item) =>
        item.Qualifier is null ? item.Name : item.Name + " (" + item.Qualifier + ")";

    public static string ClassName(string path)
    {
        var name = path[(Math.Max(path.LastIndexOf('/'), path.LastIndexOf('.')) + 1)..];
        return name.EndsWith("_C", StringComparison.Ordinal) ? name[..^2] : name;
    }

    public static string Slug(string name)
    {
        var words = CamelBoundary().Replace(AcronymBoundary().Replace(name, "$1-$2"), "$1-$2");
        return Separators().Replace(words.ToLowerInvariant(), "-").Trim('-');
    }

    public static string Readable(string name) =>
        string.Join(
            ' ',
            Slug(name)
                .Split('-')
                .Select(word =>
                    word.Length == 0 ? word : char.ToUpperInvariant(word[0]) + word[1..]
                )
        );

    [GeneratedRegex("([A-Z]+)([A-Z][a-z])", RegexOptions.CultureInvariant)]
    private static partial Regex AcronymBoundary();

    [GeneratedRegex("([a-z0-9])([A-Z])", RegexOptions.CultureInvariant)]
    private static partial Regex CamelBoundary();

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex Separators();
}
