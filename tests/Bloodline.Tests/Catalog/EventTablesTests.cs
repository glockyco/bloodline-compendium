using System.Text.Json.Nodes;
using Bloodline.Catalog;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Tests.Catalog;

public sealed class EventTablesTests
{
    private const string PageName = "Invented Events";

    private static MechanicContent Page(string data) =>
        new(PageName, "", [new MechanicSection("Events", []) { Data = data }], ["invented"]);

    [Fact]
    public void WorldMapEventTableNamesItsSpawnerCreatureAndClearsData()
    {
        var context = BanditContext();
        var page = Assert.Single(EventTables.Fill([Page("worldMapEvents")], context));
        var section = Assert.Single(page.Sections);
        var table = section.Table!;

        Assert.Null(section.Data);
        Assert.Equal(new[] { "Bandit Ambush", "Robber", "15 seconds" }, Assert.Single(table.Rows));
        Assert.Equal(["bandit-ambush"], table.Anchors);
    }

    [Fact]
    public void MergedWorldMapTableShowsOnlyPossibleTerrainsOnTheLinkedEventRow()
    {
        var context = BanditContext();
        var table = Assert
            .Single(
                Assert
                    .Single(EventTables.Fill([Page("worldMapEventsWithChances")], context))
                    .Sections
            )
            .Table!;

        Assert.Equal(
            ["Event", "What It Brings", "Chance by Terrain", "Stays on the Map"],
            table.Headings
        );
        Assert.Equal(["bandit-ambush"], table.Anchors);
        Assert.Equal(["Bandit Ambush", "Robber", "", "15 seconds"], Assert.Single(table.Rows));
        Assert.Equal(
            new MechanicChance(1, "Pine Forest", "terrain"),
            Assert.Single(Assert.Single(table.ChanceColumns![2]))
        );
    }

    [Fact]
    public void SeparateWorldMapChanceTableKeepsZeroesAndFractions()
    {
        var table = Assert
            .Single(
                Assert
                    .Single(EventTables.Fill([Page("worldMapEventChances")], BanditContext()))
                    .Sections
            )
            .Table!;

        Assert.Equal("Pine Forest", table.Headings[1]);
        Assert.Equal(new MechanicChance(1), Assert.Single(Assert.Single(table.ChanceColumns![1])));
        Assert.Equal(new MechanicChance(0), Assert.Single(Assert.Single(table.ChanceColumns[2])));
    }

    [Fact]
    public void CalendarAreaSectionNamesTheManagersAreas()
    {
        var context = EventRecords.Create(worldMap: false, calendar: true);
        var section = Assert.Single(
            Assert.Single(EventTables.Fill([Page("calendarEventAreas")], context)).Sections
        );

        Assert.Null(section.Data);
        Assert.Null(section.Table);
        Assert.Equal(
            string.Join(", ", Enumerable.Range(1, 18).Select(number => $"Area {number}")) + ".",
            Assert.Single(section.Paragraphs)
        );
    }

    [Fact]
    public void CalendarTableNamesEligibleAreasAndExposesFractionsForChanceCells()
    {
        var context = EventRecords.Create(
            worldMap: false,
            calendar: true,
            calendarRows:
            [
                EventRecords.CalendarRow("Moonstone Meteorite", "A meteor falls.", 1, "ALL"),
                EventRecords.CalendarRow("Rabbit Swarm", "Rabbits appear.", 1, "Area 1"),
            ]
        );
        var table = Assert
            .Single(Assert.Single(EventTables.Fill([Page("calendarEvents")], context)).Sections)
            .Table!;

        Assert.Equal(
            ["Moonstone Meteorite", "A meteor falls.", "All calendar event areas", ""],
            table.Rows[0]
        );
        Assert.Equal(["Rabbit Swarm", "Rabbits appear.", "Area 1", ""], table.Rows[1]);
        Assert.Equal(1.0 / 36, Assert.Single(table.ChanceColumns![3][1]).Value, 10);
    }

    [Fact]
    public void UnknownEventTableNameFailsWithPageName()
    {
        var context = EventRecords.Create(worldMap: false);

        var error = Assert.Throws<InvalidDataException>(() =>
            EventTables.Fill([Page("noSuchTable")], context)
        );

        Assert.Contains(PageName, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EnemyDropSourceCarriesItsWorldMapEventReference()
    {
        var context = BanditContext();

        var source = Assert.Single(
            new EnemyDropMechanism().FindSources(context).Select(context.SpawnSites.Arriving),
            candidate => candidate.Source.Entity.Key == EventRecords.Robber
        );
        var happening = Assert.Single(source.Source.Events);

        Assert.Equal(SourceEventKind.WorldMap, happening.Kind);
        Assert.Equal("Bandit Ambush", happening.Name);
        Assert.Equal("bandit-ambush", happening.Page.Fragment);
    }

    private static CatalogContext BanditContext()
    {
        var itemProperties = JsonNode
            .Parse("""{"ItemInfo":{"ItemName":{"$text":"Token"},"Quantity":1}}""")!
            .AsObject();
        var robberProperties = new JsonObject
        {
            ["Enemy Name"] = new JsonObject { ["$text"] = "Robber" },
            ["Loot Table"] = EventRecords.ReferenceMap((EventRecords.Item, 1)),
        };
        var spawnerProperties = new JsonObject
        {
            ["Level 1 Spawn Rates"] = EventRecords.ReferenceMap((EventRecords.Robber, 1)),
        };
        return EventRecords.Create(
            new Dictionary<string, IReadOnlyDictionary<string, double>>
            {
                ["Pine Forest Events"] = new Dictionary<string, double> { ["Bandit Ambush"] = 1 },
            },
            additionalClasses:
            [
                new ClassRecord(EventRecords.BanditSpawner, [], spawnerProperties),
                new ClassRecord(EventRecords.Robber, [], robberProperties),
                new ClassRecord(EventRecords.Item, [ItemIdentity.BaseItem], itemProperties),
            ]
        );
    }
}
