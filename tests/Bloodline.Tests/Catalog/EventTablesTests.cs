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
        Assert.Null(table.CellLinks);
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
        Assert.Equal(
            "Each percentage is the chance that the next event in that terrain is this one.",
            table.ChanceContext
        );
        Assert.Null(table.CellLinks);
        Assert.Equal(["Bandit Ambush", "Robber", "", "15 seconds"], Assert.Single(table.Rows));
        Assert.Equal(
            new MechanicChance(1, "Pine Forest", "terrain"),
            Assert.Single(Assert.Single(table.ChanceColumns![2]))
        );
    }

    [Fact]
    public void SeparateWorldMapEffectsLinkToTheirOwnRowsInTheFiveTerrainMatrix()
    {
        var context = EventRecords.Create(
            new Dictionary<string, IReadOnlyDictionary<string, double>>
            {
                ["Pine Forest Events"] = new Dictionary<string, double>
                {
                    ["Bear"] = 1,
                    ["Bandit Ambush"] = 3,
                },
            }
        );
        var content = Page("worldMapEventChances") with
        {
            Sections =
            [
                new("Chances by Terrain", []) { Data = "worldMapEventChances" },
                new("What Each Event Brings", []) { Data = "worldMapEvents" },
            ],
        };
        var page = Assert.Single(EventTables.Fill([content], context));
        var matrix = page.Sections[0].Table!;
        var effects = page.Sections[1].Table!;

        Assert.Equal(6, matrix.Headings.Count);
        Assert.Equal("Pine Forest", matrix.Headings[1]);
        Assert.Equal(["bandit-ambush", "bear"], effects.Anchors);
        Assert.Equal(["chances-bandit-ambush", "chances-bear"], matrix.Anchors);
        Assert.Equal(
            new MechanicCellLink("chances-bandit-ambush", "Terrain chances for Bandit Ambush"),
            effects.CellLinks![0][0]
        );
        Assert.Equal(
            new MechanicCellLink("chances-bear", "Terrain chances for Bear"),
            effects.CellLinks[0][1]
        );
        Assert.Equal(new MechanicChance(0.25), Assert.Single(matrix.ChanceColumns![1][1]));
        Assert.Equal(new MechanicChance(0), Assert.Single(matrix.ChanceColumns[2][1]));
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
        var content = Page("calendarEvents") with
        {
            Sections =
            [
                new("Events", []) { Data = "calendarEvents" },
                new("Calendar Areas", []) { Data = "calendarEventAreas" },
            ],
        };
        var page = Assert.Single(
            MechanicProducer.Produce(EventTables.Fill([content], context)).Pages
        );
        var table = page.Sections[0].Table!;

        Assert.Equal(
            ["Moonstone Meteorite", "A meteor falls.", "All calendar event areas", ""],
            table.Rows[0]
        );
        Assert.Equal(["Rabbit Swarm", "Rabbits appear.", "Area 1", ""], table.Rows[1]);
        Assert.Equal(new MechanicCellLink("calendar-areas"), table.CellLinks![2][0]);
        Assert.Equal(page.Sections[1].Anchor, table.CellLinks[2][0].Anchor);
        Assert.Single(table.CellLinks[2]);
        Assert.Equal(1.0 / 36, Assert.Single(table.ChanceColumns![3][1]).Value, 10);
    }

    [Fact]
    public void CalendarKeepsTheGhostShipRewardBesideItsAnchorAndOutsideCalendarText()
    {
        var context = EventRecords.Create(
            worldMap: false,
            calendar: true,
            calendarRows:
            [
                EventRecords.CalendarRow(
                    "Ghost Ship",
                    "An Ethereal Airship will appear.",
                    1,
                    "Area 1"
                ),
            ]
        );
        var table = Assert
            .Single(Assert.Single(EventTables.Fill([Page("calendarEvents")], context)).Sections)
            .Table!;

        Assert.Equal("ghost-ship", Assert.Single(table.Anchors!));
        Assert.Equal("An Ethereal Airship will appear.", Assert.Single(table.Rows)[1]);
        Assert.Equal("Destroy the ship to find its chest.", table.RowNotes![0]);
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
    public void CreatureBroughtOnlyByAnEventIsAnAvailableWay()
    {
        var context = BanditContext();

        var source = Assert.Single(
            new EnemyDropMechanism().FindSources(context).Select(context.SpawnSites.Arriving),
            candidate => candidate.Source.Entity.Key == EventRecords.Robber
        );
        var happening = Assert.Single(source.Source.Events);

        Assert.Empty(source.Source.Places);
        Assert.False(source.Source.Unconfirmed);
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
