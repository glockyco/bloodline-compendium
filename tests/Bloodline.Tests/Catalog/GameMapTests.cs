using System.Text.Json.Nodes;
using Bloodline.Catalog;
using Bloodline.Code;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Tests.Catalog;

public sealed class GameMapTests
{
    private const string Mount = "/Game/Invented/Mounts/Base.Base_C";

    private static CodeFunction Function(string package, string name, bool window = false) =>
        new(package, package, name, window, "", CodeParseStatus.Complete, [], [], [], []);

    private static (GameMapReport Map, IReadOnlyList<string> Unclassified) Build(
        IEnumerable<string>? readings = null
    )
    {
        var records = new CatalogRecords(
            new BuildRecord(1, 1, []),
            [
                new ClassRecord("/Game/Invented/Stable.Stable_C", [], new JsonObject()),
                new ClassRecord("/Game/Invented/Sword.Sword_C", [], new JsonObject()),
                // A mount outside the mounts' folder still belongs to them by what it derives from.
                new ClassRecord("/Game/Elsewhere/Camel.Camel_C", [Mount], new JsonObject()),
                new ClassRecord("/Game/Effects/Glow.Glow_C", [], new JsonObject()),
                new ClassRecord("/Game/Effects/Torch.Torch_C", [], new JsonObject()),
                new ClassRecord("/Game/NewFolder/Thing.Thing_C", [], new JsonObject()),
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
                        new ActorRecord("Torch", "/Game/Effects/Torch.Torch_C", new JsonObject()),
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
        var registry = new GameMapRegistry([
            new MapRule("Mounts", Ancestor: Mount),
            new MapRule("Areas", Kind: "level"),
            new MapRule("Effects", Prefix: "/Game/Effects/", SetAside: "Only looks."),
            new MapRule("Stables", Prefix: "/Game/Invented/"),
        ]);
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
        return GameMap.Build(
            records,
            registry,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "/Game/Invented/Sword",
                "/Game/Effects/Glow",
            },
            claims,
            readings
        );
    }

    [Fact]
    public void ListsEveryPackageWithItsSystemAndWhatTheSiteRestsOn()
    {
        var map = Build().Map.Packages.ToDictionary(package => package.Path);

        var stable = map["/Game/Invented/Stable"];
        Assert.Equal(["class", "code"], stable.Kinds);
        Assert.Equal(2, stable.Functions);
        Assert.Equal(1, stable.CitedFunctions);
        // Two actors in one level count as one level.
        Assert.Equal(1, stable.Levels);
        Assert.Equal("Stables", stable.System);
        // A cited function publishes its package, and so does a name in the site data.
        Assert.Equal(MapState.Published, stable.State);
        Assert.Equal(MapState.Published, map["/Game/Invented/Sword"].State);
        Assert.Equal(0, map["/Game/Invented/Sword"].CitedFunctions);
        // Nothing names the window or the table, and no rule sets them aside.
        Assert.True(map["/Game/Invented/StableWindow"].Window);
        Assert.Equal(MapState.Open, map["/Game/Invented/StableWindow"].State);
        Assert.Equal(MapState.Open, map["/Game/Invented/Prices"].State);
        Assert.Equal("Mounts", map["/Game/Elsewhere/Camel"].System);
        Assert.Equal("Areas", map["/Game/Invented/Town"].System);
        // What the site names is published even in a folder the registry sets aside.
        Assert.Equal(MapState.Published, map["/Game/Effects/Glow"].State);
        Assert.Equal(MapState.SetAside, map["/Game/Effects/Torch"].State);
        Assert.Equal("Only looks.", map["/Game/Effects/Torch"].Reason);
    }

    [Fact]
    public void AFunctionTheCatalogHoldsAsReadPublishesItsPackage()
    {
        var window = Build(["/Game/Invented/StableWindow:Construct"])
            .Map.Packages.Single(package => package.Path == "/Game/Invented/StableWindow");
        Assert.Equal(MapState.Published, window.State);
        Assert.Equal(1, window.CitedFunctions);
    }

    [Fact]
    public void NamesThePackagesThatNoRuleCovers()
    {
        var (map, unclassified) = Build();
        Assert.Equal(["/Game/NewFolder/Thing"], unclassified);
        Assert.DoesNotContain(map.Packages, package => package.Path == "/Game/NewFolder/Thing");
    }

    [Fact]
    public void SumsEachSystemAndShowsWhatAPlayerMeetsUnread()
    {
        var systems = Build().Map.Systems.ToDictionary(system => system.Name);
        var stables = systems["Stables"];
        Assert.Equal(4, stables.Packages);
        Assert.Equal(2, stables.Published);
        Assert.Equal(2, stables.Open);
        Assert.Equal(1, stables.OpenFunctions);
        Assert.Equal(["/Game/Invented/StableWindow"], stables.OpenWindows);
        // A set-aside class that stands in a level is listed, so a folder rule cannot hide it.
        Assert.Equal(["/Game/Effects/Torch"], systems["Effects"].SetAsidePlaced);
    }
}
