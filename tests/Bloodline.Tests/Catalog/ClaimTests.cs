using System.CommandLine;
using System.Text.Json.Nodes;
using Bloodline.Catalog;
using Bloodline.Code;
using Bloodline.Output;
using Bloodline.Records;

namespace Bloodline.Tests.Catalog;

public sealed class ClaimTests
{
    private static CodeFunction Function(
        string name,
        string fingerprint,
        CodeParseStatus status = CodeParseStatus.Complete
    ) =>
        new(
            "/Game/BloodlinesAlpha/Blueprints/FirstPerson2/FirstPersonCharacter2",
            "/Game/Invented/Player.Player_C",
            name,
            false,
            fingerprint,
            status,
            [],
            [],
            [],
            []
        );

    [Fact]
    public void MissingCitationNamesTheFactAndFunction()
    {
        var error = Assert.Throws<InvalidDataException>(() =>
            ClaimCitations.Resolve(
                "Invented sword:Damage",
                ["/Game/Invented/Player:Missing"],
                CodeIndex.Create([])
            )
        );
        Assert.Contains("Invented sword:Damage", error.Message);
        Assert.Contains("/Game/Invented/Player:Missing", error.Message);
    }

    [Fact]
    public void PartlyParsedCitationNamesTheFactAndFunction()
    {
        var function = Function("Equip", "invented", CodeParseStatus.Partial);
        var error = Assert.Throws<InvalidDataException>(() =>
            ClaimCitations.Resolve(
                "Invented armor:Armor",
                [function.Citation],
                CodeIndex.Create([function])
            )
        );
        Assert.Contains("Invented armor:Armor", error.Message);
        Assert.Contains(function.Citation, error.Message);
    }

    [Fact]
    public void CatalogRejectsAFieldConsumerThatIsPartlyParsed()
    {
        var function = Function("Strike", "invented", CodeParseStatus.Partial) with
        {
            Reads =
            [
                new CodeTarget(
                    CodeTargetKind.StructField,
                    "/Game/Invented/F_WeaponINfo.F_WeaponInfo:Damage"
                ),
            ],
        };
        var resolver = new ItemFacts([], code: CodeIndex.Create([function]), build: 42);
        var error = Assert.Throws<InvalidDataException>(() => resolver.Resolve(Item(), "invented"));
        Assert.Contains("Damage", error.Message);
        Assert.Contains(function.Citation, error.Message);
    }

    [Fact]
    public void CatalogRejectsAMissingFunctionInTheConsumerIndex()
    {
        const string citation =
            "/Game/BloodlinesAlpha/Blueprints/FirstPerson2/FirstPersonCharacter2:InventedMissingFunction";
        var index = new CodeIndex(
            [],
            [
                new CodeReference(
                    new CodeTarget(
                        CodeTargetKind.StructField,
                        "/Game/Invented/F_WeaponINfo.F_WeaponInfo:Damage"
                    ),
                    [citation],
                    []
                ),
            ],
            []
        );
        var resolver = new ItemFacts([], code: index, build: 42);
        var error = Assert.Throws<InvalidDataException>(() => resolver.Resolve(Item(), "invented"));
        Assert.Contains("Damage", error.Message);
        Assert.Contains(citation, error.Message);
    }

    [Fact]
    public void EveryPublishedItemStatGetsItsTypeBuildAndReadingFunction()
    {
        var function = Function("Strike", "invented-fingerprint") with
        {
            Reads =
            [
                new CodeTarget(
                    CodeTargetKind.StructField,
                    "/Game/Invented/F_WeaponINfo.F_WeaponInfo:Damage"
                ),
            ],
        };
        var resolver = new ItemFacts([], code: CodeIndex.Create([function]), build: 42);
        var page = resolver.Resolve(Item(), "invented");
        Assert.All(
            page.Facts,
            fact => Assert.Contains(resolver.Claims, claim => claim.Fact == fact.Label)
        );
        var damage = Assert.Single(resolver.Claims, claim => claim.Fact == "Damage");
        Assert.Equal(ClaimType.Extracted, damage.Type);
        Assert.Equal(42, damage.Build);
        Assert.Equal(
            new CodeCitation(function.Citation, function.Fingerprint),
            Assert.Single(damage.Citations)
        );
        Assert.DoesNotContain(resolver.Claims, claim => claim.Fact == "Attack speed");
    }

