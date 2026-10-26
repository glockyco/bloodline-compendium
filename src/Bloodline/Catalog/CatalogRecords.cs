using Bloodline.Output;
using Bloodline.Records;

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
        );

    private static T[] ReadDirectory<T>(string directory) =>
        Directory
            .EnumerateFiles(directory, "*.json", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .Select(CanonicalJson.Read<T>)
            .ToArray();
}
