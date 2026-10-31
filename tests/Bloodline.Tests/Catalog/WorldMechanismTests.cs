using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Bloodline.Catalog;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Tests.Catalog;

public sealed class WorldMechanismTests
{
    private static JsonObject Parse(string value) => JsonNode.Parse(value)!.AsObject();

    private static double[] NoteNumbers(Source source) =>
        Regex
            .Matches(string.Join(" ", source.Notes), @"-?\d+(?:\.\d+)?")
            .Select(match => double.Parse(match.Value, CultureInfo.InvariantCulture))
            .ToArray();

    private static ClassRecord Type(
        string name,
        string properties = "{}",
        params string[] ancestors
    ) => new($"/Invented/{name}.{name}_C", ancestors, Parse(properties));

    private static CatalogContext Context(
        ClassRecord[]? classes = null,
        TableRecord[]? tables = null,
        LevelRecord[]? levels = null,
        AssetRecord[]? dialogues = null,
        EnumRecord[]? enums = null
    ) =>
        new(
            new CatalogRecords(
                new BuildRecord(1, 1, []),
                classes ?? [],
                tables ?? [],
                enums ?? [],
                levels ?? [],
                dialogues ?? [],
                [],
                []
            ),
            new Dictionary<string, ItemPage>(),
            []
        );

    private static TableRecord Table(string name, string values) =>
        new($"/Invented/{name}.{name}", null, [new TableRow("InventedRow", Parse(values))]);

    private static LevelRecord Level(ClassRecord type, string properties = "{}") =>
        new(
            "/Invented/Coast",
            [],
            [new ActorRecord("InventedActor", type.Path, Parse(properties))]
        );

    [Fact]
    public void FarmingKeepsSeedsSeparateAndCountsNullOutcomesButNotZeroOutputs()
    {
        var table = Table(
            "SeedDataTable",
            """
            {"Item":{"$ref":"/Invented/Seed.Seed_C"},"HarvestItems":{"$map":[[{"$ref":"/Invented/Plant.Plant_C"},2],[null,3],[{"$ref":"/Invented/Dud.Dud_C"},0]]},"Yield":4,"GrowDurationDays":2,"FarmingLevel":7}
            """
        );
        var result = Assert.Single(new FarmingMechanism().FindSources(Context(tables: [table])));
        Assert.Equal("/Invented/Plant.Plant_C", result.ItemClass);
        Assert.Equal(new Odds(OddsEvent.Pick, 0.4, 0.4, null), result.Source.Odds);
        Assert.Null(result.Source.Amount);
        Assert.Equal("Seed", Assert.Single(result.Source.Inputs).Item.Name);
        Assert.Empty(result.Source.Places);
        Assert.Equal(new[] { 4d, 2d }, NoteNumbers(result.Source));
        Assert.Equal(EntityKind.Resource, result.Source.Entity.Kind);
        Assert.Equal(table.Path + ":InventedRow", result.Source.Entity.Key);
        var condition = Assert.Single(result.Source.Conditions);
        Assert.Equal(ConditionKind.ProficiencyLevel, condition.Kind);
        Assert.Equal(7, condition.Value);
    }

    [Fact]
    public void BountyAcceptanceGivesOnlyTheNoteAndUsesItsInventoryQuantity()
    {
        var note = Type("Note", """{"ItemInfo":{"Quantity":2}}""");
        var table = Table(
            "DT_UQC_Quests",
            """
            {"Type":{"$enum":"QuestType::NewEnumerator99"},"ItemBountyNoteClass":{"$ref":"/Invented/Note.Note_C"},"RequiredItemItemBounties":{"$ref":"/Invented/Requirement.Requirement_C"},"DisplayName":{"$text":"Invented bounty"}}
            """
        );
        var enumType = new EnumRecord(
            "/Invented/QuestType.QuestType",
            [new EnumEntry("NewEnumerator99", 5, "Contract")]
        );
        var result = Assert.Single(
            new BountyNoteMechanism().FindSources(Context([note], [table], enums: [enumType]))
        );
        Assert.Equal(note.Path, result.ItemClass);
        Assert.Equal(new Amount(2, 2), result.Source.Amount);
        Assert.Empty(result.Source.Inputs);
        Assert.Equal("Invented bounty", result.Source.Name);
        Assert.Equal(EntityKind.Quest, result.Source.Entity.Kind);
        Assert.Equal(table.Path + ":InventedRow", result.Source.Entity.Key);
    }

