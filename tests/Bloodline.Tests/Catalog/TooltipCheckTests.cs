using System.Text.Json.Nodes;
using Bloodline.Catalog;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Tests.Catalog;

public sealed class TooltipCheckTests
{
    private const string Widget = "W_Tooltip_QualityWhite_C";

    private static JsonObject Parse(string json) => JsonNode.Parse(json)!.AsObject();

    private static readonly TableRecord[] Tables =
    [
        new(
            "/Game/Invented/ArmorDataTable.ArmorDataTable",
            null,
            [
                new TableRow(
                    "IronCuirass",
                    Parse("""{"ArmorClass":{"$text":"Heavy"},"ArmorValue":13}""")
                ),
            ]
        ),
        new(
            "/Game/Invented/FoodEffectDataTable.FoodEffectDataTable",
            null,
            [
                new TableRow(
                    "ApplePie",
                    Parse(
                        """{"LocalizedFoodName":{"$text":"Apple Pie"},"FoodEffects":{"$map":[["Heal Player",200]]}}"""
                    )
                ),
            ]
        ),
    ];

    private static ItemPage Page(string path, string itemInfo, string? weapon = null) =>
        new ItemFacts([], Tables).Resolve(
            new ClassRecord(
                path,
                [],
                Parse(
                    "{\"ItemInfo\":"
                        + itemInfo
                        + (weapon is null ? "" : ",\"WeaponInfo\":" + weapon)
                        + "}"
                )
            ),
            "invented"
        );

    private static ItemPage Armor(string name, string slot = "Torso") =>
        Page(
            "/Game/Invented/" + name.Replace(" ", "", StringComparison.Ordinal) + ".Armor_C",
            $$$"""{"ItemName":{"$text":"{{{name}}}"},"ItemType":{"$enum":"E_ItemType::Armor","display":"Armor"},"ItemSlot":{"$enum":"Slot::Invented","display":"{{{slot}}}"},"Damage":4}"""
        );

    private static ItemPage Food(string name) =>
        Page(
            "/Game/Invented/" + name.Replace(" ", "", StringComparison.Ordinal) + ".Food_C",
            $$$"""{"ItemName":{"$text":"{{{name}}}"},"ItemType":{"$enum":"E_ItemType::Food","display":"Food"}}"""
        );

    private static readonly ItemPage Sword = Page(
        "/Game/Invented/Sword.Sword_C",
        """{"ItemName":{"$text":"Sword"},"ItemType":{"$enum":"E_ItemType::Weapon","display":"Weapon"},"Speed":{"$enum":"AttackSpeeds::Slow","display":"Slow"}}""",
        """{"Damage":1000}"""
    );

    private static TooltipObservation Tooltip(
        ItemPage item,
        string stat,
        string? speedOrType = null,
        string run = "run",
        string? widget = Widget,
        bool statHidden = false
    ) =>
        new(
            item.Key,
            run,
            widget,
            new Dictionary<string, string>
            {
                ["TextBlock_0"] = item.Name,
                ["TextBlock_2"] = item.Description ?? "",
                ["TextBlock_3"] = stat,
                ["SpeedTypeBlock"] = speedOrType ?? "",
            },
            [
                .. speedOrType is null ? ["SpeedTypeBlock"] : Array.Empty<string>(),
                .. statHidden ? ["TextBlock_3"] : Array.Empty<string>(),
            ]
        );

    private static IReadOnlyList<TooltipDifference> Differences(
        ItemPage item,
        TooltipObservation tooltip
    ) => TooltipCheck.Check(1, [item], [tooltip], []).Differences;

    [Theory]
    [InlineData("Damage: 1,000", "Slow", null, null)]
    [InlineData("Damage: 40", "Slow", "Damage", null)]
    [InlineData("Damage: 1,000", "Fast", "Speed", null)]
    [InlineData(": 0", "Slow", "Stat line", null)]
    [InlineData(": ", "Slow", "Stat line", "unlabeled-stat-line")]
    public void WeaponTooltipsDifferOnlyWhereTheirValuesDo(
        string stat,
        string speed,
        string? field,
        string? reason
    )
    {
        var differences = Differences(Sword, Tooltip(Sword, stat, speed));
        if (field is null)
        {
            Assert.Empty(differences);
            return;
        }
        var difference = Assert.Single(differences);
        Assert.Equal((field, reason), (difference.Field, difference.Reason));
    }

    [Fact]
    public void ArmorDifferencesNameTheirCause()
    {
        var withoutRow = Armor("Leather Pants");
        Assert.All(
            Differences(withoutRow, Tooltip(withoutRow, "ARMOR: 4", "")),
            difference => Assert.Equal("armor-without-row", difference.Reason)
        );
        var amulet = Armor("Iron Cuirass", "Amulet");
        var amuletDifferences = Differences(amulet, Tooltip(amulet, "ARMOR: 13", "Type: Heavy"));
        Assert.Equal(["Armor rating", "Armor type"], amuletDifferences.Select(d => d.Field));
        Assert.All(
            amuletDifferences,
            difference => Assert.Equal("armor-slot-adds-none", difference.Reason)
        );
        var matching = Armor("Iron Cuirass");
        Assert.Empty(Differences(matching, Tooltip(matching, "ARMOR: 13", "Type: Heavy")));
    }