    private static ClassRecord Item() =>
        new(
            "/Game/Invented/Sword.Sword_C",
            [],
            JsonNode
                .Parse(
                    """
                    {"ItemInfo":{"ItemName":{"$text":"Invented sword"},"ItemType":{"$enum":"Item::Weapon","display":"Weapon"},"ItemValue":2},
                    "WeaponInfo":{"Damage":4}}
                    """
                )!
                .AsObject()
        );

    [Fact]
    public async Task TwoFixtureBuildsListOnlyChangedCitationsWithEveryCitingFact()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "bloodline-compare-" + Guid.NewGuid().ToString("N")
        );
        try
        {
            var before = new RecordLayout(directory, 10);
            var after = new RecordLayout(directory, 20);
            var changed = Function("Strike", "before");
            var unchanged = Function("Hover", "stable");
            var report = new ClaimReport(
                10,
                [
                    new FactClaim(
                        "invented-sword",
                        "Damage",
                        ClaimType.Extracted,
                        10,
                        [],
                        [new CodeCitation(changed.Citation, changed.Fingerprint)]
                    ),
                    new FactClaim(
                        "invented-bow",
                        "Damage",
                        ClaimType.Extracted,
                        10,
                        [],
                        [new CodeCitation(changed.Citation, changed.Fingerprint)]
                    ),
                    new FactClaim(
                        "invented-sword",
                        "Value",
                        ClaimType.Extracted,
                        10,
                        [],
                        [new CodeCitation(unchanged.Citation, unchanged.Fingerprint)]
                    ),
                ]
            );
            CanonicalJson.Write(before.BuildFile, new BuildRecord(1, 10, []));
            CanonicalJson.Write(before.CodeIndexFile, CodeIndex.Create([changed, unchanged]));
            CanonicalJson.Write(before.ClaimsFile, report);
            CanonicalJson.Write(after.BuildFile, new BuildRecord(1, 20, []));
            CanonicalJson.Write(
                after.CodeIndexFile,
                CodeIndex.Create([changed with { Fingerprint = "after" }, unchanged])
            );
            CanonicalJson.Write(after.ClaimsFile, report with { Build = 20 });
            var output = Path.Combine(directory, "comparison.json");
            Assert.Equal(
                0,
                await CompareCommand
                    .CreateCommand()
                    .Parse([before.BuildDirectory, after.BuildDirectory, "--output", output])
                    .InvokeAsync(cancellationToken: TestContext.Current.CancellationToken)
            );
            var result = CanonicalJson.Read<BuildComparison>(output);
            Assert.Equal(10, result.PreviousBuild);
            Assert.Equal(20, result.CurrentBuild);
            var citation = Assert.Single(result.ChangedCitations);
            Assert.Equal(changed.Citation, citation.Function);
            Assert.Equal("before", citation.PreviousFingerprint);
            Assert.Equal("after", citation.CurrentFingerprint);
            Assert.Equal(
                [
                    new CitedFact("invented-bow", "Damage"),
                    new CitedFact("invented-sword", "Damage"),
                ],
                citation.Facts
            );
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void RemovedFunctionsAlsoListTheFactsThatNeedReview()
    {
        var previous = new ClaimReport(
            1,
            [
                new FactClaim(
                    "invented",
                    "Damage",
                    ClaimType.Extracted,
                    1,
                    [],
                    [new CodeCitation("/Game/Invented/Player:Strike", "before")]
                ),
            ]
        );
        var change = Assert.Single(
            CompareCommand.Compare(previous, 2, CodeIndex.Create([])).ChangedCitations
        );
        Assert.Null(change.CurrentFingerprint);
        Assert.Equal("Damage", Assert.Single(change.Facts).Fact);
    }
}