    [Fact]
    public void SettlementDeliveryKeepsAttemptCountSeparateFromPerItemAmount()
    {
        var table = Table(
            "BuildingResourceAndIncomeValues",
            """
            {"BuildingName":"Invented brewery","LootDeliveryQuantity":10,"WeeklyIncome":900,"LootTable":{"$map":[[{"$ref":"/Invented/Drink.Drink_C"},2],[null,1],[{"$ref":"/Invented/Dud.Dud_C"},0]]}}
            """
        );
        var result = Assert.Single(
            new SettlementDeliveryMechanism().FindSources(Context(tables: [table]))
        );
        // Ten picks that each choose the drink 2 times in 3.
        Assert.Equal(1 - Math.Pow(1.0 / 3, 10), result.Source.Odds!.Chance!.Value, 12);
        Assert.Equal(20.0 / 3, result.Source.Odds.Expected!.Value, 12);
        Assert.Equal(OddsEvent.Delivery, result.Source.Odds.Event);
        Assert.Equal("Invented brewery", result.Source.Name);
        Assert.Null(result.Source.Amount);
        Assert.Empty(result.Source.Places);
        Assert.True(result.Source.Settlement);
        Assert.Equal(EntityKind.Building, result.Source.Entity.Kind);
        Assert.Equal(result.Source.Entity, Assert.Single(result.Source.Conditions).Entity);
        table.Rows[0].Values["LootDeliveryQuantity"] = 0;
        Assert.Empty(new SettlementDeliveryMechanism().FindSources(Context(tables: [table])));
    }

    [Fact]
    public void DialogueMergesEventDefaultsAndPlacedNpcDialogueOverrides()
    {
        var grant = Type(
            "GrantEvent",
            """{"Amount to Give":3}""",
            "/Invented/AddItemDialogue.AddItemDialogue_C"
        );
        var npc = Type(
            "Storyteller",
            """{"Full Name":{"$text":"Invented storyteller"},"Dialogue":{"$ref":"/Invented/Tale.Tale"}}"""
        );
        var dialogue = new AssetRecord(
            "/Invented/Tale.Tale",
            "Dialogue",
            new JsonObject(),
            [
                new InnerObjectRecord(
                    "/Invented/Tale.Tale:Grant",
                    grant.Path,
                    Parse("""{"Item To Add":{"$ref":"/Invented/Gift.Gift_C"}}""")
                ),
                new InnerObjectRecord(
                    "/Invented/Tale.Tale:Remove",
                    "/Invented/GiveItems.GiveItems_C",
                    Parse("""{"Item To Add":{"$ref":"/Invented/Removed.Removed_C"}}""")
                ),
            ]
        );
        var result = Assert.Single(
            new DialogueGrantMechanism().FindSources(
                Context([grant, npc], levels: [Level(npc)], dialogues: [dialogue])
            )
        );
        Assert.Equal("/Invented/Gift.Gift_C", result.ItemClass);
        Assert.Equal(new Amount(3, 3), result.Source.Amount);
        Assert.Equal("Invented storyteller", result.Source.Name);
        Assert.Equal("/Invented/Coast", Assert.Single(result.Source.Places).Key);
        Assert.False(result.Source.Unconfirmed);
        var unplaced = Assert.Single(
            new DialogueGrantMechanism().FindSources(Context([grant], dialogues: [dialogue]))
        );
        Assert.Empty(unplaced.Source.Places);
        Assert.False(unplaced.Source.Unconfirmed);
        Assert.Equal(new Amount(3, 3), unplaced.Source.Amount);
        Assert.Equal(EntityKind.Person, result.Source.Entity.Kind);
        Assert.Equal(npc.Path, result.Source.Entity.Key);
        Assert.Equal(dialogue.Path, unplaced.Source.Entity.Key);
    }

