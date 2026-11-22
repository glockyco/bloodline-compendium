using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Bloodline.Records;

/// <summary>The Steam build that a set of records was read from.</summary>
internal sealed record BuildRecord(int AppId, long BuildId, IReadOnlyList<DepotRecord> Depots);

/// <summary>An installed depot. Manifest IDs exceed what JavaScript numbers hold exactly, so they stay strings.</summary>
internal sealed record DepotRecord(long DepotId, string ManifestId);

/// <summary>An unread object or package, or an unsupported property value, with its extraction error.</summary>
internal sealed record GapRecord(string Package, string? Object, string Error);

/// <summary>
/// A Blueprint class with its merged class defaults. <see cref="Ancestors"/> runs from the parent to the
/// first native class, which ends the chain because native defaults are not in the files. A widget Blueprint
/// also lists every widget of its widget tree with its place. Widgets whose class is a Blueprint class, such as the
/// skill slots of a skill tree, and text widgets, such as a row's label, also hold their values. Other native
/// widgets, such as panels, hold none, because only their place matters to a reader. <see cref="Bindings"/> lists
/// the functions it binds to its widgets' events.
/// </summary>
internal sealed record ClassRecord(
    string Path,
    IReadOnlyList<string> Ancestors,
    JsonObject Properties,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        IReadOnlyList<WidgetRecord>? Widgets = null
)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<DelegateBinding>? Bindings { get; init; }
}

/// <summary>A function bound to a widget's event, such as <c>OnClicked</c> of the button <c>Lute</c>.</summary>
internal sealed record DelegateBinding(string Component, string Event, string Function);

/// <summary>A DataTable with its rows in stored order.</summary>
internal sealed record TableRecord(string Path, string? RowStruct, IReadOnlyList<TableRow> Rows);

internal sealed record TableRow(string Name, JsonObject Values);

/// <summary>A user-defined enum with each value's display name.</summary>
internal sealed record EnumRecord(string Path, IReadOnlyList<EnumEntry> Entries);

internal sealed record EnumEntry(string Name, long Value, string? Display);

/// <summary>A level with its streamed levels and the actors placed in it whose class is a Blueprint class.</summary>
internal sealed record LevelRecord(
    string Path,
    IReadOnlyList<string> Streaming,
    IReadOnlyList<ActorRecord> Actors
);

/// <summary>
/// A placed actor. <see cref="Properties"/> holds only the properties that the placement overrides, with
/// overridden structs already merged over the class defaults, so a placed value replaces the class value.
/// </summary>
internal sealed record ActorRecord(string Name, string Class, JsonObject Properties);

/// <summary>An asset, such as a dialogue, with the objects it contains.</summary>
internal sealed record AssetRecord(
    string Path,
    string Class,
    JsonObject Properties,
    IReadOnlyList<InnerObjectRecord> Objects
);

/// <summary>An object inside an asset. Like a placed actor, it holds only its overrides of its class defaults.</summary>
internal sealed record InnerObjectRecord(string Path, string Class, JsonObject Properties);

/// <summary>
/// A widget in a widget tree. Like a placed actor, it holds only the values the tree sets on it, so a reader takes
/// every other value from the widget class's defaults. <see cref="Parent"/> names the panel that holds it, and
/// <see cref="Position"/> is its index among that panel's children, which is the order the game lays them out in.
/// A widget at the root of the tree has neither.
/// </summary>
internal sealed record WidgetRecord(
    string Path,
    string Class,
    JsonObject Properties,
    string? Parent,
    int? Position
);

/// <summary>A decoded texture, stored as <c>icons/&lt;Hash&gt;.png</c>, where the hash covers the decoded pixels.</summary>
internal sealed record TextureRecord(
    string Path,
    string Hash,
    int Width,
    int Height,
    string Format
);

/// <summary>Where the records and site data of one build live inside the output directory.</summary>
internal sealed class RecordLayout(string outputDirectory, long buildId)
{
    public string BuildDirectory { get; } =
        System.IO.Path.Combine(
            outputDirectory,
            buildId.ToString(System.Globalization.CultureInfo.InvariantCulture)
        );

    public string RecordsDirectory => System.IO.Path.Combine(BuildDirectory, "records");

    public string SiteDirectory => System.IO.Path.Combine(BuildDirectory, "site");

    public string BuildFile => System.IO.Path.Combine(RecordsDirectory, "build.json");

    public string GapsFile => System.IO.Path.Combine(RecordsDirectory, "gaps.json");

    public string TexturesFile => System.IO.Path.Combine(RecordsDirectory, "textures.json");

    public string IconsDirectory => System.IO.Path.Combine(RecordsDirectory, "icons");

    /// <summary>The checked model renders that <c>tools/runtime/publish-renders.mjs</c> writes, with their index.</summary>
    public string RendersDirectory => System.IO.Path.Combine(BuildDirectory, "renders");

    public string CodeIndexFile => System.IO.Path.Combine(RecordsDirectory, "code", "index.json");

    public string ClaimsFile => System.IO.Path.Combine(BuildDirectory, "item-claims.json");

    public string TooltipReportFile => System.IO.Path.Combine(BuildDirectory, "tooltip-check.json");

    /// <summary>The maintainer report of what the catalog could not read or resolve.</summary>
    public string ReportFile => System.IO.Path.Combine(BuildDirectory, "report.json");

    /// <summary>The lab's runs of the build, one directory each with its <c>run.jsonl</c>.</summary>
    public string RuntimeDirectory => System.IO.Path.Combine(BuildDirectory, "runtime");

    public string ClassesDirectory => System.IO.Path.Combine(RecordsDirectory, "classes");

    public string TablesDirectory => System.IO.Path.Combine(RecordsDirectory, "tables");

    public string EnumsDirectory => System.IO.Path.Combine(RecordsDirectory, "enums");

    public string LevelsDirectory => System.IO.Path.Combine(RecordsDirectory, "levels");

    public string DialoguesDirectory => System.IO.Path.Combine(RecordsDirectory, "dialogues");

    /// <summary>The file for a package inside a kind's directory, such as <c>classes/Game/Folder/Package.json</c>.</summary>
    public static string PackageFile(string kindDirectory, string packagePath) =>
        System.IO.Path.Combine([
            kindDirectory,
            .. packagePath.TrimStart('/').Split('/')[..^1],
            packagePath.Split('/')[^1] + ".json",
        ]);
}
