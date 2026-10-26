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
        Assert.Equal(new Weight(2, 5), result.Source.Weight);
        Assert.Null(result.Source.Chance);
        Assert.Null(result.Source.Amount);
        Assert.Equal("Seed", Assert.Single(result.Source.Inputs).Item.Name);
        Assert.Empty(result.Source.Places);
        Assert.Equal(new[] { 4d, 2d, 7d }, NoteNumbers(result.Source));
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
        Assert.Equal(new Weight(2, 3), result.Source.Weight);
        Assert.Equal("Invented brewery", result.Source.Name);
        Assert.Null(result.Source.Amount);
        Assert.Equal(new[] { 10d }, NoteNumbers(result.Source));
        Assert.Empty(result.Source.Places);
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
        Assert.Equal("/Invented/Coast", Assert.Single(result.Source.Places).Id);
        var unplaced = Assert.Single(
            new DialogueGrantMechanism().FindSources(Context([grant], dialogues: [dialogue]))
        );
        Assert.Empty(unplaced.Source.Places);
        Assert.Equal(new Amount(3, 3), unplaced.Source.Amount);
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
        Assert.Equal(new Weight(4, 5), result.Source.Weight);
        Assert.Equal(0.8, result.Source.Chance);
        Assert.Equal("Chance per catch", result.Source.ChanceBasis);
        Assert.Equal(new[] { 1d, 1d, 5d }, NoteNumbers(result.Source));
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
        Assert.Equal(1, result.Source.Chance);
        Assert.Equal("Chance per catch", result.Source.ChanceBasis);
        var lava = Assert.Single(
            new FishingMechanism().FindSources(
                Context([spot, minigame], levels: [Level(spot, """{"Lava Fish":true}""")])
            ),
            source => source.Source.Places.Count > 0
        );
        Assert.Equal("/Invented/Lava.Lava_C", lava.ItemClass);
        Assert.Null(lava.Source.Chance);
        Assert.Null(lava.Source.ChanceBasis);
        Assert.Contains(
            lava.Source.Notes,
            note => note.Contains("unconfirmed", StringComparison.Ordinal)
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
        Assert.Equal(new Weight(2, 5), result.Source.Weight);
        Assert.Equal(0.4, result.Source.Chance);
        Assert.Equal("Chance per roll", result.Source.ChanceBasis);
        Assert.Equal(new[] { 0d, 3d }, NoteNumbers(result.Source));
        Assert.DoesNotContain(
            sources,
            source => source.ItemClass.Contains("Dud", StringComparison.Ordinal)
        );
        Assert.Empty(
            Assert
                .Single(sources, source => source.ItemClass == "/Invented/Other.Other_C")
                .Source.Places
        );
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
        Assert.Equal(2.0 / 3, result.Source.Chance);
        Assert.Equal("Chance per roll", result.Source.ChanceBasis);
        Assert.Null(result.Source.Weight);
        Assert.Equal(new[] { 4d }, NoteNumbers(result.Source));
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
        Assert.Equal(new Weight(2, 3), placed.Source.Weight);
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
        Assert.Equal(new Weight(2, 3), placed.Source.Weight);
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
        Assert.Equal("/Invented/Coast", Assert.Single(source.Source.Places).Id);
        var unplaced = Assert.Single(
            new GatheringMechanism().FindSources(Context([gatherer, output]))
        );
        Assert.Empty(unplaced.Source.Places);
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