    [Theory]
    [InlineData("/Invented/Dialogue_Unused/Tale.Tale", true)]
    [InlineData("/Invented/Tale.Tale", false)]
    public void DialogueMarkedUnusedRetainsItsRewardPlaceAndExplanation(
        string path,
        bool unconfirmed
    )
    {
        var grant = Type("AddItemDialogue");
        var npc = Type("Storyteller");
        npc.Properties["Dialogue"] = new JsonObject { ["$ref"] = path };
        var dialogue = new AssetRecord(
            path,
            "Dialogue",
            new JsonObject(),
            [
                new InnerObjectRecord(
                    path + ":Grant",
                    grant.Path,
                    Parse("""{"Item To Add":{"$ref":"/Invented/Gift.Gift_C"}}""")
                ),
            ]
        );
        var source = Assert.Single(
            new DialogueGrantMechanism().FindSources(
                Context([grant, npc], levels: [Level(npc)], dialogues: [dialogue])
            )
        ).Source;
        Assert.Equal(unconfirmed, source.Unconfirmed);
        Assert.Equal("/Invented/Coast", Assert.Single(source.Places).Key);
        Assert.Equal(
            unconfirmed,
            source.Notes.Contains(
                "The game files mark this conversation unused, so it may never take place."
            )
        );
    }

    [Fact]
    public void DialoguePublishesKnownAndUnknownConditionsWithoutConditionNotes()
    {
        var grant = Type("AddItemDialogue");
        var questTable = Table("DT_UQC_Quests", """{"DisplayName":{"$text":"Invented quest"}}""");
        var dialogue = new AssetRecord(
            "/Invented/Tale.Tale",
            "Dialogue",
            Parse(
                """
                {"Data":[{"Events":[{"$ref":"Gift"}],"Conditions":[{"$ref":"QuestCheck"},{"$ref":"Unreadable"},{"$ref":"Missing"}]}]}
                """
            ),
            [
                new InnerObjectRecord(
                    "Gift",
                    grant.Path,
                    Parse("""{"Item To Add":{"$ref":"/Invented/Gift.Gift_C"}}""")
                ),
                new InnerObjectRecord(
                    "QuestCheck",
                    "/Invented/Quest_is_Active.Quest_is_Active_C",
                    Parse("""{"Quest Needed":"InventedRow"}""")
                ),
                new InnerObjectRecord(
                    "Unreadable",
                    "/Invented/CustomCheck.CustomCheck_C",
                    new JsonObject()
                ),
            ]
        );
        var found = Assert.Single(
            new DialogueGrantMechanism().FindSources(
                Context([grant], [questTable], dialogues: [dialogue])
            )
        );
        var quest = Assert.Single(found.Source.Conditions);
        Assert.Equal(ConditionKind.Quest, quest.Kind);
        Assert.Equal(questTable.Path + ":InventedRow", quest.Entity!.Key);
        // The unreadable and the missing check reach the maintainer report, not the page.
        Assert.Equal(2, found.Unresolved.Count);
        Assert.DoesNotContain(
            found.Source.Notes,
            note => note.Contains("Requires", StringComparison.Ordinal)
        );
    }

    [Fact]
    public void DialogueAncestorChecksRemainVisibleAsUnknownPathRequirements()
    {
        var grant = Type("AddItemDialogue");
        var dialogue = new AssetRecord(
            "/Invented/Tale.Tale",
            "Dialogue",
            Parse(
                """
                {"Data":[
                    {"id":0,"Links":[1],"Conditions":[{"$ref":"EarlierCheck"}]},
                    {"id":1,"Links":[2]},
                    {"id":2,"Events":[{"$ref":"Gift"}]}
                ]}
                """
            ),
            [
                new InnerObjectRecord(
                    "Gift",
                    grant.Path,
                    Parse("""{"Item To Add":{"$ref":"/Invented/Gift.Gift_C"}}""")
                ),
                new InnerObjectRecord(
                    "EarlierCheck",
                    "/Invented/CustomCheck.CustomCheck_C",
                    new JsonObject()
                ),
            ]
        );
        var source = Assert.Single(
            new DialogueGrantMechanism().FindSources(Context([grant], dialogues: [dialogue]))
        );
        // An unread earlier check tells a player nothing to do, so only the maintainer report lists it.
        Assert.Empty(source.Source.Conditions);
        Assert.Contains(
            source.Unresolved,
            label => label.Contains("Earlier lines", StringComparison.Ordinal)
        );
    }

