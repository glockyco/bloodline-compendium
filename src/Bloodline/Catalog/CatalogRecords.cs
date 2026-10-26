using Bloodline.Code;
using Bloodline.Output;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed record CatalogRecords(
    BuildRecord Build,
    IReadOnlyList<ClassRecord> Classes,
    IReadOnlyList<TableRecord> Tables,
    IReadOnlyList<EnumRecord> Enums,
    IReadOnlyList<LevelRecord> Levels,
    IReadOnlyList<AssetRecord> Dialogues,
    IReadOnlyList<TextureRecord> Textures,
    IReadOnlyList<GapRecord> Gaps
)
{
    public CodeIndex? Code { get; init; }

    /// <summary>Inventory tooltips that lab runs of the build read, empty when no run read any.</summary>
    public IReadOnlyList<TooltipObservation> Tooltips { get; init; } = [];

    /// <summary>The repository's known game defects, which the build copies beside the program.</summary>
    public KnownIssueRegistry Issues { get; init; } = new([]);

    public static CatalogRecords Load(RecordLayout layout) =>
        new(
            CanonicalJson.Read<BuildRecord>(layout.BuildFile),
            ReadDirectory<ClassRecord>(layout.ClassesDirectory),
            ReadDirectory<TableRecord>(layout.TablesDirectory),
            ReadDirectory<EnumRecord>(layout.EnumsDirectory),
            ReadDirectory<LevelRecord>(layout.LevelsDirectory),
            ReadDirectory<AssetRecord>(layout.DialoguesDirectory),
            CanonicalJson.Read<TextureRecord[]>(layout.TexturesFile),
            CanonicalJson.Read<GapRecord[]>(layout.GapsFile)
        )
        {
            Code = CanonicalJson.Read<CodeIndex>(layout.CodeIndexFile),
            Tooltips = TooltipCheck.Load(layout.RuntimeDirectory),
            Issues = KnownIssues.Load(Path.Combine(AppContext.BaseDirectory, "known-issues.json")),
        };

    private static T[] ReadDirectory<T>(string directory) =>
        Directory
            .EnumerateFiles(directory, "*.json", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .Select(CanonicalJson.Read<T>)
            .ToArray();
}
