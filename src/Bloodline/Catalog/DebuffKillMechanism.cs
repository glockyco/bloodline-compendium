using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

/// <summary>
/// Creatures give an item when they die with Divine Bond or Soulsap on them. Begin Divine Bond and Begin Soulsap in
/// EnemyStatusEffectManager bind Death Event and Grant Spirit Essence to the creature's Death Dispatcher Event, which
/// Goblin_Base:Die calls, and each adds the creature's Arcane Kill Item or Necromancy Kill Item to the inventory. End
/// Divine Bond and End Soulsap unbind them, so the creature has to die while the debuff lasts. Every creature that
/// can be met inherits Goblin_Base:Die and the kill items of Goblin_Base, and the ways that apply the debuffs cast
/// what they hit to Goblin_Base first, so the item comes from every creature these can debuff.
/// </summary>
internal sealed class DebuffKillMechanism : ISourceMechanism
{
    private const string List = "DebuffKillMechanism";
    private const string GoblinBase =
        "/Game/AIBehaviorToolkit/Blueprints/Goblin_Base.Goblin_Base_C";
    private const string Skills = "/Game/UltimateSkillSystem/SkillSystemSkill/";
    private const string Interactables = "/Game/BloodlinesAlpha/Interactables/";
    private const string WandParts = Interactables + "Crafting_Items/WeaponComponents/";

    // Assign Wand Standard Attack gives a wand with one of these shafts the Rend normal attack, a short sweep in front
    // of you, and Staff Attack lets a Rend hit apply its core's debuff on 15% of hits: Divine Bond for an Arcane core
    // and Soulsap for a Necromancy core. The other shafts fire bolts, which apply neither.
    private const string RendShafts =
        "[Bloodwrought Helix Wand Shaft](item:bloodwrought-helix-wand-shaft) or [Golden Talon Wand Shaft](item:golden-talon-wand-shaft)";

    private sealed record Debuff(string Name, string KillItemField, Way[] Ways);

    private sealed record Way(
        EntityKind Kind,
        string? Key,
        string Name,
        string Citation,
        string Fingerprint,
        string Text
    );

    private static readonly Debuff[] Debuffs =
    [
        new(
            "Divine Bond",
            "Arcane Kill Item",
            [
                new(
                    EntityKind.Skill,
                    Skills + "Skill_HolyJavelin.Skill_HolyJavelin_C",
                    "Holy Javelin",
                    Skills + "HolyJaveln_impale:ReceiveBeginPlay",
                    "e5be5810e028",
                    "Applies Divine Bond to creatures it impales."
                ),
                new(
                    EntityKind.Skill,
                    Skills + "Skill_HallowedEarth.Skill_HallowedEarth_C",
                    "Hallowed Earth",
                    "/Game/UltimateSkillSystem/HallowedGround:Curse",
                    "e50adc7c93f4",
                    "Applies Divine Bond to creatures that walk on its ground."
                ),
                new(
                    EntityKind.Skill,
                    Skills + "Skill_ArcaneTap.Skill_ArcaneTap_C",
                    "Arcane Tap",
                    Skills + "Skill_ArcaneTap:OnEffect",
                    "de0c95d52094",
                    "Applies Divine Bond to creatures it hits."
                ),
                new(
                    EntityKind.Item,
                    Interactables + "BlessedBlaststone.BlessedBlaststone_C",
                    "Blessed Blaststone",
                    "/Game/Blueprints/BlessedBlaststone_Thrown:Special Hit",
                    "402f6107306a",
                    "Applies Divine Bond to creatures it hits when thrown."
                ),
                new(
                    EntityKind.Item,
                    Interactables + "Armor/Arms/Arm_Armor_MonkWraps.Arm_Armor_MonkWraps_C",
                    "Monk's Wraps",
                    Interactables
                        + "Armor/Arms/Arm_Armor_MonkWraps:ExecuteUbergraph_Arm_Armor_MonkWraps",
                    "401bc03ee759",
                    "Unarmed attacks while you wear them apply Divine Bond to creatures they hit."
                ),
                new(
                    EntityKind.Item,
                    WandParts + "WC_LightbornWandCore.WC_LightbornWandCore_C",
                    "Lightborn Wand Core",
                    Interactables
                        + "Weapons/1HandedWeapons/CustomWeapon_Base:ExecuteUbergraph_CustomWeapon_Base",
                    "019adb9288df",
                    $"A wand with this core and a {RendShafts} applies Divine Bond with 15% of the hits of its normal attack."
                ),
                new(
                    EntityKind.Activity,
                    null,
                    "Arcane Enchantment",
                    "/Game/BloodlinesAlpha/Enchantment/EnchantedWeaponHandler:ExecuteUbergraph_EnchantedWeaponHandler",
                    "7f2714ef2794",
                    "Blocking with a weapon that has the Arcane enchantment applies Divine Bond to creatures in front of you."
                ),
                new(
                    EntityKind.Activity,
                    null,
                    "Arcane Pulse",
                    "/Game/BloodlinesAlpha/UI/CharacterSheet/CustomArmorComponent:ExecuteUbergraph_CustomArmorComponent",
                    "de1a123ed2b3",
                    "Armor attuned with Arcane Pulse applies Divine Bond to creatures around you when your health falls to 35% or less."
                ),
            ]
        ),
        new(
            "Soulsap",
            "Necromancy Kill Item",
            [
                new(
                    EntityKind.Skill,
                    Skills + "Skill_Absorb.Skill_Absorb_C",
                    "Absorb",
                    Skills + "Skill_Absorb:OnEffect",
                    "e4f63f5837b3",
                    "Applies Soulsap to creatures it hits."
                ),
                new(
                    EntityKind.Skill,
                    Skills + "Skill_Soulbeam.Skill_Soulbeam_C",
                    "Soul Beam",
                    Skills + "Skill_Soulbeam:Beam Cast__UpdateFunc",
                    "8b225413a6e4",
                    "Applies Soulsap to creatures its beam hits."
                ),
                new(
                    EntityKind.Item,
                    Interactables + "SoulsapperMine.SoulsapperMine_C",
                    "Soulsapper Mine",
                    "/Game/Blueprints/SoulsapperMine_Actor:BndEvt__SoulsapperMine_Actor_Sphere_K2Node_ComponentBoundEvent_1_ComponentBeginOverlapSignature__DelegateSignature",
                    "d3867dbb5e8c",
                    "Once placed, applies Soulsap to creatures that step on it."
                ),
                new(
                    EntityKind.Item,
                    WandParts + "WC_MourningCore.WC_MourningCore_C",
                    "Mourning Wand Core",
                    Interactables
                        + "Weapons/1HandedWeapons/CustomWeapon_Base:ExecuteUbergraph_CustomWeapon_Base",
                    "019adb9288df",
                    $"A wand with this core and a {RendShafts} applies Soulsap with 15% of the hits of its normal attack."
                ),
            ]
        ),
    ];