    [Theory]
    [InlineData("PlayerHasEnoughMoney", "Required Money")]
    [InlineData("EqualOrGreaterFishingLevel", "Required Fishing Level")]
    [InlineData("ChapterRankCheck", "Required Rank")]
    public void DialogueUnreadableThresholdIsAnUnknownCondition(string check, string field)
    {
        var grant = new InnerObjectRecord(
            "Gift",
            "/Invented/AddItemDialogue.AddItemDialogue_C",
            new JsonObject()
        );
        var dialogue = new AssetRecord(
            "/Invented/Tale.Tale",
            "Dialogue",
            Parse("""{"Data":[{"Events":[{"$ref":"Gift"}],"Conditions":[{"$ref":"Check"}]}]}"""),
            [
                grant,
                new InnerObjectRecord(
                    "Check",
                    $"/Invented/{check}.{check}_C",
                    new JsonObject { [field] = new JsonObject { ["$unsupported"] = "value" } }
                ),
            ]
        );
        var condition = Assert.Single(
            WorldSourceData.DialogueConditions(Context(), dialogue, grant).Shown
        );
        Assert.Equal(ConditionKind.Unknown, condition.Kind);
        Assert.Null(condition.Value);
    }

    [Theory]
    [InlineData(true, false, "Requires talking to Scarecrow first.")]
    [InlineData(false, false, "Only until you talk to Scarecrow.")]
    [InlineData(false, true, "Given only once.")]
    public void DialogueTagChecksNameTheConversationThatSetsTheTag(bool set, bool own, string label)
    {
        var grant = new InnerObjectRecord(
            "Gift",
            "/Invented/AddItemDialogue.AddItemDialogue_C",
            new JsonObject()
        );
        var check = set ? "CheckIfDialogueTagIsValid" : "CheckIfDialogueTagIsNOTValid";
        var setter = new InnerObjectRecord(
            "Setter",
            "/Invented/AddSpecialTagDialogue.AddSpecialTagDialogue_C",
            Parse("""{"Special Tag":"SpokeWithScarecrow"}""")
        );
        var dialogue = new AssetRecord(
            "/Invented/Tale.Tale",
            "Dialogue",
            Parse("""{"Data":[{"Events":[{"$ref":"Gift"}],"Conditions":[{"$ref":"Check"}]}]}"""),
            [
                grant,
                new InnerObjectRecord(
                    "Check",
                    $"/Invented/{check}.{check}_C",
                    Parse("""{"Tag to Find":"SpokeWithScarecrow"}""")
                ),
                .. own ? [setter] : Array.Empty<InnerObjectRecord>(),
            ]
        );
        var scarecrow = new AssetRecord(
            "/Invented/Scarecrow.Scarecrow",
            "Dialogue",
            new JsonObject(),
            [setter]
        );
        var speaker = Type(
            "ScarecrowNpc",
            """{"Full Name":{"$text":"Scarecrow"},"Dialogue":{"$ref":"/Invented/Scarecrow.Scarecrow"}}"""
        );
        var context = Context([speaker], dialogues: own ? [dialogue] : [dialogue, scarecrow]);

        var requirements = WorldSourceData.DialogueConditions(context, dialogue, grant);

        Assert.Equal(label, Assert.Single(requirements.Shown).Label);
        Assert.Empty(requirements.Unresolved);
    }

    [Fact]
    public void ATagTheConversationSetsOnTheWayIsNoRequirement()
    {
        var grant = new InnerObjectRecord(
            "Gift",
            "/Invented/AddItemDialogue.AddItemDialogue_C",
            new JsonObject()
        );
        var dialogue = new AssetRecord(
            "/Invented/Tale.Tale",
            "Dialogue",
            Parse("""{"Data":[{"Events":[{"$ref":"Gift"}],"Conditions":[{"$ref":"Check"}]}]}"""),
            [
                grant,
                new InnerObjectRecord(
                    "Check",
                    "/Invented/CheckIfDialogueTagIsValid.CheckIfDialogueTagIsValid_C",
                    Parse("""{"Tag to Find":"AskedForWares"}""")
                ),
                new InnerObjectRecord(
                    "Setter",
                    "/Invented/AddSpecialTagDialogue.AddSpecialTagDialogue_C",
                    Parse("""{"Special Tag":"AskedForWares"}""")
                ),
            ]
        );

        var requirements = WorldSourceData.DialogueConditions(
            Context(dialogues: [dialogue]),
            dialogue,
            grant
        );

        Assert.Empty(requirements.Shown);
        Assert.Empty(requirements.Unresolved);
    }

