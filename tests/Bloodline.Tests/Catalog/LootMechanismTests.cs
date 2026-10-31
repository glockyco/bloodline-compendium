using System.Text.Json.Nodes;
using Bloodline.Catalog;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Tests.Catalog;

public sealed class LootMechanismTests
{
    private const string Item = "/Game/Invented/Token.Token_C";
    private const string Container = "/Game/Invented/Box.Box_C";
    private const string Enemy = "/Game/Invented/Beast.Beast_C";
    private const string Level = "/Game/Invented/Meadow";

    private static JsonObject Parse(string json) => JsonNode.Parse(json)!.AsObject();

    private static JsonObject Pool(double weight = 2) =>
        new()
        {
            ["$map"] = new JsonArray(
                new JsonArray(new JsonObject { ["$ref"] = Item }, JsonValue.Create(weight)),
                new JsonArray(null, JsonValue.Create(6))
            ),
        };

    // Containers pick once with these bounds, so a source's chance per container is its pool share.
    private static JsonObject OnePick(JsonObject properties)
    {
        properties["Min amount of loot items"] = 0;
        properties["Max amount of loot items"] = 0;
        return properties;
    }

    private static CatalogContext Context(
        IReadOnlyList<ClassRecord> classes,
        IReadOnlyList<ActorRecord>? actors = null,
        IReadOnlyList<TableRecord>? tables = null
    )
    {
        var item = new ClassRecord(
            Item,
            [ItemIdentity.BaseItem],
            Parse("""{"ItemInfo":{"ItemName":{"$text":"Token"},"Quantity":3}}""")
        );
        var records = new CatalogRecords(
            new BuildRecord(1159290, 1, []),
            [item, .. classes],
            tables ?? [],
            [
                new EnumRecord(
                    "/Game/Invented/Loot",
                    [
                        new EnumEntry("Loot::NewEnumerator1", 1, "Dungeon"),
                        new EnumEntry("Loot::NewEnumerator99", 3, "Fort"),
                        new EnumEntry("Loot::Rare", 5, "Rare"),
                        new EnumEntry("Style::First", 0, "First"),
                        new EnumEntry("Style::Second", 3, "Second"),
                    ]
                ),
            ],
            [new LevelRecord(Level, [], actors ?? [])],
            [],
            [],
            []
        );
        return new CatalogContext(records, new Dictionary<string, ItemPage>(), []);
    }

    [Fact]
    public void ContainerUsesSelectedPoolAndKeepsEmptyOutcomesInItsChance()
    {
        var properties = Parse(
            """{"Object Name":{"$text":"Old box"},"Min amount of loot items":2,"Max amount of loot items":4,"Spawn Chance":0.5}"""
        );
        properties["Selected Loot Table"] = Pool();
        properties["Loot Table - Dungeon"] = Pool(99);
        var context = Context(
            [new ClassRecord(Container, [ContainerLootMechanism.Root], properties)],
            [new ActorRecord("Box1", Container, new JsonObject())]
        );
        var candidate = Assert.Single(new ContainerLootMechanism().FindSources(context));
        var source = candidate.Source;
        Assert.Equal("Old box", source.Name);
        Assert.Equal(new Amount(3, 3), source.Amount);
        Assert.Equal(Level, Assert.Single(source.Places).Key);
        Assert.Equal(
            LootOdds.Of(OddsEvent.Container, 0.25, PickCount.Container(2, 4)),
            source.Odds
        );
        Assert.Empty(source.Inputs);
        Assert.Empty(source.Notes);
        Assert.Equal(0.5, candidate.Container!.AppearanceChance);
    }

