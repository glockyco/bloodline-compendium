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
        Assert.Equal((KnownIssues.KneeArmor, "Invented effect."), (note.Issue, note.Effect));
        Assert.Equal("Knee guards", Assert.Single(Assert.Single(result.Issues).Affected).Name);
    }

    [Fact]
    public void ItemNotesOverrideOnlyTheMatchingItemsEffect()
    {
        var issue = Issue(KnownIssues.KneeArmor) with
        {
            ItemNotes = new Dictionary<string, string>
            {
                [KneeGuards().Path] = "Invented item-specific effect.",
            },
        };
        var result = CatalogBuilder.Build(Records(issue), []);

        Assert.Equal(
            "Invented item-specific effect.",
            Assert.Single(Assert.Single(result.Items).Notes).Effect
        );
        Assert.Equal("Invented effect.", Assert.Single(result.Issues).Issue.Effect);
    }

    [Theory]
    [InlineData("/Game/Invented/Missing.Missing_C")]
    [InlineData("/Game/Invented/KneeGuards.KneeGuards_C")]
    public void AnItemNoteForAnItemThatDoesNotNoteTheIssueFailsTheCatalog(string key)
    {
        var issue = Issue("unrelated-issue") with
        {
            ItemNotes = new Dictionary<string, string> { [key] = "Invented item-specific effect." },
        };

        var error = Assert.Throws<InvalidDataException>(() =>
            CatalogBuilder.Build(Records(Issue(KnownIssues.KneeArmor), issue), [])
        );

        Assert.Contains(issue.Id, error.Message);
        Assert.Contains(key, error.Message);
        Assert.Contains("does not note this issue", error.Message);
    }

    [Fact]
    public void StaffIssuesNoteOnlyTheReproducedItemKeys()
    {
        var template = Assert.Single(
            CatalogBuilder.Build(Records(Issue(KnownIssues.KneeArmor)), []).Items
        );
        (string Key, string? Issue)[] cases =
        [
            (
                "/Game/BloodlinesAlpha/Interactables/Weapons/Halberds/FairyStaff.FairyStaff_C",
                KnownIssues.FairyStaffNoFireballs
            ),
            (
                "/Game/BloodlinesAlpha/Interactables/Weapons/Staves/AcolyteGeomancerStaff.AcolyteGeomancerStaff_C",
                KnownIssues.StaffPointBlankDamage
            ),
            (
                "/Game/BloodlinesAlpha/Interactables/Weapons/Halberds/WalkingStick.WalkingStick_C",
                KnownIssues.StaffPointBlankDamage
            ),
            (
                "/Game/BloodlinesAlpha/Interactables/Weapons/Staves/MoonstoneStaff.MoonstoneStaff_C",
                KnownIssues.StaffPointBlankDamage
            ),
            (
                "/Game/BloodlinesAlpha/Interactables/Weapons/Staves/AcolyteCryomancerStaff.AcolyteCryomancerStaff_C",
                null
            ),
            (
                "/Game/BloodlinesAlpha/Interactables/Weapons/Staves/AdeptCryomancerStaff.AdeptCryomancerStaff_C",
                null
            ),
            ("/Game/Invented/FairyStaff.FairyStaff_C", null),
            (
                "/Game/BloodlinesAlpha/Interactables/Weapons/Staves/WalkingStick.WalkingStick_C",
                null
            ),
        ];
        var items = cases
            .Select(test => template with { Key = test.Key, Name = test.Key, Type = "Staff" })
            .ToArray();
        var registry = new KnownIssueRegistry([
            Issue(KnownIssues.FairyStaffNoFireballs),
            Issue(KnownIssues.StaffPointBlankDamage),
        ]);

        var (pages, issues) = KnownIssues.Attach(items, registry, null, new HashSet<string>());

        foreach (var test in cases)
        {
            var notes = pages.Single(page => page.Key == test.Key).Notes;
            if (test.Issue is null)
            {
                Assert.Empty(notes);
            }
            else
            {
                Assert.Equal(test.Issue, Assert.Single(notes).Issue);
            }
        }
        foreach (var issue in issues)
        {
            Assert.Equal(
                cases.Where(test => test.Issue == issue.Issue.Id).Select(test => test.Key).Order(),
                issue.Affected.Select(item => item.Key).Order()
            );
        }
    }

    [Theory]
    [InlineData(
        "/Game/BloodlinesAlpha/Interactables/Weapons/Halberds/FairyStaff.FairyStaff_C",
        KnownIssues.FairyStaffNoFireballs
    )]
    [InlineData(
        "/Game/BloodlinesAlpha/Interactables/Weapons/Staves/AcolyteGeomancerStaff.AcolyteGeomancerStaff_C",
        KnownIssues.StaffPointBlankDamage
    )]
    [InlineData(
        "/Game/BloodlinesAlpha/Interactables/Weapons/Halberds/WalkingStick.WalkingStick_C",
        KnownIssues.StaffPointBlankDamage
    )]
    [InlineData(
        "/Game/BloodlinesAlpha/Interactables/Weapons/Staves/MoonstoneStaff.MoonstoneStaff_C",
        KnownIssues.StaffPointBlankDamage
    )]
    public void AStaffNoteNamingAMissingIssueFailsTheCatalog(string key, string issue)
    {
        var template = Assert.Single(
            CatalogBuilder.Build(Records(Issue(KnownIssues.KneeArmor)), []).Items
        );
        var page = template with { Key = key, Name = "Invented staff", Type = "Staff" };

        var error = Assert.Throws<InvalidDataException>(() =>
            KnownIssues.Attach([page], new KnownIssueRegistry([]), null, new HashSet<string>())
        );

        Assert.Contains(issue, error.Message);
        Assert.Contains(page.Name, error.Message);
    }

    [Fact]
    public void TooltipCausesNoteOnlyValuesThatThePageShows()
    {
        var page = Assert.Single(
            CatalogBuilder.Build(Records(Issue(KnownIssues.KneeArmor)), []).Items
        );
        page = page with
        {
            Facts = page
                .Facts.Select(fact =>
                    fact.Stat == Stat.ArmorRating
                        ? fact with
                        {
                            GameShows = "4",
                            GameShowsIssue = "armor-without-row",
                        }
                        : fact
                )
                .ToArray(),
        };
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
                Difference("Armor type", null, "armor-slot-adds-none"),
                Difference("Description", "Text", "crafted-armor-tooltip"),
            ]
        );
        var registry = new KnownIssueRegistry([
            Issue("armor-without-row"),
            Issue("armor-slot-adds-none"),
            Issue(KnownIssues.KneeArmor),
        ]);

        var (pages, issues) = KnownIssues.Attach([page], registry, report, new HashSet<string>());

        Assert.Equal(
            ["armor-without-row", KnownIssues.KneeArmor],
            Assert.Single(pages).Notes.Select(note => note.Issue)
        );
        Assert.Equal(
            "Wearing Knee guards adds no armor.",
            Assert.Single(pages).Notes.Single(note => note.Issue == "armor-without-row").Effect
        );
        Assert.Empty(issues.Single(issue => issue.Issue.Id == "armor-slot-adds-none").Affected);
        var missing = Assert.Throws<InvalidDataException>(() =>
            KnownIssues.Attach(
                [page],
                new KnownIssueRegistry([Issue("armor-without-row"), Issue(KnownIssues.KneeArmor)]),
                report,
                new HashSet<string>()
            )
        );
        Assert.Contains("armor-slot-adds-none", missing.Message);
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
