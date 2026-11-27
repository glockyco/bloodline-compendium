using System.Text.Json.Nodes;
using Bloodline.Catalog;
using Bloodline.Code;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Tests.Catalog;

public sealed class GameMapTests
{
    private static CodeFunction Function(string package, string name, bool window = false) =>
        new(package, package, name, window, "", CodeParseStatus.Complete, [], [], [], []);

    [Fact]
    public void ListsEveryPackageWithWhatTheSiteRestsOn()
    {
        var records = new CatalogRecords(
            new BuildRecord(1, 1, []),
            [
                new ClassRecord("/Game/Invented/Stable.Stable_C", [], new JsonObject()),
                new ClassRecord("/Game/Invented/Sword.Sword_C", [], new JsonObject()),
            ],
            [new TableRecord("/Game/Invented/Prices.Prices", null, [])],
            [],
            [
                new LevelRecord(
                    "/Game/Invented/Town",
                    [],
                    [
                        new ActorRecord(
                            "Stable1",
                            "/Game/Invented/Stable.Stable_C",
                            new JsonObject()
                        ),
                        new ActorRecord(
                            "Stable2",
                            "/Game/Invented/Stable.Stable_C",
                            new JsonObject()
                        ),
                    ]
                ),
            ],
            [],
            [],
            []
        )
        {
            Code = new CodeIndex(
                [
                    Function("/Game/Invented/Stable", "Open Window"),
                    Function("/Game/Invented/Stable", "Buy Mount"),
                    Function("/Game/Invented/StableWindow", "Construct", window: true),
                ],
                [],
                []
            ),
        };
        var claims = new[]
        {
            new FactClaim(
                EntityKind.Item,
                "/Game/Invented/Sword.Sword_C",
                "price",
                ClaimType.Formula,
                1,
                [],
                [new CodeCitation("/Game/Invented/Stable:Buy Mount", "")]
            ),
        };
        var map = GameMap
            .Build(
                records,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "/Game/Invented/Sword" },
                claims
            )
            .Packages.ToDictionary(package => package.Path);

        // Every record and every package with code is on the map, whether or not the site uses it.
        Assert.Equal(
            [
                "/Game/Invented/Prices",
                "/Game/Invented/Stable",
                "/Game/Invented/StableWindow",
                "/Game/Invented/Sword",
                "/Game/Invented/Town",
            ],
            map.Keys.Order(StringComparer.Ordinal)
        );
        var stable = map["/Game/Invented/Stable"];
        Assert.Equal(["class", "code"], stable.Kinds);
        Assert.Equal(2, stable.Functions);
        Assert.Equal(1, stable.CitedFunctions);
        // Two actors in one level count as one level.
        Assert.Equal(1, stable.Levels);
        // A cited function publishes its package, and so does a name in the site data.
        Assert.True(stable.Published);
        Assert.True(map["/Game/Invented/Sword"].Published);
        Assert.Equal(0, map["/Game/Invented/Sword"].CitedFunctions);
        Assert.True(map["/Game/Invented/StableWindow"].Window);
        Assert.False(map["/Game/Invented/StableWindow"].Published);
        Assert.False(map["/Game/Invented/Prices"].Published);
    }
}