    [Fact]
    public void LinesTheTooltipKeepsHiddenAreNotCompared()
    {
        // Food tooltips keep a filled heal line in a collapsed box, which a player never sees.
        var food = Food("Apple Pie");
        Assert.Empty(Differences(food, Tooltip(food, "Heals: 15hp", statHidden: true)));
        Assert.Equal(
            "Stat line",
            Assert.Single(Differences(food, Tooltip(food, "Heals: 15hp"))).Field
        );
    }

    [Fact]
    public void PotionTooltipsWithoutTheirTextHaveTheirCause()
    {
        var potion = Page(
            "/Game/Invented/Potion.Potion_C",
            """{"ItemName":{"$text":"Health Potion"},"Description":{"$text":"A standard healing potion."},"ItemType":{"$enum":"E_ItemType::Potion","display":"Potion"}}"""
        );
        var tooltip = new TooltipObservation(
            potion.Key,
            "run",
            Widget,
            new Dictionary<string, string>
            {
                ["TextBlock_0"] = "",
                ["TextBlock_2"] = "",
                ["TextBlock_3"] = ": ",
            },
            ["SpeedTypeBlock"]
        );
        Assert.Equal(
            [
                ("Name", "potion-tooltip-text"),
                ("Description", "potion-tooltip-text"),
                ("Stat line", "unlabeled-stat-line"),
            ],
            Differences(potion, tooltip).Select(difference => (difference.Field, difference.Reason))
        );
    }

    [Fact]
    public void FactsAKnownDefectMisstatesCarryWhatTheTooltipShows()
    {
        var pants = Armor("Leather Pants");
        var sword = Sword;
        var report = TooltipCheck.Check(
            1,
            [pants, sword],
            [Tooltip(pants, "ARMOR: 4", ""), Tooltip(sword, "Damage: 40", "Slow")],
            []
        );
        var shown = TooltipCheck
            .WithGameShows([pants, sword], report)
            .ToDictionary(item => item.Name);
        Fact Fact(string item, Stat stat) => shown[item].Facts.Single(fact => fact.Stat == stat);
        Assert.Equal(
            (0d, "4"),
            (
                Fact("Leather Pants", Stat.ArmorRating).Number!.Value,
                Fact("Leather Pants", Stat.ArmorRating).GameShows
            )
        );
        // A difference without a known cause is a catalog question, not a value the page shows.
        Assert.Null(Fact("Sword", Stat.Damage).GameShows);
    }

    [Fact]
    public void ItemsWithoutAReadableTooltipAreUnreadRatherThanCompared()
    {
        var report = TooltipCheck.Check(
            1,
            [Sword, Food("Steak")],
            [Tooltip(Sword, "Damage: 40", "Slow", widget: null)],
            []
        );
        Assert.Equal(0, report.Compared);
        Assert.Empty(report.Differences);
        Assert.Equal(
            [
                ("Steak", "No lab run has read its tooltip."),
                ("Sword", "Hovering it builds no tooltip widget."),
            ],
            report
                .Unread.OrderBy(unread => unread.Name, StringComparer.Ordinal)
                .Select(unread => (unread.Name, unread.Reason))
        );
    }

    [Fact]
    public void ALaterRunsTooltipReplacesAnEarlierOne()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "bloodline-tooltips-" + Guid.NewGuid().ToString("N")
        );
        try
        {
            void Run(string name, params string[] lines)
            {
                Directory.CreateDirectory(Path.Combine(directory, name));
                File.WriteAllLines(Path.Combine(directory, name, "run.jsonl"), lines);
            }
            const string earlier =
                """{"command":"tooltip-batch","ok":true,"result":{"shownOnly":true,"tooltips":[{"item":"/Game/Invented/Sword.Sword_C","widget":"W","text":{"TextBlock_3":"Damage: 1"},"hidden":{}}]}}""";
            const string later =
                """{"command":"tooltip-batch","ok":true,"result":{"shownOnly":true,"tooltips":[{"item":"/Game/Invented/Sword.Sword_C","widget":"W","text":{"TextBlock_3":"Damage: 2"},"hidden":["SpeedTypeBlock"]}]}}""";
            const string failed =
                """{"command":"tooltip-batch","ok":false,"result":{"shownOnly":true,"tooltips":[{"item":"/Game/Invented/Sword.Sword_C","widget":"W","text":{"TextBlock_3":"Damage: 3"}}]}}""";
            // A reading without shownOnly may hold text in collapsed boxes, so even a later one does not count.
            const string unmarked =
                """{"command":"tooltip-batch","ok":true,"result":{"tooltips":[{"item":"/Game/Invented/Sword.Sword_C","widget":"W","text":{"TextBlock_3":"Damage: 4"},"hidden":{}}]}}""";
            Run("2026-01-01T00-00-00-000Z-first", earlier);
            Run(
                "2026-01-02T00-00-00-000Z-second",
                later,
                failed,
                """{"command":"quit","ok":true}"""
            );
            Run("2026-01-03T00-00-00-000Z-third", unmarked);

            var tooltip = Assert.Single(TooltipCheck.Load(directory));
            Assert.Equal("Damage: 2", tooltip.Text["TextBlock_3"]);
            Assert.Equal(["SpeedTypeBlock"], tooltip.Hidden);
            Assert.Equal("2026-01-02T00-00-00-000Z-second", tooltip.Run);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
