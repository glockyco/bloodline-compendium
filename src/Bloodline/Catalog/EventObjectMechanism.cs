using System.Text.Json.Nodes;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

/// <summary>
/// What the objects of calendar events leave for the player to loot, read with <c>bloodline script</c>. The event
/// brings the object to its area for a day, so these ways have the event instead of a place.
/// </summary>
internal sealed class EventObjectMechanism : ISourceMechanism
{
    private const string List = "EventObjectMechanism";
    private const string Items = "/Game/BloodlinesAlpha/Interactables/";
    private const string Crate = Items + "LootableObject.LootableObject_C";

    /// <summary>
    /// <c>DwarvenAirship:Slide To Stop__FinishedFunc</c> scatters 10 crates when the airship of the Airship Crash
    /// event lands, each set to pick 10 to 25 times from this table, which the script builds as a literal.
    /// </summary>
    private static readonly (string Name, double Weight)[] CrashGoods =
    [
        ("SmallGryoscopicStabilizer", 0.1),
        ("SmallPropeller", 0.25),
        ("SparkPlug", 0.35),
        ("MagnifyingLens", 0.5),
        ("MagneticCoil", 0.4),
        ("LooterDrone_Item", 0.35),
        ("Hoverlight", 0.5),
        ("EngineersSpringboard_Item", 0.5),
        ("Condensed_Magma_Item", 0.5),
        ("CombustionEngine", 0.35),
        ("ChunkOfCoal", 1),
        ("CopperIngot", 0.75),
        ("CopperOre", 0.85),
        ("UnyieldingChitin", 0.35),
        ("SilverOre", 0.2),
        ("IronOre", 0.5),
        ("IronIngot", 0.35),
        ("GoldOore", 0.1),
        ("1HSWORD_Dwarven1", 0.5),
        ("1HSWORD_DwarvenHammer", 0.5),
        ("DwarvenUltrasword", 0.15),
        ("DwarvenShortspear", 0.2),
        ("DwarvenShortspear_Iron", 0.2),
    ];

    public IEnumerable<EntitySource> FindSources(CatalogContext context)
    {
        if (
            ReadFunction.Find(
                context,
                "/Game/PuzzleProps/DwarvenAirship:ExecuteUbergraph_DwarvenAirship",
                "ed27fdb4df09",
                List
            )
                is null
            || context.Events.Calendar.All(happening => happening.Name != "Airship Crash")
        )
            yield break;
        var pool = new JsonObject
        {
            ["$map"] = new JsonArray([
                .. CrashGoods.Select(entry =>
                    (JsonNode)
                        new JsonArray(
                            new JsonObject { ["$ref"] = Path(context, entry.Name) },
                            JsonValue.Create(entry.Weight)
                        )
                ),
            ]),
        };
        var name = LootRecords.Name(context.Classes[Crate].Properties, "Object Name", Crate);
        foreach (
            var source in LootRecords.Pool(
                context,
                pool,
                SourceKind.ContainerLoot,
                name,
                [],
                [],
                WorldSourceData.Reference(EntityKind.Container, Crate + "#Airship Crash", name),
                OddsEvent.Container,
                PickCount.Container(10, 25)
            )
        )
            yield return source with
            {
                Source = source.Source with
                {
                    Events = [GameEvents.Reference(SourceEventKind.Calendar, "Airship Crash")],
                },
                UseGiverEvents = false,
            };
    }

    /// <summary>The class path of an item the script names by its class name alone.</summary>
    private static string Path(CatalogContext context, string name) =>
        context.Classes.Keys.Single(path =>
            path.EndsWith("/" + name + "." + name + "_C", StringComparison.Ordinal)
        );
}
