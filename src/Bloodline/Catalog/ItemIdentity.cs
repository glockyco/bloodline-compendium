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

    public static Dictionary<string, string> Slugs(IEnumerable<ClassRecord> classes) =>
        classes.ToDictionary(
            item => item.Path,
            item => Slug(RecordValues.Text(RecordValues.At(item.Properties, "ItemInfo.ItemName"))!),
            StringComparer.OrdinalIgnoreCase
        );

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
