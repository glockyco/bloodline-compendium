using System.Text.Json.Nodes;
using Bloodline.Catalog;
using Bloodline.Output;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Tests.Catalog;

public sealed class KnownIssueTests
{
    private static KnownIssue Issue(string id) =>
        new(
            id,
            "Invented issue",
            "Invented effect.",
            null,
            ["Invented step."],
            "Invented expectation.",
            "Invented observation.",
            "Invented cause.",
            ["/Game/Invented/Player:Equip"],
            [1],
            ReportStatus.NotReported,
            null
        );

    private static ClassRecord KneeGuards() =>
        new(
            "/Game/Invented/KneeGuards.KneeGuards_C",
            [ItemIdentity.BaseItem],
            JsonNode
                .Parse(
                    """
                    {"ItemInfo":{"ItemName":{"$text":"Knee guards"},"ItemType":{"$enum":"E_ItemType::Armor","display":"Armor"},
                     "ItemSlot":{"$enum":"Slot::Knees","display":"Knees"},"ItemValue":1}}
                    """
                )!
                .AsObject()
        );

    private static CatalogRecords Records(params KnownIssue[] issues) =>
        new(new BuildRecord(1159290, 1, []), [KneeGuards()], [], [], [], [], [], [])
        {
            Issues = new KnownIssueRegistry(issues),
        };

    [Fact]
    public void ANoteNamingAMissingIssueFailsTheCatalog()
    {
        var error = Assert.Throws<InvalidDataException>(() => CatalogBuilder.Build(Records(), []));
        Assert.Contains(KnownIssues.KneeArmor, error.Message);
        Assert.Contains("Knee guards", error.Message);
    }

    [Fact]
    public void AffectedPagesCarryTheNoteAndThePublishedIssueListsThem()
    {
        var result = CatalogBuilder.Build(Records(Issue(KnownIssues.KneeArmor)), []);

        var note = Assert.Single(Assert.Single(result.Items).Notes);
        Assert.Equal((KnownIssues.KneeArmor, "Invented issue"), (note.Issue, note.Title));
        Assert.Equal("Knee guards", Assert.Single(Assert.Single(result.Issues).Affected).Name);
    }

    [Fact]
    public void TooltipCausesNoteOnlyValuesThatThePageShows()
    {
        var page = Assert.Single(
            CatalogBuilder.Build(Records(Issue(KnownIssues.KneeArmor)), []).Items
        );
        TooltipDifference Difference(string field, string? catalog, string reason) =>
            new(page.Key, page.Name, field, "4", catalog, reason, []);
        var report = new TooltipReport(
            1,
            TooltipCheck.Consumer,
            [],
            1,
            TooltipCheck.Reasons,
            [],
            [
                Difference("Armor rating", "0", "armor-without-row"),
                Difference("Stat line", null, "empty-stat-line"),
                Difference("Description", "Text", "crafted-armor-tooltip"),
            ]
        );
        var registry = new KnownIssueRegistry([
            Issue("armor-without-row"),
            Issue("empty-stat-line"),
            Issue(KnownIssues.KneeArmor),
        ]);

        var (pages, issues) = KnownIssues.Attach([page], registry, report, new HashSet<string>());

        Assert.Equal(
            ["armor-without-row", KnownIssues.KneeArmor],
            Assert.Single(pages).Notes.Select(note => note.Issue)
        );
        Assert.Empty(issues.Single(issue => issue.Issue.Id == "empty-stat-line").Affected);
        var missing = Assert.Throws<InvalidDataException>(() =>
            KnownIssues.Attach(
                [page],
                new KnownIssueRegistry([Issue("armor-without-row"), Issue(KnownIssues.KneeArmor)]),
                report,
                new HashSet<string>()
            )
        );
        Assert.Contains("empty-stat-line", missing.Message);
    }

    [Theory]
    [InlineData("same-id", "same-id", false, null)]
    [InlineData("Bad_Id", "other-id", false, null)]
    [InlineData("one-id", "other-id", true, null)]
    [InlineData("one-id", "other-id", false, "https://example.com")]
    public void TheRegistryRejectsAmbiguousOrIncompleteEntries(
        string first,
        string second,
        bool reported,
        string? url
    )
    {
        var file = Path.Combine(
            Path.GetTempPath(),
            "bloodline-issues-" + Guid.NewGuid().ToString("N") + ".json"
        );
        try
        {
            CanonicalJson.Write(
                file,
                new KnownIssueRegistry([
                    Issue(first) with
                    {
                        Report = reported ? ReportStatus.Reported : ReportStatus.NotReported,
                        ReportUrl = url,
                    },
                    Issue(second),
                ])
            );
            Assert.Throws<InvalidDataException>(() => KnownIssues.Load(file));
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void TheRepositoryRegistryLoads()
    {
        var registry = KnownIssues.Load(
            Path.Combine(AppContext.BaseDirectory, "known-issues.json")
        );
        Assert.NotEmpty(registry.Issues);
    }
}
