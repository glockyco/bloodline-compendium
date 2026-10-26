using System.Text.Json.Nodes;

namespace Bloodline.Records;

/// <summary>The Steam build that a set of records was read from.</summary>
internal sealed record BuildRecord(int AppId, long BuildId, IReadOnlyList<DepotRecord> Depots);

/// <summary>An installed depot. Manifest IDs exceed what JavaScript numbers hold exactly, so they stay strings.</summary>
internal sealed record DepotRecord(long DepotId, string ManifestId);

/// <summary>An unread object or package, or an unsupported property value, with its extraction error.</summary>
internal sealed record GapRecord(string Package, string? Object, string Error);

/// <summary>
/// A Blueprint class with its merged class defaults. <see cref="Ancestors"/> runs from the parent to the
/// first native class, which ends the chain because native defaults are not in the files.
/// </summary>
internal sealed record ClassRecord(
    string Path,
    IReadOnlyList<string> Ancestors,
    JsonObject Properties
);

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

    public string CodeIndexFile => System.IO.Path.Combine(RecordsDirectory, "code", "index.json");

    public string ClaimsFile => System.IO.Path.Combine(BuildDirectory, "item-claims.json");

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
