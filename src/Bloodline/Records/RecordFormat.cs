namespace Bloodline.Records;

/// <summary>
/// Records store property values as JSON. Plain JSON holds booleans, numbers, names and strings,
/// arrays and sets (as arrays), and structs (as objects keyed by field name). Every other value is
/// an object with one tag key that starts with <c>$</c>:
/// <list type="bullet">
/// <item><c>{"$ref": "/Game/Folder/Package.Object"}</c> is an object or class reference. A reference to an
/// object inside a package adds its outer chain, as in <c>/Game/Folder/Package.Package:Inner</c>.
/// An empty reference is plain <c>null</c>.</item>
/// <item><c>{"$soft": "/Game/Folder/Package.Object"}</c> is a soft object or soft class reference.</item>
/// <item><c>{"$enum": "E_ItemType::NewEnumerator3", "display": "Food"}</c> is an enum value. Only
/// user-defined enums carry a display name.</item>
/// <item><c>{"$text": "Iron Sword", "namespace": "", "key": "4A1F..."}</c> is localized text with its source
/// string. String table entries carry <c>table</c> and <c>key</c> instead of a namespace.</item>
/// <item>Formatted text adds <c>format</c> with the source pattern and <c>arguments</c> with encoded argument
/// values keyed by name, or by zero-based numeric strings for ordered arguments. Its <c>$text</c> is the
/// readable result using source text and invariant numbers. Unresolved arguments keep their placeholders.
/// Number, percent, currency, date, time and case-transform histories use a normalized <c>{value}</c>
/// pattern, with a percent suffix or ISO currency prefix where appropriate, and retain their
/// <c>history</c> and authored options. Dates and times display their stored ticks in invariant ISO form,
/// with time zone and style metadata retained instead of applying machine-local settings.</item>
/// <item><c>{"$map": [[key, value], ...]}</c> is a map, with entries in stored order.</item>
/// <item><c>{"$unsupported": "PropertyType"}</c> is a property type that the encoder does not handle.</item>
/// </list>
/// Struct field names drop the <c>_N_GUID</c> suffix that user-defined structs add, unless two fields of one
/// struct would then share a name. A field that is missing from a record has no value in the files, for
/// example a default that only native code sets, so readers treat it as unknown.
/// Event delegate bindings are not data properties and are omitted without gaps.
/// </summary>
internal static class RecordFormat
{
    public const string Reference = "$ref";
    public const string SoftReference = "$soft";
    public const string Enum = "$enum";
    public const string EnumDisplay = "display";
    public const string Text = "$text";
    public const string TextNamespace = "namespace";
    public const string TextKey = "key";
    public const string TextTable = "table";
    public const string TextFormat = "format";
    public const string TextArguments = "arguments";
    public const string Map = "$map";
    public const string Unsupported = "$unsupported";

    /// <summary>Whether a key is a tag key, which no struct field name starts with.</summary>
    public static bool IsTag(string key) => key.StartsWith('$');
}
