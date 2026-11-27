using System.Text.Json.Nodes;
using Bloodline.Catalog;
using Bloodline.Code;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Tests.Catalog;

public sealed class LivenessTests
{
    private const string Keeper = "/Game/Invented/Keeper.Keeper_C";
    private const string Beast = "/Game/Invented/Beast.Beast_C";
    private const string Level = "/Game/Invented/Meadow";

    private static CodeFunction Reads(string owner, string name, string read) =>
        new(
            owner.Split('.')[0],
            owner,
            name,
            false,
            "abc123",
            CodeParseStatus.Complete,
            [new CodeTarget(CodeTargetKind.Object, read)],
            [],
            [],
            []
        );

    private static CatalogContext Context(
        JsonObject keeper,
        IReadOnlyList<string>? keeperAncestors = null,
        params CodeFunction[] functions
    )
    {
        var records = new CatalogRecords(
            new BuildRecord(1159290, 1, []),
            [
                new ClassRecord(Keeper, keeperAncestors ?? [], keeper),
                new ClassRecord("/Game/Invented/Base.Base_C", [], new JsonObject()),
                new ClassRecord(Beast, [], new JsonObject()),
            ],
            [],
            [],
            [new LevelRecord(Level, [], [new ActorRecord("Keeper", Keeper, new JsonObject())])],
            [],
            [],
            []
        )
        {
            Code = CodeIndex.Create(functions),
        };
        return new CatalogContext(records, new Dictionary<string, ItemPage>(), []);
    }

    private static JsonObject Weighted(double weight) =>
        new()
        {
            ["Spawn Rates"] = new JsonObject
            {
                ["$map"] = new JsonArray(
                    new JsonArray(new JsonObject { ["$ref"] = Beast }, JsonValue.Create(weight))
                ),
            },
        };

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    public void TableEntryCountsOnlyWithWeightAboveZero(double weight, bool live)
    {
        Assert.Equal(live, new Liveness(Context(Weighted(weight)), []).IsLive(Beast));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void ScriptThatOnlyLooksForClassDoesNotBringItIn(bool lookUp, bool live)
    {
        var context = Context(new JsonObject(), null, Reads(Keeper, "Count Beasts", Beast));
        LookUp[] lookUps = lookUp ? [new("/Game/Invented/Keeper:Count Beasts", "abc")] : [];
        Assert.Equal(live, new Liveness(context, lookUps).IsLive(Beast));
    }

    [Fact]
    public void ObjectRunsScriptsOfClassItDerivesFromWithoutBringingThatClassIn()
    {
        const string parent = "/Game/Invented/Base.Base_C";
        var liveness = new Liveness(
            Context(new JsonObject(), [parent], Reads(parent, "Call Beast", Beast)),
            []
        );
        Assert.True(liveness.IsLive(Beast));
        Assert.False(liveness.IsLive(parent));
    }

    [Fact]
    public void LookUpThatChangedSinceItWasReadFailsTheCatalog()
    {
        var context = Context(new JsonObject(), null, Reads(Keeper, "Count Beasts", Beast));
        var error = Assert.Throws<InvalidDataException>(() =>
            new Liveness(context, [new("/Game/Invented/Keeper:Count Beasts", "fff")])
        );
        Assert.Contains(
            "/Game/Invented/Keeper:Count Beasts",
            error.Message,
            StringComparison.Ordinal
        );
    }
}
