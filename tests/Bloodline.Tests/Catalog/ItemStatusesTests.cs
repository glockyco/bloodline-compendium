using System.Text.Json.Nodes;
using Bloodline.Catalog;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Tests.Catalog;

public sealed class ItemStatusesTests
{
    private const string Item = "/Game/Invented/Helm.Helm_C";
    private const string TestLevel = "/Game/Invented/DevTestMap";

    private static JsonObject Holding() =>
        new() { ["Custom Loot Table"] = new JsonArray(new JsonObject { ["$ref"] = Item }) };

    private static ItemStatus Status(int sources, params LevelRecord[] levels)
    {
        var self = new ClassRecord(
            Item,
            [],
            new JsonObject
            {
                ["ItemInfo"] = new JsonObject
                {
                    ["ItemClass"] = new JsonObject { ["$ref"] = Item },
                },
            }
        );
        var records = new CatalogRecords(
            new BuildRecord(1, 1, []),
            [self],
            [],
            [],
            levels,
            [],
            [],
            []
        );
        return new ItemStatuses(records, new HashSet<string>([TestLevel])).Of(Item, sources);
    }

    [Fact]
    public void AnItemThatOnlyTestLevelsHoldIsNotInNormalPlay() =>
        Assert.Equal(
            ItemStatus.NotInNormalPlay,
            Status(
                0,
                new LevelRecord(
                    TestLevel,
                    [],
                    [new ActorRecord("Box", "/Game/Invented/Box.Box_C", Holding())]
                )
            )
        );

    [Fact]
    public void AnItemAPlayedLevelHoldsHasNoKnownSourceUntilOneIsRead() =>
        Assert.Equal(
            ItemStatus.NoKnownSource,
            Status(
                0,
                new LevelRecord(
                    TestLevel,
                    [],
                    [new ActorRecord("Box", "/Game/Invented/Box.Box_C", Holding())]
                ),
                new LevelRecord(
                    "/Game/Invented/Meadow",
                    [],
                    [new ActorRecord("Box", "/Game/Invented/Box.Box_C", Holding())]
                )
            )
        );

    [Fact]
    public void AnItemNamedOnlyByItselfHasNoKnownSource() =>
        Assert.Equal(ItemStatus.NoKnownSource, Status(0));

    [Fact]
    public void AnItemWithASourceIsObtainable() =>
        Assert.Equal(
            ItemStatus.Obtainable,
            Status(
                1,
                new LevelRecord(
                    TestLevel,
                    [],
                    [new ActorRecord("Box", "/Game/Invented/Box.Box_C", Holding())]
                )
            )
        );
}
