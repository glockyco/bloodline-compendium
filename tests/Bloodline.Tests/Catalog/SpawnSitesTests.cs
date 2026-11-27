using System.Text.Json.Nodes;
using Bloodline.Catalog;
using Bloodline.Code;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Tests.Catalog;

public sealed class SpawnSitesTests
{
    private const string Den = "/Game/Invented/Den.Den_C";
    private const string Sleeper = "/Game/Invented/Sleeper.Sleeper_C";
    private const string Beast = "/Game/Invented/Beast.Beast_C";
    private const string Level = "/Game/Invented/Meadow";
    private const string SpawnCall = "/Script/Engine:BeginDeferredActorSpawnFromClass";

    private static CodeFunction Spawns(string owner, string name, string spawned) =>
        new(
            owner.Split('.')[0],
            owner,
            name,
            false,
            "abc123",
            CodeParseStatus.Complete,
            [new CodeTarget(CodeTargetKind.Object, spawned)],
            [],
            [SpawnCall],
            []
        );

    private static CatalogContext Context(JsonObject den, params CodeFunction[] functions)
    {
        var records = new CatalogRecords(
            new BuildRecord(1159290, 1, []),
            [
                new ClassRecord(Den, [], new JsonObject()),
                new ClassRecord(Sleeper, [], new JsonObject()),
                new ClassRecord(Beast, [], new JsonObject()),
            ],
            [],
            [],
            [new LevelRecord(Level, [], [new ActorRecord("Den", Den, den)])],
            [],
            [],
            []
        )
        {
            Code = CodeIndex.Create(functions),
        };
        return new CatalogContext(records, new Dictionary<string, ItemPage>(), []);
    }

    [Fact]
    public void ObjectSpawnedBySitePassesItsPlaceOn()
    {
        // A den spawns sleepers and each sleeper becomes a beast, so the beast is met where the den stands.
        var context = Context(
            new JsonObject(),
            Spawns(Den, "Fill", Sleeper),
            Spawns(Sleeper, "Wake", Beast)
        );
        var sites = new SpawnSites(
            context,
            [new("/Game/Invented/Sleeper:Wake", "abc"), new("/Game/Invented/Den:Fill", "abc")]
        );
        Assert.Equal(Level, Assert.Single(sites.Places(Beast)).Key);
        Assert.True(sites.IsRead(Beast));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(2, true)]
    public void SiteSpawnsOnlyWhereItsCountIsAboveZero(int most, bool spawns)
    {
        var context = Context(
            new JsonObject { ["Most Sleepers"] = most },
            Spawns(Den, "Fill", Sleeper)
        );
        var sites = new SpawnSites(
            context,
            [new("/Game/Invented/Den:Fill", "abc", "Most Sleepers")]
        );
        Assert.Equal(spawns, sites.Places(Sleeper).Count == 1);
    }

    [Fact]
    public void SiteThatChangedSinceItWasReadFailsTheCatalog()
    {
        var context = Context(new JsonObject(), Spawns(Den, "Fill", Sleeper));
        var error = Assert.Throws<InvalidDataException>(() =>
            new SpawnSites(context, [new("/Game/Invented/Den:Fill", "fff")])
        );
        Assert.Contains("/Game/Invented/Den:Fill", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SiteWhoseFunctionIsGoneFailsTheCatalogWhileItsClassRemains()
    {
        var context = Context(new JsonObject());
        Assert.Throws<InvalidDataException>(() =>
            new SpawnSites(context, [new("/Game/Invented/Den:Fill", "abc")])
        );
    }
}