    public IEnumerable<EntitySource> FindSources(CatalogContext context)
    {
        if (
            ReadFunction.Find(
                context,
                "/Game/AIBehaviorToolkit/Blueprints/EnemyStatusEffectManager:ExecuteUbergraph_EnemyStatusEffectManager",
                "57ae492973ba",
                List
            )
                is null
            || ReadFunction.Find(
                context,
                "/Game/AIBehaviorToolkit/Blueprints/Goblin_Base:Die",
                "45b81233a9fd",
                List
            )
                is null
            || !context.Classes.TryGetValue(GoblinBase, out var goblinBase)
        )
            yield break;
        foreach (
            var (citation, fingerprint) in new[]
            {
                (
                    "/Game/BloodlinesAlpha/Interactables/Weapons/1HandedWeapons/CustomWeapon_Base:Assign Wand Element",
                    "62531bd7907b"
                ),
                (
                    "/Game/BloodlinesAlpha/Interactables/Weapons/1HandedWeapons/CustomWeapon_Base:Assign Wand Standard Attack",
                    "0964f7afd569"
                ),
            }
        )
            if (ReadFunction.Find(context, citation, fingerprint, List) is null)
                yield break;
        foreach (var debuff in Debuffs)
        {
            var item =
                RecordValues.AnyReference(goblinBase.Properties[debuff.KillItemField])
                ?? throw new InvalidDataException(
                    $"Goblin_Base has no {debuff.KillItemField}. Update {List}."
                );
            // A creature with its own kill item would give something else, which these rows would not say.
            var other = context.Classes.Values.FirstOrDefault(type =>
                type.Ancestors.Contains(GoblinBase, StringComparer.OrdinalIgnoreCase)
                && type.Properties.TryGetPropertyValue(debuff.KillItemField, out var value)
                && !string.Equals(
                    RecordValues.AnyReference(value),
                    item,
                    StringComparison.OrdinalIgnoreCase
                )
            );
            if (other is not null)
                throw new InvalidDataException(
                    $"{other.Path} has its own {debuff.KillItemField}. Update {List}."
                );
            foreach (var way in debuff.Ways)
            {
                if (ReadFunction.Find(context, way.Citation, way.Fingerprint, List) is null)
                    continue;
                yield return new EntitySource(
                    item,
                    new Source(SourceKind.DebuffKill, way.Name, [], null, new Amount(1, 1), [], [])
                    {
                        Entity = WorldSourceData.Reference(way.Kind, way.Key, way.Name),
                        Triggers = [way.Text],
                    }
                );
            }
        }
    }
}