    [Fact]
    public void DialogueTagThatNoConversationSetsGoesToTheReport()
    {
        var grant = new InnerObjectRecord(
            "Gift",
            "/Invented/AddItemDialogue.AddItemDialogue_C",
            new JsonObject()
        );
        var dialogue = new AssetRecord(
            "/Invented/Tale.Tale",
            "Dialogue",
            Parse("""{"Data":[{"Events":[{"$ref":"Gift"}],"Conditions":[{"$ref":"Check"}]}]}"""),
            [
                grant,
                new InnerObjectRecord(
                    "Check",
                    "/Invented/CheckIfDialogueTagIsValid.CheckIfDialogueTagIsValid_C",
                    Parse("""{"Tag to Find":"Nowhere"}""")
                ),
            ]
        );

        var requirements = WorldSourceData.DialogueConditions(Context(), dialogue, grant);

        Assert.Empty(requirements.Shown);
        Assert.Contains(
            "Nowhere",
            Assert.Single(requirements.Unresolved),
            StringComparison.Ordinal
        );
    }

    [Theory]
    [InlineData(true, "Available during Hallowed Eve.")]
    [InlineData(false, "Available outside Hallowed Eve.")]
    public void DialogueCalendarCheckUsesItsRequiredResult(bool required, string label)
    {
        var grant = new InnerObjectRecord(
            "Gift",
            "/Invented/AddItemDialogue.AddItemDialogue_C",
            new JsonObject()
        );
        var dialogue = new AssetRecord(
            "/Invented/Tale.Tale",
            "Dialogue",
            Parse("""{"Data":[{"Events":[{"$ref":"Gift"}],"Conditions":[{"$ref":"Calendar"}]}]}"""),
            [
                grant,
                new InnerObjectRecord(
                    "Calendar",
                    "/Invented/CheckIfIsHallowedEve.CheckIfIsHallowedEve_C",
                    new JsonObject { ["Required Result"] = required }
                ),
            ]
        );
        var condition = Assert.Single(
            WorldSourceData.DialogueConditions(Context(), dialogue, grant).Shown
        );
        Assert.Equal(ConditionKind.CalendarEvent, condition.Kind);
        Assert.Equal(label, condition.Label);
        Assert.Equal(EntityKind.CalendarEvent, condition.Entity!.Kind);
        Assert.Null(condition.Entity.Key);
    }

    [Fact]
    public void DialogueMissingAmountUsesOneAndExplicitZeroGrantsNothing()
    {
        var grant = Type("AddItemDialogue");
        var dialogue = new AssetRecord(
            "/Invented/Tale.Tale",
            "Dialogue",
            new JsonObject(),
            [
                new InnerObjectRecord(
                    "Gift",
                    grant.Path,
                    Parse("""{"Item To Add":{"$ref":"/Invented/Gift.Gift_C"}}""")
                ),
            ]
        );
        Assert.Equal(
            new Amount(1, 1),
            Assert
                .Single(
                    new DialogueGrantMechanism().FindSources(
                        Context([grant], dialogues: [dialogue])
                    )
                )
                .Source.Amount
        );
        dialogue.Objects[0].Properties["Amount to Give"] = 0;
        Assert.Empty(
            new DialogueGrantMechanism().FindSources(Context([grant], dialogues: [dialogue]))
        );
    }

    [Fact]
    public void FishingSpecificPoolOverridesDifficultyPoolsAndPreservesSpotNotes()
    {
        var minigame = Type(
            "Fishing_Minigame",
            """{"Beginner Loot Table":{"$map":[[{"$ref":"/Invented/Other.Other_C"},1]]}}"""
        );
        var spot = Type(
            "FishingSpot",
            """{"Min Difficulty":1,"Max Difficulty":1,"Remaining Fish":5}"""
        );
        var placement = Level(
            spot,
            """{"Use Specific Loot table?":true,"Specific Loot Table":{"$map":[[{"$ref":"/Invented/Fish.Fish_C"},4],[null,1],[{"$ref":"/Invented/Dud.Dud_C"},0]]}}"""
        );
        var sources = new FishingMechanism()
            .FindSources(Context([spot, minigame], levels: [placement]))
            .ToArray();
        var result = Assert.Single(sources, source => source.Source.Places.Count > 0);
        Assert.Equal("/Invented/Fish.Fish_C", result.ItemClass);
        Assert.Equal(new Odds(OddsEvent.Catch, 0.8, 0.8, null), result.Source.Odds);
        Assert.Equal(new[] { 1d, 5d }, NoteNumbers(result.Source));
        Assert.DoesNotContain(
            sources,
            source => source.ItemClass.Contains("Dud", StringComparison.Ordinal)
        );
    }

