using Bloodline.SiteData;

namespace Bloodline.Catalog;

/// <summary>
/// The station that opens the crafting window for each discipline. Anvil_Workbench, Engineers_Workbench,
/// NecromancersAltar and Smelting_Oven create Crafting with their crafting type, FocusAssemblyTable creates it for
/// magical components, and CookingFire opens CookingWindow, which reads AllFoodRecipes.
/// </summary>
internal static class Stations
{
    private static readonly Dictionary<string, (string Path, string Name)> ByDiscipline = new(
        StringComparer.Ordinal
    )
    {
        ["Weaponsmithing"] = ("/Game/BloodlinesAlpha/Anvil_Workbench.Anvil_Workbench_C", "Anvil"),
        ["Focus crafting"] = (
            "/Game/BloodlinesAlpha/Interactables/FocusAssemblyTable.FocusAssemblyTable_C",
            "Focus Assembly Table"
        ),
        ["Smelting"] = ("/Game/BloodlinesAlpha/Smelting_Oven.Smelting_Oven_C", "Smelting Oven"),
        ["Engineering"] = (
            "/Game/BloodlinesAlpha/Engineers_Workbench.Engineers_Workbench_C",
            "Engineer's Workbench"
        ),
        ["Necromancy"] = (
            "/Game/BloodlinesAlpha/NecromancersAltar.NecromancersAltar_C",
            "Necromancer's Altar"
        ),
        ["Cooking"] = ("/Game/BloodlinesAlpha/CookingFire.CookingFire_C", "Cooking Fire"),
    };

    public static EntityReference? For(string? discipline) =>
        discipline is not null && ByDiscipline.TryGetValue(discipline, out var station)
            ? WorldSourceData.Reference(EntityKind.Activity, station.Path, station.Name)
            : null;
}
