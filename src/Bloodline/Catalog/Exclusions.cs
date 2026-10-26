using Bloodline.Records;

namespace Bloodline.Catalog;

internal enum ExclusionKind
{
    Level,
    Class,
}

internal sealed record Exclusion(ExclusionKind Kind, string Path, string Reason, string Evidence);

internal static class Exclusions
{
    public static IReadOnlyList<Exclusion> Reviewed { get; } =
    [
        new(
            ExclusionKind.Level,
            "/Game/DevTestMap",
            "Development test level with no gameplay travel path.",
            "FirstPersonGameMode console dispatch opens DevTestMap only for the LoadLevel DevTestMap command."
        ),
        new(
            ExclusionKind.Level,
            "/Game/Eudros/OverworldTest1",
            "Test overworld with no gameplay travel path.",
            "The shipped gameplay travel targets do not name OverworldTest1."
        ),
        new(
            ExclusionKind.Level,
            "/Game/Eudros/Overworld_working",
            "Working overworld with no gameplay travel path.",
            "The shipped gameplay travel targets do not name Overworld_working. Cooking the map does not establish a gameplay path."
        ),
    ];

    public static void Validate(CatalogRecords records, IReadOnlyList<Exclusion> entries)
    {
        var levels = records
            .Levels.Select(level => level.Path)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var classes = records
            .Classes.Select(item => item.Path)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            if (!(entry.Kind == ExclusionKind.Level ? levels : classes).Contains(entry.Path))
            {
                throw new InvalidDataException(
                    $"Stale {entry.Kind} exclusion {entry.Path}: the records do not contain this entry. {entry.Reason}"
                );
            }
        }
    }
}
