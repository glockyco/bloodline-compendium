using Bloodline.Catalog;

namespace Bloodline.Tests.Catalog;

public sealed class GameEventsTests
{
    [Fact]
    public void WorldMapChanceIsItsWeightShareAndIsNullWhenAnEventIsAbsent()
    {
        var context = EventRecords.Create(
            new Dictionary<string, IReadOnlyDictionary<string, double>>
            {
                ["Pine Forest Events"] = new Dictionary<string, double>
                {
                    ["Bear"] = 1,
                    ["Wolf Pack"] = 3,
                },
                ["Birch Forest Events"] = new Dictionary<string, double> { ["Bear"] = 2 },
            }
        );

        var bear = context.Events.WorldMap.Single(happening => happening.Name == "Bear");
        var wolf = context.Events.WorldMap.Single(happening => happening.Name == "Wolf Pack");

        Assert.Equal(0.25, bear.Chances[0]!.Value, 10);
        Assert.Equal(1.0, bear.Chances[1]!.Value, 10);
        Assert.Null(bear.Chances[2]);
        Assert.Equal(0.75, wolf.Chances[0]!.Value, 10);
        Assert.Null(wolf.Chances[1]);
    }

    [Fact]
    public void UnnamedTerrainTagUsesTheFirstWorldMapList()
    {
        var context = EventRecords.Create(
            new Dictionary<string, IReadOnlyDictionary<string, double>>
            {
                ["Pine Forest Events"] = new Dictionary<string, double> { ["Bear"] = 1 },
            },
            ["Invented Forest"]
        );

        Assert.Contains("Invented Forest", context.Events.Terrains[0].Terrains);
    }

    [Fact]
    public void WorldMapEventWithoutAKnownStayTimeFailsWithItsName()
    {
        var context = EventRecords.Create(
            new Dictionary<string, IReadOnlyDictionary<string, double>>
            {
                ["Pine Forest Events"] = new Dictionary<string, double> { ["New Event"] = 1 },
            }
        );

        var error = Assert.Throws<InvalidDataException>(() => _ = context.Events.WorldMap);

        Assert.Contains("New Event", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CalendarChancesSumToOneAndAreaLimitedChanceIsRateShareOverAreas()
    {
        var context = EventRecords.Create(
            worldMap: false,
            calendar: true,
            calendarRows:
            [
                EventRecords.CalendarRow("Everywhere", "All areas", 2, "ALL"),
                EventRecords.CalendarRow("One area", "One area", 3, "Area 1"),
            ]
        );

        Assert.Equal(1, context.Events.Calendar.Sum(happening => happening.Chance), 10);
        Assert.Equal(
            0.6 / 18,
            context.Events.Calendar.Single(happening => happening.Name == "One area").Chance,
            10
        );
    }

    [Fact]
    public void CalendarWithWrongAreaCountFails()
    {
        var context = EventRecords.Create(
            worldMap: false,
            calendar: true,
            areaCount: 17,
            calendarRows: [EventRecords.CalendarRow("Everywhere", "All areas", 1, "ALL")]
        );

        var error = Assert.Throws<InvalidDataException>(() => _ = context.Events.Calendar);

        Assert.Contains("number of areas", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ArmyEventSpawnsOnlyTroopsWithPositiveWeight()
    {
        var context = EventRecords.Create(
            new Dictionary<string, IReadOnlyDictionary<string, double>>
            {
                ["Pine Forest Events"] = new Dictionary<string, double> { ["Band of Goblins"] = 1 },
            },
            armyTroops: [("/Game/Invented/Robber.Robber_C", 1), ("/Game/Invented/Other.Other_C", 0)]
        );

        Assert.Equal(
            "/Game/Invented/Robber.Robber_C",
            Assert.Single(context.Events.WorldMap).Spawns.Single()
        );
    }
}