    [Theory]
    [InlineData("Crate", false)]
    [InlineData("Loot", true)]
    public void GenericLootNameUsesContainerIdentity(string defaultName, bool unknownName)
    {
        var properties = OnePick(
            new JsonObject
            {
                ["Object Name"] = new JsonObject { ["$text"] = defaultName },
                ["Selected Loot Table"] = Pool(),
            }
        );
        var placed = Parse("""{"Object Name":{"$text":"Loot"}}""");
        var source = Assert
            .Single(
                new ContainerLootMechanism().FindSources(
                    Context(
                        [new ClassRecord(Container, [ContainerLootMechanism.Root], properties)],
                        [new ActorRecord("Box1", Container, placed)]
                    )
                )
            )
            .Source;
        Assert.NotEqual("Loot", source.Name);
        if (!unknownName)
            Assert.Equal(defaultName, source.Name);
        Assert.Equal(0.25, source.Odds!.Chance);
        Assert.Equal(new Amount(3, 3), source.Amount);
        Assert.Equal(Level, Assert.Single(source.Places).Key);
    }

    [Theory]
    [InlineData("MarshBeast12")]
    [InlineData("MarshBeast_Base")]
    [InlineData("MarshBeast_Idle")]
    public void UnnamedEnemyVariantUsesReadableIdentityWithoutDevelopmentSuffixes(string className)
    {
        var variant = $"/Game/Invented/{className}.{className}_C";
        var properties = Parse("""{"Enemy Name":{"$text":""}}""");
        properties["Loot Table"] = Pool();
        var source = Assert
            .Single(
                new EnemyDropMechanism().FindSources(
                    Context([new ClassRecord(variant, [], properties)])
                )
            )
            .Source;
        Assert.Equal("Marsh Beast", source.Name);
        Assert.Equal(0.25, source.Odds!.Chance);
        Assert.Equal(new Amount(3, 3), source.Amount);
    }

    [Fact]
    public void PlacedOverridesReplaceDefaultPoolAndName()
    {
        var properties = OnePick(Parse("""{"Object Name":{"$text":"Default box"}}"""));
        properties["Selected Loot Table"] = Pool(9);
        var placed = Parse("""{"Object Name":{"$text":"Placed box"}}""");
        placed["Selected Loot Table"] = Pool(1);
        var source = Assert
            .Single(
                new ContainerLootMechanism().FindSources(
                    Context(
                        [new ClassRecord(Container, [ContainerLootMechanism.Root], properties)],
                        [new ActorRecord("Box1", Container, placed)]
                    )
                )
            )
            .Source;
        Assert.Equal("Placed box", source.Name);
        Assert.Equal(1.0 / 7, source.Odds!.Chance);
    }

    [Fact]
    public void CustomPoolOverridesMatchingSelectedEntries()
    {
        var properties = OnePick(
            Parse(
                """{"Loot Type":{"$enum":"Loot::NewEnumerator1"},"Add Custom Loot to Table":true}"""
            )
        );
        properties["Loot Table - Dungeon"] = Pool(9);
        properties["Custom Loot Table"] = Pool(1);
        var source = Assert
            .Single(
                new ContainerLootMechanism().FindSources(
                    Context([new ClassRecord(Container, [ContainerLootMechanism.Root], properties)])
                )
            )
            .Source;
        Assert.Equal(1.0 / 7, source.Odds!.Chance);
        Assert.Empty(source.Places);
    }

    [Fact]
    public void PoolSelectorUsesEnumNumericValueRatherThanEnumeratorSuffix()
    {
        var properties = OnePick(Parse("""{"Loot Type":{"$enum":"Loot::NewEnumerator99"}}"""));
        properties["Loot Table - Minolurk Fort"] = Pool(4);
        properties["Selected Loot Table"] = Pool(99);
        var source = Assert
            .Single(
                new ContainerLootMechanism().FindSources(
                    Context([new ClassRecord(Container, [ContainerLootMechanism.Root], properties)])
                )
            )
            .Source;
        Assert.Equal(0.4, source.Odds!.Chance);
    }

