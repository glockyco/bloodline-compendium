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

    /// <summary>Skill tooltips that lab runs of the build read, empty when no run read any.</summary>
    public IReadOnlyList<SkillTooltip> SkillTooltips { get; init; } = [];

    /// <summary>The repository's known game defects, which the build copies beside the program.</summary>
    public KnownIssueRegistry Issues { get; init; } = new([]);

    /// <summary>The hand-written mechanic pages, which the build copies beside the program.</summary>
    public IReadOnlyList<MechanicContent> Mechanics { get; init; } = [];

    /// <summary>The skill windows that the latest skill-trees run of the build built, empty without one.</summary>
    public IReadOnlyList<TreeEntry> Trees { get; init; } = [];

    /// <summary>Ranged damage that lab runs measured where the catalog cannot read it from the firing code.</summary>
    public IReadOnlyList<MeasuredDamage> MeasuredDamage { get; init; } = [];

    /// <summary>The harvestables in play in each level, from the latest census run of the build, empty without one.</summary>
    public IReadOnlyList<HarvestCensus> Censuses { get; init; } = [];

    /// <summary>What each harvestable preset gives and the gathering level it requires, which lab runs measured.</summary>
    public HarvestPresets HarvestPresets { get; init; } = new([], []);

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
            SkillTooltips = SkillTooltipCheck.Load(layout.RuntimeDirectory),
            Trees = TreeCheck.Load(layout.RuntimeDirectory),
            Censuses = HarvestCheck.Load(layout.RuntimeDirectory),
            Issues = KnownIssues.Load(Path.Combine(AppContext.BaseDirectory, "known-issues.json")),
            Mechanics = MechanicProducer.Load(
                Path.Combine(AppContext.BaseDirectory, "mechanics.json")
            ),
            MeasuredDamage = CanonicalJson.Read<MeasuredDamage[]>(
                Path.Combine(AppContext.BaseDirectory, "measured-damage.json")
            ),
            HarvestPresets = CanonicalJson.Read<HarvestPresets>(
                Path.Combine(AppContext.BaseDirectory, "harvest-presets.json")
            ),
        };

    private static T[] ReadDirectory<T>(string directory) =>
        Directory
            .EnumerateFiles(directory, "*.json", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .Select(CanonicalJson.Read<T>)
            .ToArray();
}
