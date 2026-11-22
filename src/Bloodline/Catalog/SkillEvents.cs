using Bloodline.Code;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

/// <summary>
/// A function that unlocks a skill outside the skill trees, skill books and conversations, with reader text for
/// what the player does. <see cref="Fingerprint"/> is the reviewed function's fingerprint, so a game update that
/// changes it fails the catalog until the text is reviewed again. A null <see cref="Skill"/> and
/// <see cref="Text"/> with a <see cref="Reason"/> marks a function that gives a player no skill.
/// </summary>
internal sealed record SkillEvent(
    string Citation,
    string Fingerprint,
    string? Skill,
    string? Text,
    string? Reason = null
);

/// <summary>
/// Skills that events in the game's code unlock. Every function that calls the game's skill unlock must be covered:
/// by a source mechanism for its owner class, or by a reviewed entry here.
/// </summary>
internal sealed class SkillEventMechanism : ISourceMechanism
{
    /// <summary>The calls through which the game's code unlocks a skill.</summary>
    private static readonly HashSet<string> UnlockCalls = new(StringComparer.Ordinal)
    {
        "virtual:AddNewSkill",
        "virtual:UnlockSkill",
        "virtual:Unlock Skill",
    };

    /// <summary>Owner classes whose unlocks other mechanisms publish: tree slots, skill books and conversations.</summary>
    private static readonly HashSet<string> Mechanisms = new(StringComparer.Ordinal)
    {
        "SkillSlot_C",
        "SkillBook_InteractionObject_C",
        "UnlockSkill_C",
    };

    public static readonly IReadOnlyList<SkillEvent> Reviewed =
    [
        new(
            "/Game/BloodlinesAlpha/UI/CharacterSheet/DiseaseManager:Become Vampire Event",
            "0b5959334d432c8a",
            "/Game/UltimateSkillSystem/SkillSystemSkill/Skill_Feed.Skill_Feed_C",
            "Becoming a vampire"
        ),
        new(
            "/Game/Overworld/OpenWorldAreas/Askil_Cities/Karraq_Askil_City:Move to Player__FinishedFunc",
            "11b1b2ce3fa7dcbb",
            "/Game/UltimateSkillSystem/SystemDemonstration/ExampleSkills/Skill_WarpBolt.Skill_WarpBolt_C",
            "An event in Karraq"
        ),
        new(
            "/Game/Overworld/OpenWorldAreas/TheCollapsedCrag:Reward Bloodrain Skill",
            "ad10564a8c47e720",
            "/Game/UltimateSkillSystem/SkillSystemSkill/Skill_BloodRain.Skill_BloodRain_C",
            "A reward in The Collapsed Crag"
        ),
        new(
            "/Game/Overworld/OpenWorldAreas/TheElderglenWoods:Esros Comment on Fall",
            "a568d112c61e421f",
            "/Game/Skill_CloudCatch.Skill_CloudCatch_C",
            "A fall in The Elderglen Woods"
        ),
        new(
            "/Game/Overworld/OpenWorldAreas/TombOfTheBetrayer_Interior_Section:ReceiveBeginPlay",
            "34769a810559ebff",
            "/Game/UltimateSkillSystem/SkillSystemSkill/DropKick_Skill.DropKick_Skill_C",
            "Entering the Tomb of the Betrayer"
        ),
        new(
            "/Game/BloodlinesAlpha/Blueprints/FirstPerson2/FirstPersonCharacter2:GiveAllSkills",
            "ca6bd5ec9753a6b2",
            null,
            null,
            "A developer command."
        ),
        new(
            "/Game/BloodlinesAlpha/Blueprints/FirstPersonGameMode:Recieve Console Command",
            "cd5387d8b37024a5",
            null,
            null,
            "A console command."
        ),
        new(
            "/Game/BloodlinesAlpha/Blueprints/FirstPersonGameMode:UnlockSkill",
            "7e0a1d15a5ba9c97",
            null,
            null,
            "Called only by the console command."
        ),
        new(
            "/Game/UltimateSkillSystem/SkillBookWidgets/W_SkillBook:Construct",
            "0ef6cc1ff14ded4d",
            null,
            null,
            "Lists the skills a player has already learned."
        ),
        new(
            "/Game/fantasy_gui_4/widgets/sample_windows/CustomCurrencyShop:Unlock Skill",
            "8cf91a7a8f16742e",
            null,
            null,
            "No currency shop's stock offers a skill."
        ),
        new(
            "/Game/fantasy_gui_4/widgets/sample_windows/CustomCurrencyItemPurchase:BndEvt__CustomCurrencyItemPurchase_PurchaseButton_K2Node_ComponentBoundEvent_0_OnButtonPressedEvent__DelegateSignature",
            "446c356b54a6557f",
            null,
            null,
            "No currency shop's stock offers a skill."
        ),
    ];

    public IEnumerable<EntitySource> FindSources(CatalogContext context)
    {
        if (context.Records.Code is not { } code)
            yield break;
        var reviewed = Reviewed.ToDictionary(entry => entry.Citation, StringComparer.Ordinal);
        foreach (
            var function in code.Functions.Where(function =>
                function.Calls.Any(UnlockCalls.Contains)
                && !Mechanisms.Contains(function.Owner.Split('.')[^1])
            )
        )
        {
            if (!reviewed.TryGetValue(function.Citation, out var entry))
                throw new InvalidDataException(
                    $"{function.Citation} unlocks a skill, but SkillEventMechanism.Reviewed has no entry for it."
                );
            if (!function.Fingerprint.StartsWith(entry.Fingerprint, StringComparison.Ordinal))
                throw new InvalidDataException(
                    $"{function.Citation} changed since its skill event was reviewed. Review its entry in SkillEventMechanism.Reviewed."
                );
            if (entry.Skill is null || entry.Text is null)
                continue;
            yield return new EntitySource(
                entry.Skill,
                new Source(SourceKind.Event, entry.Text, [], null, null, [], [])
                {
                    Entity = new EntityReference(
                        EntityKind.Activity,
                        function.Citation,
                        entry.Text,
                        null,
                        null
                    ),
                }
            )
            {
                Kind = EntityKind.Skill,
            };
        }
        // A reviewed function that its package no longer has means the game moved the event, and its text may be
        // stale. Code without the package at all, such as invented test records, has nothing to compare.
        var packages = code.Functions.Select(function => function.Package).ToHashSet(StringComparer.Ordinal);
        foreach (var entry in Reviewed)
        {
            if (
                packages.Contains(entry.Citation.Split(':')[0])
                && !code.Functions.Any(function => function.Citation == entry.Citation)
            )
                throw new InvalidDataException(
                    $"SkillEventMechanism.Reviewed names {entry.Citation}, which its package no longer has."
                );
        }
    }
}