    [Fact]
    public void FishingDifficultyBoundsUseMergedSpotDefaults()
    {
        var minigame = Type(
            "Fishing_Minigame",
            """{"Beginner Loot Table":{"$map":[[{"$ref":"/Invented/Small.Small_C"},1]]},"Easy Loot Table":{"$map":[[{"$ref":"/Invented/Medium.Medium_C"},1]]},"Standard Lava Loot":{"$map":[[{"$ref":"/Invented/Lava.Lava_C"},1]]}}"""
        );
        var spot = Type(
            "FishingSpot",
            """{"Min Difficulty":2,"Max Difficulty":2,"Remaining Fish":5}"""
        );
        var result = Assert.Single(
            new FishingMechanism().FindSources(Context([spot, minigame], levels: [Level(spot)])),
            source => source.Source.Places.Count > 0
        );
        Assert.Equal("/Invented/Medium.Medium_C", result.ItemClass);
        Assert.Equal(new Odds(OddsEvent.Catch, 1, 1, null), result.Source.Odds);
        var lava = Assert.Single(
            new FishingMechanism().FindSources(
                Context([spot, minigame], levels: [Level(spot, """{"Lava Fish":true}""")])
            ),
            source => source.Source.Places.Count > 0
        );
        Assert.Equal("/Invented/Lava.Lava_C", lava.ItemClass);
        Assert.Equal(
            new Odds(OddsEvent.Catch, null, null, "How lava fishing chooses its catch is unknown."),
            lava.Source.Odds
        );
    }

    [Fact]
    public void GatheringAssociatesOnlySelectedWeightedPoolWithThePlacement()
    {
        var gatherer = Type(
            "HarvestableInteractionObject",
            """
            {"Preset":{"$enum":"Resource::NewEnumerator99"},"Maximum amount of items that can be harvested":0,"Required level":3,"Iron Deposit Loot":{"$map":[[{"$ref":"/Invented/Ore.Ore_C"},2],[null,3],[{"$ref":"/Invented/Dud.Dud_C"},0]]},"Copper Deposit Loot":{"$map":[[{"$ref":"/Invented/Other.Other_C"},1]]}}
            """
        );
        var enumType = new EnumRecord(
            "/Invented/Resource.Resource",
            [new EnumEntry("NewEnumerator99", 1, "Ore")]
        );
        var sources = new GatheringMechanism()
            .FindSources(Context([gatherer], levels: [Level(gatherer)], enums: [enumType]))
            .ToArray();
        var result = Assert.Single(sources, source => source.Source.Places.Count > 0);
        Assert.Equal("/Invented/Ore.Ore_C", result.ItemClass);
        Assert.Single(sources, source => source.ItemClass == "/Invented/Ore.Ore_C");
        Assert.Equal(new Odds(OddsEvent.Pick, 0.4, 0.4, null), result.Source.Odds);
        Assert.Equal(new[] { 0d }, NoteNumbers(result.Source));
        Assert.True(result.Source.Unconfirmed);
        Assert.Equal(3, Assert.Single(result.Source.Conditions).Value);
        // Every gathering node shares this class, so the node is told apart by the name it shows.
        Assert.Equal(gatherer.Path + "#Iron Deposit", result.Source.Entity.Key);
        Assert.DoesNotContain(
            sources,
            source => source.ItemClass.Contains("Dud", StringComparison.Ordinal)
        );
        var unused = Assert.Single(
            sources,
            source => source.ItemClass == "/Invented/Other.Other_C"
        ).Source;
        Assert.Empty(unused.Places);
        Assert.True(unused.Unconfirmed);
        Assert.Contains("No gathering spot in the world is known to give it.", unused.Notes);
    }

