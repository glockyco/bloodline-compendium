using System.Text.Json.Nodes;
using Bloodline.Catalog;
using Bloodline.Code;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Tests.Catalog;

public sealed class SkillCatalogTests
{
    private const string Fire = "/Game/Invented/FireTree.FireTree_C";
    private const string Old = "/Game/Invented/OldTree.OldTree_C";
    private const string Menu = "/Game/Invented/MagicMenu.MagicMenu_C";
    private const string Categories = "/Game/Invented/Categories.Categories_C";
    private const string Slot = "/Game/Invented/SkillSlot.SkillSlot_C";
    private const string Passive = "/Game/Invented/PassiveSkillButton.PassiveSkillButton_C";
    private const string Book =
        "/Game/Invented/SkillBook_InteractionObject.SkillBook_InteractionObject_C";

    private static string Skill(string name) => $"/Game/Invented/{name}.{name}_C";

    private static JsonObject Json(string json) => JsonNode.Parse(json)!.AsObject();

    private static WidgetRecord Widget(
        string name,
        string type,
        string? parent,
        int position,
        string properties = "{}"
    ) =>
        new(
            "WidgetTree." + name,
            type,
            Json(properties),
            parent is null ? null : "WidgetTree." + parent,
            parent is null ? null : position
        );

    private static WidgetRecord Text(string name, string parent, int position, string text) =>
        Widget(
            name,
            "/Script/UMG.TextBlock",
            parent,
            position,
            $$$"""{"Text":{"$text":"{{{text}}}"}}"""
        );

    private static WidgetRecord SkillSlot(string name, string parent, string skill, int level) =>
        Widget(
            name,
            Slot,
            parent,
            0,
            $$"""{"Skill":{"$ref":"{{skill}}"},"Required Level":{{level}},"Level To Check":"Fire"}"""
        );

    private static CodeFunction Opens(string owner, string name, string target) =>
        new(
            owner.Split('.')[0],
            owner,
            name,
            true,
            "fingerprint",
            CodeParseStatus.Complete,
            [new CodeTarget(CodeTargetKind.Object, target)],
            [],
            [],
            []
        );

    /// <summary>
    /// A category button opens a menu, whose button opens the Fire tree. The Fire tree has a skill row of level 1 with
    /// two slots in boxes, placed in the opposite order of their names, and a proficiency row of level 5 whose second
    /// passive requires level 3. An older tree that nothing opens holds another skill.
    /// </summary>
    private static CatalogResult Build(params CodeFunction[] extraCode)
    {
        ClassRecord Widgets(
            string path,
            IReadOnlyList<WidgetRecord> widgets,
            params DelegateBinding[] bindings
        ) =>
            new(path, [], new JsonObject(), widgets)
            {
                Bindings = bindings.Length == 0 ? null : bindings,
            };
        var fire = Widgets(
            Fire,
            [
                Widget("Root", "/Script/UMG.VerticalBox", null, 0),
                Widget("Row1", "/Script/UMG.HorizontalBox", "Root", 0),
                Text("Label1", "Row1", 0, "Fire Level 1: "),
                Widget("Box1", "/Script/UMG.ScaleBox", "Row1", 2),
                Widget("Box2", "/Script/UMG.ScaleBox", "Row1", 1),
                SkillSlot("SlotA", "Box1", Skill("Ember"), 1),
                SkillSlot("SlotZ", "Box2", Skill("Spark"), 1),
                Widget("Row2", "/Script/UMG.HorizontalBox", "Root", 1),
                Text("Label2", "Row2", 0, "Proficiency Level 5:"),
                Widget(
                    "Burn",
                    Passive,
                    "Row2",
                    1,
                    """{"Passive Name":{"$text":"Burn"},"Required Level":5,"Point Cost":2,"Point Increase Per Level":2,"MultiLevel":true,"Max MultiLevel":5,"Level To Check":"Fire"}"""
                ),
                Widget(
                    "Early",
                    Passive,
                    "Row2",
                    2,
                    """{"Passive Name":{"$text":"Early"},"Required Level":3,"Point Cost":1,"Point Increase Per Level":1,"MultiLevel":false,"Max MultiLevel":5,"Level To Check":"Fire"}"""
                ),
                Widget(
                    "Kindle",
                    Passive,
                    "Row2",
                    3,
                    """{"Passive Name":{"$text":"Kindle"},"Required Level":5,"MultiLevel":true,"Level To Check":"Fire"}"""
                ),
            ]
        );
        var old = Widgets(
            Old,
            [
                Widget("Root", "/Script/UMG.VerticalBox", null, 0),
                Text("Label", "Root", 0, "Fire Level 1:"),
                SkillSlot("Slot", "Root", Skill("Forgotten"), 1),
            ]
        );
        var menu = Widgets(
            Menu,
            [
                Widget("FireButton", "/Script/UMG.Button", null, 0),
                Text("FireLabel", "FireButton", 0, "Fire"),
            ],
            new DelegateBinding("FireButton", "OnClicked", "OpenFire")
        );
        var categories = Widgets(
            Categories,
            [
                Widget(
                    "Magic_BTN",
                    "/Game/Invented/Button.Button_C",
                    null,
                    0,
                    """{"Text":{"$text":"Magic"}}"""
                ),
            ],
            new DelegateBinding("Magic_BTN", "OnClick", "OpenMagic")
        );
        var skills = new TableRecord(
            "/Game/Invented/SkillsDataTable.SkillsDataTable",
            null,
            [
                .. new[] { "Ember", "Spark", "Forgotten", "Lost", "Booked" }.Select(
                    name => new TableRow(
                        name,
                        Json(
                            $$"""{"Name":{"$text":"{{name}}"},"Skill":{"$ref":"{{Skill(name)}}"},"ManaCost":10,"Cooldown":5,"CastngTime":0.5}"""
                        )
                    )
                ),
            ]
        );
        var level = new LevelRecord(
            "/Game/Invented/Town",
            [],
            [
                new ActorRecord(
                    "Book_1",
                    Book,
                    Json($$$"""{"Skill to Learn":{"$ref":"{{{Skill("Booked")}}}"}}""")
                ),
            ]
        );
        var records = new CatalogRecords(
            new BuildRecord(1159290, 1, []),
            [
                fire,
                old,
                menu,
                categories,
                new ClassRecord(Slot, [], new JsonObject()),
                new ClassRecord(
                    Passive,
                    [],
                    Json(
                        """{"Point Cost":1,"Point Increase Per Level":1,"MultiLevel":false,"Max MultiLevel":5,"Required Level":0}"""
                    )
                ),
                new ClassRecord(Book, [], new JsonObject()),
                .. new[] { "Ember", "Spark", "Forgotten", "Lost", "Booked" }.Select(
                    name => new ClassRecord(Skill(name), [], Json($$"""{"SkillId":"{{name}}"}"""))
                ),
            ],
            [skills],
            [],
            [level],
            [],
            [],
            []
        )
        {
            Code = CodeIndex.Create([
                Opens(Categories, "OpenMagic", Menu),
                Opens(Menu, "OpenFire", Fire),
                .. extraCode,
            ]),
        };
        return CatalogBuilder.Build(records, [], null);
    }