    [Theory]
    [InlineData("Style::First", false, "Hammer or Mace Loot Table Dwarven Dungeon RARE")]
    [InlineData("Style::First", true, "Spear Loot Table - DWARVEN")]
    [InlineData("Style::Second", false, "Undead Mace Dungeon Loot Table")]
    public void RarePoolFlagsOverrideTheGeneratedSwordAndDoNotAppendCustomLoot(
        string style,
        bool spear,
        string selectedField
    )
    {
        var properties = Parse(
            $$$"""{"Loot Type":{"$enum":"Loot::Rare"},"Randomized Loot Style":{"$enum":"{{{style}}}"},"Hammer or Mace":true,"Add Custom Loot to Table":true}"""
        );
        properties["Spear"] = spear;
        OnePick(properties);
        properties["Sword Loot Table - Dwarven Dungeon RARE"] = Pool(99);
        properties["Undead Sword Loot Table Undead Dungeon"] = Pool(99);
        properties["Custom Loot Table"] = Pool(99);
        properties[selectedField] = Pool(4);
        var source = Assert
            .Single(
                new ContainerLootMechanism().FindSources(
                    Context([new ClassRecord(Container, [ContainerLootMechanism.Root], properties)])
                )
            )
            .Source;
        Assert.Equal(0.4, source.Odds!.Chance);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LooseLootUsesMaximumSpawnCountRatherThanAnUnusedMinimum(bool hasMinimum)
    {
        var properties = Parse(
            """{"Use Preset Table?":true,"Preset Table Name":"generic","Min Spawn Amount":2,"Max Spawn Amount":5}"""
        );
        if (!hasMinimum)
            properties.Remove("Min Spawn Amount");
        properties["Generic Loot Table"] = Pool(4);
        properties["Loot Table"] = Pool(99);
        var source = Assert
            .Single(
                new ContainerLootMechanism().FindSources(
                    Context(
                        [
                            new ClassRecord(
                                Container,
                                [ContainerLootMechanism.LooseRoot],
                                properties
                            ),
                        ],
                        [new ActorRecord("Loose1", Container, new JsonObject())]
                    )
                )
            )
            .Source;
        Assert.Equal(LootOdds.Of(OddsEvent.Container, 0.4, PickCount.Fixed(5)), source.Odds);
        Assert.Equal(Level, Assert.Single(source.Places).Key);
        Assert.Contains(source.Notes, note => note.Contains("loose", StringComparison.Ordinal));
    }

    [Fact]
    public void LooseLootUsesNamedPresetEvenWithoutConstructionPresetSwitch()
    {
        var properties = Parse(
            """{"Preset Table Name":"inventedpreset","Use Preset Table?":false}"""
        );
        properties["Loot Table"] = Pool(99);
        var row = Parse("""{"ContainerName":{"$text":"Scattered fruit"}}""");
        row["ContainerLootTable"] = Pool(4);
        var source = Assert
            .Single(
                new ContainerLootMechanism().FindSources(
                    Context(
                        [
                            new ClassRecord(
                                Container,
                                [ContainerLootMechanism.LooseRoot],
                                properties
                            ),
                        ],
                        tables:
                        [
                            new TableRecord(
                                "/Game/Invented/PresetLootTables_DT",
                                null,
                                [new TableRow("InventedPreset", row)]
                            ),
                        ]
                    )
                )
            )
            .Source;
        Assert.Equal("Scattered fruit", source.Name);
        Assert.Equal(
            new Odds(OddsEvent.Container, null, null, "How many items lie here is unknown."),
            source.Odds
        );
    }

    [Fact]
    public void LooseLootDoesNotInventMissingOrBytecodeOnlyPresetPools()
    {
        var properties = Parse("""{"Preset Table Name":"Mining","Use Preset Table?":true}""");
        properties["Loot Table"] = Pool(99);
        Assert.Empty(
            new ContainerLootMechanism().FindSources(
                Context([
                    new ClassRecord(Container, [ContainerLootMechanism.LooseRoot], properties),
                ])
            )
        );
    }

    [Fact]
    public void PresetPoolUsesRowNameAndPlayerFacingContainerName()
    {
        var properties = OnePick(
            Parse("""{"Use Specific Loot Table?":true,"Loot Table Name":"inventedpreset"}""")
        );
        properties["Selected Loot Table"] = Pool(9);
        var row = Parse("""{"ContainerName":{"$text":"Fruit basket"}}""");
        row["ContainerLootTable"] = Pool(1);
        var source = Assert
            .Single(
                new ContainerLootMechanism().FindSources(
                    Context(
                        [new ClassRecord(Container, [ContainerLootMechanism.Root], properties)],
                        tables:
                        [
                            new TableRecord(
                                "/Game/Invented/PresetLootTables_DT",
                                null,
                                [new TableRow("InventedPreset", row)]
                            ),
                        ]
                    )
                )
            )
            .Source;
        Assert.Equal("Fruit basket", source.Name);
        Assert.Equal(1.0 / 7, source.Odds!.Chance);
    }

    [Fact]
    public void DisabledContainerDoesNotGrant()
    {
        var properties = new JsonObject { ["Spawn Chance"] = 0, ["Selected Loot Table"] = Pool() };
        Assert.Empty(
            new ContainerLootMechanism().FindSources(
                Context([new ClassRecord(Container, [ContainerLootMechanism.Root], properties)])
            )
        );
    }

    [Fact]
    public void ZeroLootCountSettingsRetainTheInclusiveInitialRoll()
    {
        var properties = Parse("""{"Min amount of loot items":0,"Max amount of loot items":0}""");
        properties["Selected Loot Table"] = Pool();
        var source = Assert
            .Single(
                new ContainerLootMechanism().FindSources(
                    Context([new ClassRecord(Container, [ContainerLootMechanism.Root], properties)])
                )
            )
            .Source;
        Assert.Equal(new Odds(OddsEvent.Container, 0.25, 0.25, null), source.Odds);
    }

    [Fact]
    public void EnemyZeroWeightEntriesDoNotGrant()
    {
        var properties = Parse("""{"Enemy Name":{"$text":"Marsh beast"}}""");
        properties["Loot Table"] = Pool(0);
        properties["Rare Pickup Loot"] = Pool(0);
        Assert.Empty(
            new EnemyDropMechanism().FindSources(Context([new ClassRecord(Enemy, [], properties)]))
        );
    }

    [Theory]
    [InlineData("npcParametersArr")]
    [InlineData("spawnDataArr")]
    public void PopulationSpawnerUsesBothRecordedArrays(string field)
    {
        var properties = Parse("""{"Enemy Name":{"$text":"Marsh beast"}}""");
        properties["Loot Table"] = Pool();
        var spawner = new JsonObject
        {
            [field] = JsonNode.Parse(
                """[{"classNPC":{"$ref":"/Game/Invented/Beast.Beast_C"},"maxSpawnNPC":1}]"""
            ),
        };
        var source = Assert
            .Single(
                new EnemyDropMechanism().FindSources(
                    Context(
                        [new ClassRecord(Enemy, [], properties)],
                        [new ActorRecord("Spawner", "/Game/Invented/Spawner.Spawner_C", spawner)]
                    )
                )
            )
            .Source;
        Assert.Equal(Level, Assert.Single(source.Places).Key);
    }

    [Fact]
    public void CustomAndDungeonSpawnersNameEnemyClasses()
    {
        var properties = Parse("""{"Enemy Name":{"$text":"Marsh beast"}}""");
        properties["Loot Table"] = Pool();
        var spawner = Parse(
            """{"Spawns":{"$map":[[{"$ref":"/Game/Invented/Beast.Beast_C"},1]]},"Enemy to Spawn":{"$ref":"/Game/Invented/Beast.Beast_C"}}"""
        );
        var source = Assert
            .Single(
                new EnemyDropMechanism().FindSources(
                    Context(
                        [new ClassRecord(Enemy, [], properties)],
                        [new ActorRecord("Spawner", "/Game/Invented/Spawner.Spawner_C", spawner)]
                    )
                )
            )
            .Source;
        Assert.Equal(Level, Assert.Single(source.Places).Key);
    }

    [Fact]
    public void ZeroWeightAndEntireTableContainersDoNotPublishStoredPool()
    {
        var properties = new JsonObject { ["Selected Loot Table"] = Pool(0) };
        Assert.Empty(
            new ContainerLootMechanism().FindSources(
                Context([new ClassRecord(Container, [ContainerLootMechanism.Root], properties)])
            )
        );
        properties["Selected Loot Table"] = Pool();
        properties["Use Entire Data Table?"] = true;
        Assert.Empty(
            new ContainerLootMechanism().FindSources(
                Context([new ClassRecord(Container, [ContainerLootMechanism.Root], properties)])
            )
        );
    }

    [Fact]
    public void RandomLootExplainsDynamicEligibilityWithoutInventingProbability()
    {
        var row = Parse(
            """{"ItemClass":{"$ref":"/Game/Invented/Token.Token_C"},"BaseSpawnRate":0.25,"Level":12}"""
        );
        var zero = row.DeepClone().AsObject();
        zero["BaseSpawnRate"] = 0;
        var context = Context(
            [],
            tables:
            [
                new TableRecord(
                    "/Game/Invented/AllLootSpawnRates",
                    null,
                    [new TableRow("Positive", row), new TableRow("Zero", zero)]
                ),
            ]
        );
        var source = Assert.Single(new RandomLootMechanism().FindSources(context)).Source;
        Assert.Equal(SourceKind.RandomLoot, source.Kind);
        Assert.Equal("General random loot", source.Name);
        Assert.Null(source.Odds!.Chance);
        Assert.Contains("your level", source.Odds.Unknown, StringComparison.Ordinal);
        Assert.Equal(new Amount(3, 3), source.Amount);
        Assert.Empty(source.Places);
    }

    [Theory]
    [InlineData("Beast", null)]
    [InlineData("CopperCrownBandit", 1.0)]
    [InlineData("Herald_Ashbinder", 0.25)]
    public void EnemyRarePoolDropsOnlyWhereADeathEventSpawnsIt(string className, double? gate)
    {
        var enemy = $"/Game/Invented/{className}.{className}_C";
        var properties = Parse("""{"Enemy Name":{"$text":"Marsh beast"}}""");
        properties["Loot Table"] = Pool();
        properties["Rare Pickup Loot"] = Pool();
        var spawner = new JsonObject
        {
            ["spawnDataArr"] = new JsonArray(
                new JsonObject { ["classNPC"] = new JsonObject { ["$ref"] = enemy } }
            ),
        };
        var sources = new EnemyDropMechanism()
            .FindSources(
                Context(
                    [new ClassRecord(enemy, [], properties)],
                    [new ActorRecord("Spawner", "/Game/Invented/Spawner.Spawner_C", spawner)]
                )
            )
            .Select(source => source.Source)
            .ToArray();
        Assert.All(
            sources,
            source =>
            {
                Assert.Equal("Marsh beast", source.Name);
                Assert.Equal(Level, Assert.Single(source.Places).Key);
                Assert.Equal(new Amount(3, 3), source.Amount);
            }
        );
        // The normal drop picks once per kill, and the rare drop once in as many kills as its event gates.
        var expected = new List<Odds> { new(OddsEvent.Kill, 0.25, 0.25, null) };
        if (gate is { } chance)
            expected.Add(new Odds(OddsEvent.Kill, chance * 0.25, chance * 0.25, null));
        Assert.Equal(expected, sources.Select(source => source.Odds));
    }

    [Fact]
    public void EnemyPlacedOverrideDoesNotReplaceSpawnedDefaultPool()
    {
        var properties = Parse("""{"Enemy Name":{"$text":"Marsh beast"}}""");
        properties["Loot Table"] = Pool(2);
        var placed = new JsonObject { ["Loot Table"] = Pool(1) };
        var spawner = Parse(
            """{"spawnDataArr":[{"classNPC":{"$ref":"/Game/Invented/Beast.Beast_C"}}]}"""
        );
        var sources = new EnemyDropMechanism()
            .FindSources(
                Context(
                    [new ClassRecord(Enemy, [], properties)],
                    [
                        new ActorRecord("Beast1", Enemy, placed),
                        new ActorRecord("Spawner", "/Game/Invented/Spawner.Spawner_C", spawner),
                    ]
                )
            )
            .ToArray();
        Assert.Equal(2, sources.Length);
        Assert.Contains(sources, source => source.Source.Odds!.Chance == 1.0 / 7);
        Assert.Contains(sources, source => source.Source.Odds!.Chance == 0.25);
    }

    [Theory]
    [InlineData("Enemy To Spawn")]
    [InlineData("Actor To Spawn")]
    [InlineData("Enemy Type")]
    public void AuthoredSpawnerClassFieldsReportTheirUncheckedConditions(string field)
    {
        var properties = Parse("""{"Enemy Name":{"$text":"Marsh beast"}}""");
        properties["Loot Table"] = Pool();
        var spawner = new JsonObject { [field] = new JsonObject { ["$ref"] = Enemy } };
        var source = Assert.Single(
            new EnemyDropMechanism().FindSources(
                Context(
                    [new ClassRecord(Enemy, [], properties)],
                    [new ActorRecord("Spawner", "/Game/Invented/Spawner.Spawner_C", spawner)]
                )
            )
        );
        Assert.Equal(Level, Assert.Single(source.Source.Places).Key);
        Assert.Contains(
            source.Unresolved,
            label => label.Contains("spawner", StringComparison.OrdinalIgnoreCase)
        );
    }

    [Theory]
    [InlineData("Quest Units To Spawn")]
    [InlineData("Wave 3 Invented Challenge")]
    public void QuestAndWaveSpawnerArraysNameEnemyClasses(string field)
    {
        var properties = Parse("""{"Enemy Name":{"$text":"Marsh beast"}}""");
        properties["Loot Table"] = Pool();
        var spawner = new JsonObject
        {
            [field] = new JsonArray(new JsonObject { ["$ref"] = Enemy }),
        };
        var source = Assert
            .Single(
                new EnemyDropMechanism().FindSources(
                    Context(
                        [new ClassRecord(Enemy, [], properties)],
                        [new ActorRecord("Spawner", "/Game/Invented/Spawner.Spawner_C", spawner)]
                    )
                )
            )
            .Source;
        Assert.Equal(Level, Assert.Single(source.Places).Key);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(0, 0)]
    public void LeveledSpawnerMapIgnoresZeroSpawnWeights(double weight, int expectedPlaces)
    {
        var properties = Parse("""{"Enemy Name":{"$text":"Marsh beast"}}""");
        properties["Loot Table"] = Pool();
        var spawner = new JsonObject
        {
            ["Level 7 Spawn Rates"] = new JsonObject
            {
                ["$map"] = new JsonArray(
                    new JsonArray(new JsonObject { ["$ref"] = Enemy }, JsonValue.Create(weight))
                ),
            },
        };
        var source = Assert
            .Single(
                new EnemyDropMechanism().FindSources(
                    Context(
                        [new ClassRecord(Enemy, [], properties)],
                        [new ActorRecord("Spawner", "/Game/Invented/Spawner.Spawner_C", spawner)]
                    )
                )
            )
            .Source;
        Assert.Equal(expectedPlaces, source.Places.Count);
        Assert.Equal(new Odds(OddsEvent.Kill, 0.25, 0.25, null), source.Odds);
    }

    [Fact]
    public void EmptyEnemyDisplayNameUsesAnExplicitlyUncertainClassName()
    {
        var properties = Parse("""{"Enemy Name":{"$text":""}}""");
        properties["Loot Table"] = Pool();
        var source = Assert
            .Single(
                new EnemyDropMechanism().FindSources(
                    Context([new ClassRecord(Enemy, [], properties)])
                )
            )
            .Source;
        Assert.Equal("Beast", source.Name);
    }

    [Fact]
    public void ContainerWithUnknownPickCountHasNoChance()
    {
        var properties = new JsonObject { ["Selected Loot Table"] = Pool() };
        var source = Assert
            .Single(
                new ContainerLootMechanism().FindSources(
                    Context([new ClassRecord(Container, [ContainerLootMechanism.Root], properties)])
                )
            )
            .Source;
        Assert.Equal(
            new Odds(
                OddsEvent.Container,
                null,
                null,
                "How many items this container holds is unknown."
            ),
            source.Odds
        );
    }
}