    [Fact]
    public void GatheringUniformArraysCountDuplicatesAndNullSelections()
    {
        var gatherer = Type(
            "Overworld_Gatherpoint",
            """
            {"Tree Items":[{"$ref":"/Invented/Log.Log_C"},{"$ref":"/Invented/Log.Log_C"},null],"Maximum amount of items that can be harvested":4,"Required level":7}
            """
        );
        var result = Assert.Single(new GatheringMechanism().FindSources(Context([gatherer])));
        Assert.Equal(new Odds(OddsEvent.Pick, 2.0 / 3, 2.0 / 3, null), result.Source.Odds);
        Assert.Equal(new[] { 4d }, NoteNumbers(result.Source));
        Assert.True(result.Source.Unconfirmed);
        Assert.Contains(
            "No gathering spot in the world is known to use this gathering method.",
            result.Source.Notes
        );
    }

    [Fact]
    public void GatheringHetrumPresetUsesItsOwnPoolAndPlacedOverride()
    {
        var gatherer = Type(
            "HarvestableInteractionObject",
            """{"Preset":11,"Hetrum Deposit Loot":{"$map":[[{"$ref":"/Invented/Old.Old_C"},1]]},"Bone Pile Loot":{"$map":[[{"$ref":"/Invented/Bone.Bone_C"},1]]}}"""
        );
        var sources = new GatheringMechanism()
            .FindSources(
                Context(
                    [gatherer],
                    levels:
                    [
                        Level(
                            gatherer,
                            """{"Hetrum Deposit Loot":{"$map":[[{"$ref":"/Invented/Crystal.Crystal_C"},2],[null,1]]}}"""
                        ),
                    ]
                )
            )
            .ToArray();
        var placed = Assert.Single(sources, source => source.Source.Places.Count > 0);
        Assert.Equal("/Invented/Crystal.Crystal_C", placed.ItemClass);
        Assert.Equal(2.0 / 3, placed.Source.Odds!.Chance);
        Assert.False(placed.Source.Unconfirmed);
        Assert.DoesNotContain(sources, source => source.ItemClass == "/Invented/Old.Old_C");
        Assert.Empty(
            Assert
                .Single(sources, source => source.ItemClass == "/Invented/Bone.Bone_C")
                .Source.Places
        );
    }

    [Fact]
    public void AllZeroFarmingPoolsDoNotGrantItems()
    {
        var table = Table(
            "SeedDataTable",
            """{"Yield":10,"HarvestItems":{"$map":[[{"$ref":"/Invented/Fruit.Fruit_C"},0]]}}"""
        );
        Assert.Empty(new FarmingMechanism().FindSources(Context(tables: [table])));
    }

    [Fact]
    public void BountyNoteReferencesInOtherQuestTypesAreNotAcceptanceGrants()
    {
        var table = Table(
            "DT_UQC_Quests",
            """{"Type":1,"ItemBountyNoteClass":{"$ref":"/Invented/Note.Note_C"}}"""
        );
        Assert.Empty(new BountyNoteMechanism().FindSources(Context(tables: [table])));
    }

    [Fact]
    public void ExplicitEmptyPlacedFishingPoolDoesNotFallBackToMinigameLoot()
    {
        var minigame = Type(
            "Fishing_Minigame",
            """{"Beginner Loot Table":{"$map":[[{"$ref":"/Invented/Fish.Fish_C"},1]]}}"""
        );
        var spot = Type(
            "FishingSpot",
            """{"Min Difficulty":1,"Max Difficulty":1,"Remaining Fish":3,"Use Specific Loot table?":true,"Specific Loot Table":{"$map":[[{"$ref":"/Invented/Old.Old_C"},1]]}}"""
        );
        Assert.DoesNotContain(
            new FishingMechanism().FindSources(
                Context(
                    [minigame, spot],
                    levels: [Level(spot, """{"Specific Loot Table":{"$map":[]}}""")]
                )
            ),
            source => source.Source.Places.Count > 0
        );
    }

    [Theory]
    [InlineData("GatherPoint", true)]
    [InlineData("Overworld_Gatherpoint", false)]
    public void GatheringKeepsCrownberryConsumerBranchesDistinct(string className, bool givesItem)
    {
        var gatherer = Type(
            className,
            """{"GatherPointType":11,"Abyssberry Bush Table":{"$map":[[{"$ref":"/Invented/Berry.Berry_C"},1]]}}"""
        );
        var placed = new GatheringMechanism()
            .FindSources(Context([gatherer], levels: [Level(gatherer)]))
            .Where(source => source.Source.Places.Count > 0)
            .ToArray();
        Assert.Equal(givesItem ? 1 : 0, placed.Length);
    }