    [Fact]
    public void PublishesTheTreesTheMenusOpenUnderTheirLabels()
    {
        var discipline = Assert.Single(Build().Skills.Disciplines);
        Assert.Equal(
            ("Fire", "Magic", "Fire"),
            (discipline.Name, discipline.Category, discipline.LevelName)
        );
        Assert.DoesNotContain(Build().Skills.Disciplines, page => page.Key == Old);
    }

    [Fact]
    public void LaysOutRowsInTheGameOrderWithEachEntryKeepingItsOwnLevel()
    {
        var result = Build();
        var discipline = Assert.Single(result.Skills.Disciplines);
        var row = Assert.Single(discipline.SkillRows);
        Assert.Equal(1, row.Level);
        Assert.Equal(["Spark", "Ember"], row.Slots.Select(slot => slot.Skill.Name));
        var passives = Assert.Single(discipline.PassiveRows).Passives;
        var burn = passives[0];
        Assert.Equal(
            (5, 2, 2, 5),
            (burn.RequiredLevel, burn.PointCost, burn.CostIncrease, burn.Ranks)
        );
        Assert.Equal((3, 1), (passives[1].RequiredLevel, passives[1].Ranks));
        // An entry that leaves a value at its class's default costs what the game reads from the class.
        Assert.Equal(
            (1, 1, 5),
            (passives[2].PointCost, passives[2].CostIncrease, passives[2].Ranks)
        );
        Assert.Contains(
            result.Report.Gaps.Single(gap => gap.Kind == GapKind.RowLevelDifference).Entries,
            entry => entry.Label == "Fire: Early"
        );
    }

    [Fact]
    public void GivesEverySkillItsSourcesOrAStatusThatSaysWhy()
    {
        var skills = Build().Skills.Skills.ToDictionary(skill => skill.Name);
        var tree = Assert.Single(skills["Ember"].Sources);
        Assert.Equal(SourceKind.SkillTree, tree.Kind);
        Assert.Equal(new PointCost(1, PointKind.SkillPoint), tree.Cost);
        Assert.Equal(1, Assert.Single(tree.Conditions).Value);
        Assert.Equal(SourceKind.SkillBook, Assert.Single(skills["Booked"].Sources).Kind);
        Assert.Equal(ItemStatus.NotInNormalPlay, skills["Forgotten"].Status);
        Assert.Equal(ItemStatus.NoKnownSource, skills["Lost"].Status);
        Assert.Equal(ItemStatus.Obtainable, skills["Ember"].Status);
    }

    [Fact]
    public void FailsOnAFunctionThatUnlocksASkillWithoutReview()
    {
        var unlock = new CodeFunction(
            "/Game/Invented/Shrine",
            "/Game/Invented/Shrine.Shrine_C",
            "Bless",
            false,
            "fingerprint",
            CodeParseStatus.Complete,
            [],
            [],
            ["virtual:AddNewSkill"],
            []
        );
        var error = Assert.Throws<InvalidDataException>(() => Build(unlock));
        Assert.Contains("/Game/Invented/Shrine:Bless", error.Message);
    }

    [Fact]
    public void ReportsATooltipThatShowsOtherValuesThanTheFacts()
    {
        var skill = Build().Skills.Skills.Single(skill => skill.Name == "Ember");
        var shown = SkillTooltipCheck
            .Expected(skill)
            .Select(text =>
                text.StartsWith("Cooldown", StringComparison.Ordinal) ? "Cooldown: 9 seconds" : text
            )
            .ToArray();
        var coverage = new Coverage();
        SkillTooltipCheck.Compare([skill], [new SkillTooltip(skill.Key, "run", shown)], coverage);
        var entry = Assert.Single(
            coverage.Report([]).Gaps.Single(gap => gap.Kind == GapKind.TooltipDifference).Entries
        );
        Assert.Contains("Cooldown: 5 seconds", entry.Detail);
        Assert.Contains("Cooldown: 9 seconds", entry.Detail);
        var agreeing = new Coverage();
        SkillTooltipCheck.Compare(
            [skill],
            [new SkillTooltip(skill.Key, "run", SkillTooltipCheck.Expected(skill))],
            agreeing
        );
        Assert.Empty(agreeing.Report([]).Gaps);
    }
}
