using System.Text.Json.Nodes;
using Bloodline.Catalog;
using Bloodline.Records;

namespace Bloodline.Tests.Catalog;

public sealed class ConsoleCommandsTests
{
    private static TableRow Row(string name, string item) =>
        new(name, new JsonObject { ["ItemClass"] = new JsonObject { ["$ref"] = item } });

    [Fact]
    public void GivesOnlyItemsWhoseRowNameSurvivesTheConsoleParsing()
    {
        var commands = new ConsoleCommands([
            new TableRecord(
                "/Game/Invented/AllLootSpawnRates.AllLootSpawnRates",
                null,
                [
                    Row("Bread", "/Game/Invented/Bread.Bread_C"),
                    // The console strips digits and underscores, so these rows cannot be typed.
                    Row("Tier1ArmorRune", "/Game/Invented/Rune.Rune_C"),
                    Row("Arrow_General", "/Game/Invented/Arrow.Arrow_C"),
                    // A second, typable row for the same item still gives it.
                    Row("ArrowGeneral", "/Game/Invented/Arrow.Arrow_C"),
                ]
            ),
            new TableRecord(
                "/Game/Invented/OtherTable.OtherTable",
                null,
                [Row("Cheese", "/Game/Invented/Cheese.Cheese_C")]
            ),
        ]);

        Assert.Equal("AddItem Bread", commands.For("/Game/Invented/Bread.Bread_C"));
        Assert.Null(commands.For("/Game/Invented/Rune.Rune_C"));
        Assert.Equal("AddItem ArrowGeneral", commands.For("/Game/Invented/Arrow.Arrow_C"));
        Assert.Null(commands.For("/Game/Invented/Cheese.Cheese_C"));
    }
}