    [Theory]
    [InlineData(9, "Bone Pile Loot")]
    [InlineData(10, "Magma Stone Loot")]
    public void GatheringConstructionSelectsBoneAndMagmaPools(int preset, string field)
    {
        var gatherer = Type("HarvestableInteractionObject");
        gatherer.Properties["Preset"] = preset;
        gatherer.Properties[field] = Parse(
            """{"$map":[[{"$ref":"/Invented/Resource.Resource_C"},2],[null,1]]}"""
        );
        var placed = Assert.Single(
            new GatheringMechanism().FindSources(Context([gatherer], levels: [Level(gatherer)])),
            source => source.Source.Places.Count > 0
        );
        Assert.Equal(2.0 / 3, placed.Source.Odds!.Chance);
    }

    [Theory]
    [InlineData("Iron", "Iron Deposit Loot")]
    [InlineData("Copper", "Copper Deposit Loot")]
    [InlineData("Pine", "Pine Tree Loot")]
    public void GatheringCustomPresetsUseAuthoredResourceMaps(string preset, string field)
    {
        var gatherer = Type("HarvestableInteractionObject", """{"Custom Preset":true}""");
        gatherer.Properties["Custom Preset Name"] = preset;
        gatherer.Properties[field] = Parse(
            """{"$map":[[{"$ref":"/Invented/Resource.Resource_C"},1]]}"""
        );
        Assert.Single(
            new GatheringMechanism().FindSources(Context([gatherer], levels: [Level(gatherer)])),
            source => source.Source.Places.Count > 0
        );
        gatherer.Properties["Table Based Preset?"] = true;
        Assert.DoesNotContain(
            new GatheringMechanism().FindSources(Context([gatherer], levels: [Level(gatherer)])),
            source => source.Source.Places.Count > 0
        );
    }

    [Theory]
    [InlineData("WarpSpike", "Warpshards")]
    [InlineData("WheatField_Volume", "Wheat Ref")]
    public void BespokeGatheringUsesMergedGrantReferencesAndInventoryQuantities(
        string className,
        string field
    )
    {
        var gatherer = Type(className);
        gatherer.Properties[field] = Parse("""{"$ref":"/Invented/Old.Old_C"}""");
        var output = Type("Resource", """{"ItemInfo":{"Quantity":3}}""");
        var properties = new JsonObject
        {
            [field] = Parse("""{"$ref":"/Invented/Resource.Resource_C"}"""),
        };
        var level = new LevelRecord(
            "/Invented/Coast",
            [],
            [new ActorRecord("Harvest", gatherer.Path, properties)]
        );
        var source = Assert.Single(
            new GatheringMechanism().FindSources(Context([gatherer, output], levels: [level]))
        );
        Assert.Equal(output.Path, source.ItemClass);
        Assert.Equal(new Amount(3, 3), source.Source.Amount);
        Assert.Equal("/Invented/Coast", Assert.Single(source.Source.Places).Key);
        Assert.False(source.Source.Unconfirmed);
        var unplaced = Assert.Single(
            new GatheringMechanism().FindSources(Context([gatherer, output]))
        );
        Assert.Empty(unplaced.Source.Places);
        Assert.True(unplaced.Source.Unconfirmed);
        Assert.Contains(
            "No gathering spot in the world is known to give it.",
            unplaced.Source.Notes
        );
        gatherer.Properties[field] = null;
        Assert.Empty(new GatheringMechanism().FindSources(Context([gatherer, output])));
    }

    [Theory]
    [InlineData(true, "Readable name")]
    [InlineData(false, "Invented Character")]
    public void UnreadableFormattedNpcNamesUseReadableFallbacks(bool hasName, string expected)
    {
        var properties = Parse("""{"Full Name":{"$unsupported":"TextHistory.ArgumentFormat"}}""");
        if (hasName)
        {
            properties["Name"] = "Readable name";
        }
        Assert.Equal(
            expected,
            WorldSourceData.Name(properties, "/Invented/InventedCharacter.InventedCharacter_C")
        );
    }
}
