using Bloodline.SiteData;

namespace Bloodline.Catalog;

/// <summary>
/// The Chiseling Station takes a Chiselstone and uncovers a find whose kind depends on the shape it takes, and whose
/// quality depends on how intact the player keeps it, read with <c>bloodline script</c> and observed in the lab
/// (design-facts items 39 and 40).
/// </summary>
internal sealed class ChiselingMechanism : ISourceMechanism
{
    private const string List = "ChiselingMechanism";
    private const string Station = "/Game/BloodlinesAlpha/Interactables/ChiselingStation";
    private const string Fossil = "/Game/BloodlinesAlpha/Interactables/Fossil_Base";
    private const string Items = "/Game/BloodlinesAlpha/Interactables/Crafting_Items/";
    private const string Chiselstone = Items + "Ore_And_Ingots/Hollowstone.Hollowstone_C";

    /// <summary>
    /// Each item with the chance that a Chiselstone holds its kind of find and how intact the find has to end for
    /// this quality. <c>Fossil_Base:ReceiveBeginPlay</c> draws one of 25 shapes and <c>Set Mesh Scale</c> gives each
    /// its kind: 4 Creature Fossil, 5 Human Fossil, 11 Treasure Artifact and 5 armor rune, which is Tier 1 with 50%,
    /// otherwise Tier 3 with 40%, otherwise Tier 2. <c>Grant Item</c> gives the quality by the grade that
    /// <c>Calculate Reward</c> sets from the remaining health.
    /// </summary>
    private static readonly (string Item, double Chance, string? Intact)[] Finds =
    [
        (
            Items + "Ore_And_Ingots/HumanFossil_A_Grade.HumanFossil_A_Grade_C",
            5 / 25.0,
            "Keep at least 75% of it intact."
        ),
        (
            Items + "Ore_And_Ingots/HumanFossil_B_Grade.HumanFossil_B_Grade_C",
            5 / 25.0,
            "Keep 50% to 74% of it intact."
        ),
        (
            Items + "Ore_And_Ingots/HumanFossil_C_Grade.HumanFossil_C_Grade_C",
            5 / 25.0,
            "Keep less than 50% of it intact."
        ),
        (
            Items + "Ore_And_Ingots/CreatureFossil_A_Grade.CreatureFossil_A_Grade_C",
            4 / 25.0,
            "Keep at least 75% of it intact."
        ),
        (
            Items + "Ore_And_Ingots/CreatureFossil_B_Grade.CreatureFossil_B_Grade_C",
            4 / 25.0,
            "Keep 50% to 74% of it intact."
        ),
        (
            Items + "Ore_And_Ingots/CreatureFossil_C_Grade.CreatureFossil_C_Grade_C",
            4 / 25.0,
            "Keep less than 50% of it intact."
        ),
        (
            Items + "Ore_And_Ingots/TreasureArtifact_A_Grade.TreasureArtifact_A_Grade_C",
            11 / 25.0,
            "Keep at least 90% of it intact."
        ),
        (
            Items + "Ore_And_Ingots/TreasureArtifact_B_Grade.TreasureArtifact_B_Grade_C",
            11 / 25.0,
            "Keep 75% to 89% of it intact."
        ),
        (
            Items + "Ore_And_Ingots/TreasureArtifact_C_Grade.TreasureArtifact_C_Grade_C",
            11 / 25.0,
            "Keep less than 75% of it intact."
        ),
        (Items + "Generic_Loot/Tier1ArmorRune.Tier1ArmorRune_C", 5 / 25.0 * 0.5, null),
        (Items + "Generic_Loot/Tier2ArmorRune.Tier2ArmorRune_C", 5 / 25.0 * 0.5 * 0.6, null),
        (Items + "Generic_Loot/Tier3ArmorRune.Tier3ArmorRune_C", 5 / 25.0 * 0.5 * 0.4, null),
    ];

    public IEnumerable<EntitySource> FindSources(CatalogContext context)
    {
        if (
            ReadFunction.Find(context, Station + ":Grant Item", "f516898da8a0", List) is null
            || ReadFunction.Find(context, Station + ":Calculate Reward", "5623071bfb94", List)
                is null
            || ReadFunction.Find(context, Fossil + ":Set Mesh Scale", "74c40d6aea73", List) is null
            || ReadFunction.Find(context, Fossil + ":ReceiveBeginPlay", "c1f4e67db688", List)
                is null
        )
            yield break;
        var places = context
            .Placements.Where(placement => placement.Actor.Class.Split('.')[0] == Station)
            .Select(placement => context.Places.Get(placement.Level))
            .Distinct()
            .ToArray();
        if (places.Length == 0)
            yield break;
        var station = WorldSourceData.Reference(
            EntityKind.Building,
            Station + ".ChiselingStation_C",
            "Chiseling Station"
        );
        foreach (var (item, chance, intact) in Finds)
        {
            if (!context.Items.ContainsKey(item))
                throw new InvalidDataException(
                    $"{item} is gone. Read the Chiseling Station again and update {List}."
                );
            yield return new EntitySource(
                item,
                new Source(
                    SourceKind.Chiseling,
                    "Chiseling Station",
                    places,
                    null,
                    new Amount(1, 1),
                    [new ItemAmount(context.ItemLink(Chiselstone), 1)],
                    []
                )
                {
                    Entity = station,
                    Odds = new Odds(OddsEvent.Chiselstone, chance, chance, null),
                    Triggers = intact is null ? [] : [intact],
                }
            );
        }
    }
}
