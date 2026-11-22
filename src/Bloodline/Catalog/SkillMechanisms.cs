using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

/// <summary>
/// A slot of a tree the skill window opens: unlocking its skill costs 1 skill point and needs the slot's required
/// level in the discipline it checks.
/// </summary>
internal sealed class SkillTreeMechanism : ISourceMechanism
{
    public IEnumerable<EntitySource> FindSources(CatalogContext context)
    {
        foreach (var tree in context.SkillTrees.All)
        {
            var discipline = SkillProducer.DisciplineReference(tree);
            foreach (var slot in tree.SkillRows.SelectMany(row => row.Entries))
            {
                if (RecordValues.AnyReference(slot.Properties["Skill"]) is not { } skill)
                    continue;
                var level = SkillProducer.Integer(slot.Properties["Required Level"]) ?? 0;
                var checkedDiscipline = SkillProducer.LevelDiscipline(
                    context.SkillTrees.All,
                    SkillProducer.CheckedLevel(slot) ?? tree.LevelName ?? tree.Name
                );
                yield return new EntitySource(
                    skill,
                    new Source(SourceKind.SkillTree, tree.Name, [], null, null, [], [])
                    {
                        Entity = discipline,
                        Conditions =
                        [
                            new RelationCondition(
                                ConditionKind.DisciplineLevel,
                                checkedDiscipline,
                                level,
                                $"{checkedDiscipline.Name} level {level}"
                            ),
                        ],
                        Cost = new PointCost(1, PointKind.SkillPoint),
                    }
                )
                {
                    Kind = EntityKind.Skill,
                };
            }
        }
    }
}

/// <summary>A skill book placed in a level, which teaches its skill when a player reads it.</summary>
internal sealed class SkillBookMechanism : ISourceMechanism
{
    public IEnumerable<EntitySource> FindSources(CatalogContext context)
    {
        foreach (var placement in context.Placements)
        {
            if (
                !WorldSourceData.IsClass(
                    context,
                    placement.Actor.Class,
                    "SkillBook_InteractionObject_C"
                )
                || RecordValues.AnyReference(placement.Properties["Skill to Learn"])
                    is not { } skill
            )
                continue;
            var place = context.Places.Get(placement.Level);
            yield return new EntitySource(
                skill,
                new Source(SourceKind.SkillBook, "Skill book", [place], null, null, [], [])
                {
                    Entity = place,
                    Conditions = WorldSourceData.Conditions(
                        context,
                        placement.Properties,
                        placement.Level
                    ),
                }
            )
            {
                Kind = EntityKind.Skill,
            };
        }
    }
}

/// <summary>A spellsong note sheet placed in a level, which teaches the spellsong it names.</summary>
internal sealed class NoteSheetMechanism : ISourceMechanism
{
    public IEnumerable<EntitySource> FindSources(CatalogContext context)
    {
        var songs = SkillProducer
            .SpellsongRows(context)
            .ToDictionary(
                row => SkillProducer.Words(row.Row.Values["SpellsongName"]) ?? row.Row.Name,
                row => row.Key,
                StringComparer.OrdinalIgnoreCase
            );
        foreach (var placement in context.Placements)
        {
            if (
                SkillProducer.Words(placement.Properties["Spellsong Name"]) is not { } name
                || !songs.TryGetValue(name, out var key)
            )
                continue;
            var place = context.Places.Get(placement.Level);
            yield return new EntitySource(
                key,
                new Source(SourceKind.NoteSheet, "Note sheet", [place], null, null, [], [])
                {
                    Entity = place,
                    Conditions = WorldSourceData.Conditions(
                        context,
                        placement.Properties,
                        placement.Level
                    ),
                }
            )
            {
                Kind = EntityKind.Spellsong,
            };
        }
    }
}
