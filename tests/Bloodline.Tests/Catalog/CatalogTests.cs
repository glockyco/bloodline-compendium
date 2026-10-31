using System.Text.Json.Nodes;
using Bloodline.Catalog;
using Bloodline.Output;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Tests.Catalog;

public sealed class CatalogTests
{
    private static readonly SiteRelease TestRelease = new(
        "Invented Update",
        "2026-01-01",
        "https://example.com/patch",
        "2026-01-02"
    );

    private static JsonObject Parse(string json) => JsonNode.Parse(json)!.AsObject();

    private static ClassRecord Item(
        string path = "/Game/Items/IronSword.IronSword_C",
        string name = "Iron sword",
        string category = "Weapon"
    ) =>
        new(
            path,
            [ItemIdentity.BaseItem],
            Parse(
                $$$"""
                {"ItemInfo":{"ItemName":{"$text":"{{{name}}}"},"Description":{"$text":"Invented description"},"ItemType":{"$enum":"E_ItemType::Weapon","display":"{{{category}}}"},"ItemValue":12,"SpecificItemType":{"$enum":"Specific::OneHand","display":"One Handed Weapon"},"ItemImage":{"$ref":"/Game/Icons/Sword.Sword"},"Damage":900},
                 "WeaponInfo":{"Damage":10,"AttackSpeed":1.25,"Range":150,"DamageType":{"$enum":"Damage::Slash","display":"Slashing"},"AttackType":{"$enum":"Attack::Sword","display":"Sword"},"MagazineSize":0}}
                """
            )
        );

    private static CatalogRecords Records(
        IReadOnlyList<ClassRecord>? classes = null,
        IReadOnlyList<LevelRecord>? levels = null,
        IReadOnlyList<GapRecord>? gaps = null
    ) =>
        new(
            new BuildRecord(1159290, 1, []),
            classes ?? [Item()],
            [],
            [],
            levels ?? [],
            [],
            [new TextureRecord("/Game/Icons/Sword.Sword", "aabb", 1, 1, "BGRA")],
            gaps ?? []
        );

    private static CatalogResult Build(
        CatalogRecords records,
        IReadOnlyList<Exclusion>? exclusions = null
    ) => CatalogBuilder.Build(records, exclusions ?? []);

    [Theory]
    [InlineData("unusedDialogue", false)]
    [InlineData("unusedGatheringDefinition", false)]
    [InlineData("zeroGatheringYield", false)]
    [InlineData("unplacedGathering", false)]
    [InlineData("unplacedGatheringMethod", false)]
    [InlineData("zeroBespokeYield", false)]
    [InlineData("unplacedPickup", false)]
    [InlineData("unplacedLoosePickup", false)]
    [InlineData("unplacedPickup", true)]
    public void FileOnlySourcesRemainPublishedWithoutClaimingAvailability(
        string sourceCase,
        bool hasConfirmedPickup
    )
    {
        var item = Item();
        var reference = new JsonObject { ["$ref"] = item.Path };
        var classes = new List<ClassRecord> { item };
        var levels = new List<LevelRecord>();
        var dialogues = new List<AssetRecord>();
        if (sourceCase == "unusedDialogue")
        {
            var grant = "/Invented/AddItemDialogue.AddItemDialogue_C";
            classes.Add(new ClassRecord(grant, [], new JsonObject()));
            dialogues.Add(
                new AssetRecord(
                    "/Invented/Dialogue_Unused/Tale.Tale",
                    "Dialogue",
                    new JsonObject(),
                    [
                        new InnerObjectRecord(
                            "Gift",
                            grant,
                            new JsonObject { ["Item To Add"] = reference }
                        ),
                    ]
                )
            );
        }
        else
        {
            var properties = new JsonObject();
            string path;
            IReadOnlyList<string> ancestors = [];
            if (sourceCase == "unplacedPickup")
            {
                path = "/Invented/Bundle.Bundle_C";
                ancestors = [ItemIdentity.Interactable];
                properties["Item"] = reference;
            }
            else if (sourceCase == "unplacedLoosePickup")
            {
                path = ContainerLootMechanism.LooseRoot;
                properties["Loot Table"] = new JsonObject
                {
                    ["$map"] = new JsonArray(new JsonArray(reference, JsonValue.Create(1))),
                };
                properties["Max Spawn Amount"] = 1;
            }
            else if (sourceCase == "zeroBespokeYield")
            {
                path = "/Invented/WarpSpike.WarpSpike_C";
                properties["Warpshards"] = reference;
                item.Properties["ItemInfo"]!["Quantity"] = 0;
            }
            else if (sourceCase == "unplacedGatheringMethod")
            {
                path = "/Invented/Overworld_Gatherpoint.Overworld_Gatherpoint_C";
                properties["Tree Items"] = new JsonArray(reference);
            }
            else
            {
                path = "/Invented/HarvestableInteractionObject.HarvestableInteractionObject_C";
                properties["Preset"] = 1;
                properties[
                    sourceCase == "unusedGatheringDefinition"
                        ? "Copper Deposit Loot"
                        : "Iron Deposit Loot"
                ] = new JsonObject
                {
                    ["$map"] = new JsonArray(new JsonArray(reference, JsonValue.Create(1))),
                };
                properties["Maximum amount of items that can be harvested"] =
                    sourceCase == "zeroGatheringYield" ? 0 : 3;
            }
            classes.Add(new ClassRecord(path, ancestors, properties));
            if (
                sourceCase
                is "unusedGatheringDefinition"
                    or "zeroGatheringYield"
                    or "zeroBespokeYield"
            )
            {
                levels.Add(
                    new LevelRecord(
                        "/Invented/Meadow",
                        [],
                        [new ActorRecord("Resource", path, new JsonObject())]
                    )
                );
            }
        }
        if (hasConfirmedPickup)
        {
            levels.Add(
                new LevelRecord(
                    "/Invented/Coast",
                    [],
                    [new ActorRecord("Pickup", item.Path, new JsonObject())]
                )
            );
        }
        var result = Build(Records(classes, levels) with { Dialogues = dialogues });
        var page = Assert.Single(result.Items);
        var unconfirmed = Assert.Single(page.Sources, source => source.Unconfirmed);
        Assert.NotEmpty(unconfirmed.Notes);
        Assert.Contains(
            result.Graph.Relations,
            relation => relation.Id == unconfirmed.RelationId && relation.Attributes!.Unconfirmed
        );
        var expected = hasConfirmedPickup ? ItemStatus.Obtainable : ItemStatus.NoKnownSource;
        Assert.Equal(expected, page.Status);
        Assert.Equal(expected, Assert.Single(result.Index.Items).Status);
        if (!hasConfirmedPickup)
        {
            Assert.False(Assert.Single(result.Index.Items).Facets.ContainsKey("source"));
            Assert.False(Assert.Single(result.Index.Items).Facets.ContainsKey("place"));
        }
    }

    [Fact]
    public void SelectsNamedInventoryItemsButNotPlacedInteractables()
    {
        var item = Item();
        var bench = Item("/Game/Bench.Bench_C") with
        {
            Ancestors = [ItemIdentity.Interactable, ItemIdentity.BaseItem],
        };
        var rootInteractable = Item(ItemIdentity.Interactable);
        var unnamed = Item("/Game/Unnamed.Unnamed_C", " ");
        var unrelated = Item("/Game/Other.Other_C") with { Ancestors = [] };
        Assert.Single(Build(Records([bench, unrelated, item, unnamed, rootInteractable])).Items);
    }

    [Fact]
    public void SlugsUseDisplayNamesAndResolveCollisionsIndependentlyOfOrdering()
    {
        var a = Item("/Game/A/IronSword.IronSword_C");
        var b = Item("/Game/B/Iron_Sword.Iron_Sword_C");
        var alone = ItemIdentity.Slugs([a]);
        Assert.Equal("iron-sword", alone[a.Path]);
        Assert.Equal(
            "renamed",
            ItemIdentity.Slugs([a with { Properties = Item(name: "Renamed").Properties }])[a.Path]
        );
        var forward = ItemIdentity.Slugs([a, b]);
        var backward = ItemIdentity.Slugs([b, a]);
        Assert.Equal(forward[a.Path], backward[a.Path]);
        Assert.Equal(forward[b.Path], backward[b.Path]);
        Assert.NotEqual(forward[a.Path], forward[b.Path]);
    }

    [Theory]
    [InlineData("HeadArmor_CelestialKnightHelm", "head-armor-celestial-knight-helm")]
    [InlineData("BP_ABCThing", "bp-abc-thing")]
    public void SplitsClassNames(string name, string expected) =>
        Assert.Equal(expected, ItemIdentity.Slug(name));

    [Fact]
    public void MissingDamageIsUnknownRatherThanUsingInventoryDisplayDamage()
    {
        var item = Item();
        item.Properties["WeaponInfo"]!.AsObject().Remove("Damage");
        var result = Build(Records([item]));
        Assert.Null(Assert.Single(result.Items[0].Facts, fact => fact.Label == "Damage").Value);
        Assert.Contains(
            result.Report.Gaps.Single(gap => gap.Kind == GapKind.UnknownFact).Entries,
            entry => entry.Entity?.Slug == result.Items[0].Slug && entry.Detail == "Damage"
        );
    }

    [Fact]
    public void UsesEquippedWeaponDamageAndTheArmorTableInsteadOfDisplayDefaults()
    {
        var weapon = Item();
        var armor = Item("/Game/Armor.Armor_C", "Iron Cuirass", "Armor");
        armor.Properties["ItemInfo"]!["ItemSlot"] = Parse(
            """{"$enum":"Slot::Torso","display":"Torso"}"""
        );
        armor.Properties["ArmorInfo"] = Parse("""{"DamageRating":7}""");
        var table = new TableRecord(
            "/Game/BloodlinesAlpha/ArmorDataTable.ArmorDataTable",
            null,
            [
                new TableRow(
                    "IronCuirass",
                    Parse("""{"ArmorClass":{"$text":"Heavy"},"ArmorValue":13}""")
                ),
            ]
        );
        var result = Build(Records([weapon, armor]) with { Tables = [table] });
        Assert.Equal(
            "10",
            result
                .Items.Single(item => item.Key == weapon.Path)
                .Facts.Single(fact => fact.Label == "Damage")
                .Value
        );
        Assert.Equal(
            "13",
            result
                .Items.Single(item => item.Key == armor.Path)
                .Facts.Single(fact => fact.Label == "Armor rating")
                .Value
        );
    }

    [Theory]
    [InlineData("Etc.")]
    [InlineData("Armor")]
    [InlineData("Food")]
    public void UnrelatedItemsDoNotClaimToBeOneHandedWeapons(string category)
    {
        var result = Build(Records([Item(category: category)]));
        Assert.DoesNotContain(result.Items[0].Facts, fact => fact.Label == "Type");
    }

    [Fact]
    public void CoverageListsOnlyTooltipDifferencesWithoutAKnownCause()
    {
        var sword = Item();
        var pants = new ClassRecord(
            "/Game/Items/Pants.Pants_C",
            [ItemIdentity.BaseItem],
            Parse(
                """{"ItemInfo":{"ItemName":{"$text":"Leather pants"},"Description":{"$text":"Invented description"},"ItemType":{"$enum":"E_ItemType::Armor","display":"Armor"},"ItemSlot":{"$enum":"Slot::Torso","display":"Torso"},"ItemValue":3,"ItemImage":{"$ref":"/Game/Icons/Sword.Sword"},"Damage":4}}"""
            )
        );
        TooltipObservation Tooltip(ClassRecord item, string name, string stat) =>
            new(
                item.Path,
                "run",
                "W_Tooltip_QualityWhite_C",
                new Dictionary<string, string>
                {
                    ["TextBlock_0"] = name,
                    ["TextBlock_2"] = "Invented description",
                    ["TextBlock_3"] = stat,
                },
                ["SpeedTypeBlock"]
            );
        // An armor table without a row for the pants makes them armor that adds none.
        var armorTable = new TableRecord("/Game/Invented/ArmorDataTable.ArmorDataTable", null, []);
        var result = Build(
            new CatalogRecords(
                new BuildRecord(1159290, 1, []),
                [sword, pants],
                [armorTable],
                [],
                [],
                [],
                [new TextureRecord("/Game/Icons/Sword.Sword", "aabb", 1, 1, "BGRA")],
                []
            ) with
            {
                Tooltips =
                [
                    Tooltip(sword, "Iron sword", "Damage: 900"),
                    Tooltip(pants, "Leather pants", "ARMOR: 4"),
                ],
                Issues = new KnownIssueRegistry([
                    new KnownIssue(
                        "armor-without-row",
                        "Invented issue",
                        "Invented effect.",
                        null,
                        ["Invented step."],
                        "Invented expectation.",
                        "Invented observation.",
                        "Invented cause.",
                        ["/Game/Invented/Tooltip:Show"],
                        [1],
                        ReportStatus.NotReported,
                        null
                    ),
                ]),
            }
        );

        Assert.Equal(2, result.Tooltips!.Differences.Count);
        var entry = Assert.Single(
            Assert.Single(result.Report.Gaps, gap => gap.Kind == GapKind.TooltipDifference).Entries
        );
        Assert.Equal(sword.Path, entry.Entity!.Key);
        Assert.Contains("900", entry.Detail);
    }

    [Fact]
    public void ExcludedLevelPlacementsCannotBecomePickupSources()
    {
        var item = Item();
        var levels = new[]
        {
            new LevelRecord(
                "/Game/Test",
                [],
                [new ActorRecord("Sword", item.Path, new JsonObject())]
            ),
            new LevelRecord(
                "/Game/Live",
                [],
                [new ActorRecord("Sword", item.Path, new JsonObject())]
            ),
        };
        var result = Build(
            Records([item], levels),
            [new Exclusion(ExclusionKind.Level, "/Game/Test", "Test", "Invented test")]
        );
        var source = Assert.Single(result.Items[0].Sources);
        Assert.Equal("/Game/Live", Assert.Single(source.Places).Key);
    }

    [Fact]
    public void FailsWithTheStaleExclusionPath()
    {
        var error = Assert.Throws<InvalidDataException>(() =>
            Build(
                Records(),
                [new Exclusion(ExclusionKind.Level, "/Game/Missing", "Test", "Invented test")]
            )
        );
        Assert.Contains("/Game/Missing", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RepeatedPickupsKeepOneRelationPerPlace()
    {
        var item = Item();
        var actor = new ActorRecord("Sword", item.Path, new JsonObject());
        var result = Build(
            Records(
                [item],
                [
                    new LevelRecord("/Game/A", [], [actor, actor]),
                    new LevelRecord("/Game/B", [], [actor]),
                ]
            )
        );
        Assert.Equal(
            ["/Game/A", "/Game/B"],
            result.Items[0].Sources.Select(source => source.Entity.Key).Order()
        );
        Assert.All(
            result.Items[0].Sources,
            source => Assert.Equal(source.Entity, Assert.Single(source.Places))
        );
    }

    [Fact]
    public void CentralMergerDropsExcludedPlacesAndPreservesDistinctPrices()
    {
        Source Source(int price, params string[] levels) =>
            new(
                SourceKind.Shop,
                "Vendor",
                levels
                    .Select(path => new EntityReference(EntityKind.Place, path, path, null, null))
                    .ToArray(),
                new Price(price, null),
                null,
                [],
                []
            );
        var sources = CatalogContext.MergeSources(
            new[]
            {
                Source(1, "/Game/Test"),
                Source(2, "/Game/A", "/Game/Test"),
                Source(3, "/Game/A"),
            }.Select(source => new ItemSource("/Invented/Item.Item_C", source)),
            new HashSet<string>(["/Game/Test"], StringComparer.OrdinalIgnoreCase)
        );
        Assert.Equal(2, sources.Count);
        Assert.All(sources, source => Assert.Equal("/Game/A", Assert.Single(source.Places).Key));
        Assert.Equal([2, 3], sources.Select(source => source.Price!.Amount).Order());
    }

    [Fact]
    public void ListsNoSourceMissingIconUnknownFactAndUnreadPackage()
    {
        var item = Item();
        item.Properties["ItemInfo"]!.AsObject().Remove("ItemValue");
        var result = Build(
            Records(
                [item],
                gaps:
                [
                    new GapRecord("/Game/Broken", null, "Cannot parse"),
                    new GapRecord("/Game/Icons/Sword", "/Game/Icons/Sword.Sword", "Cannot decode"),
                ]
            )
        );
        Assert.Null(result.Items[0].Icon);
        Assert.Contains(result.Report.Gaps, gap => gap.Kind == GapKind.ItemWithoutSource);
        Assert.Contains(result.Report.Gaps, gap => gap.Kind == GapKind.ItemWithoutIcon);
        Assert.Contains(
            result.Report.Gaps.Single(gap => gap.Kind == GapKind.UnknownFact).Entries,
            entry => entry.Detail == "Value"
        );
        Assert.Contains(
            result.Report.Gaps.Single(gap => gap.Kind == GapKind.UnreadObject).Entries,
            entry =>
                entry.Label == "/Game/Broken"
                && entry.Detail!.Contains("Cannot parse", StringComparison.Ordinal)
        );
    }

    [Fact]
    public void UsesEntranceDisplayNamesAndReportsUnnamedPlaces()
    {
        var item = Item();
        var entrance = new ActorRecord(
            "Entrance",
            "/Game/Entrance.Entrance_C",
            Parse("""{"Location Name":{"$text":"Green Valley"},"Level to Load":"Valley_Map"}""")
        );
        var records = Records(
            [item],
            [
                new LevelRecord("/Game/Overworld", [], [entrance]),
                new LevelRecord(
                    "/Game/Valley_Map",
                    ["/Game/Valley_Caves"],
                    [new ActorRecord("Sword", item.Path, new JsonObject())]
                ),
                new LevelRecord("/Game/Valley_Caves", [], []),
            ]
        );
        var result = Build(records);
        Assert.Equal("Green Valley", result.Items[0].Sources[0].Places[0].Name);
        Assert.Equal(2, result.NamedPlaces);
        Assert.Single(
            result.Report.Gaps.Single(gap => gap.Kind == GapKind.PlaceWithoutName).Entries
        );
    }

    [Fact]
    public void NamesCampMapsPartsOfPlacesAndUnnamedLevelsAsPlayersKnowThem()
    {
        var entrance = new ActorRecord(
            "Entrance",
            "/Game/Entrance.Entrance_C",
            Parse(
                """{"Location Name":{"$text":"The Mage's College"},"Level to Load":"TheMagesCollege"}"""
            )
        );
        string[] levels =
        [
            "/Game/TheMagesCollege",
            "/Game/TheMagesCollege_MainHall",
            "/Game/Small_Event_Areas/Generic_GrassyPlains_02",
            "/Game/Small_Event_Areas/Generic_SnowPineArea_02",
            "/Game/CathedralOfArema",
        ];
        var records = Records(
            [Item()],
            [
                new LevelRecord("/Game/Overworld", [], [entrance]),
                .. levels.Select(level => new LevelRecord(level, [], [])),
            ]
        );
        var places = new Places(records, new HashSet<string>());
        Assert.Equal(
            [
                "The Mage's College",
                "The Mage's College: Main Hall",
                "Grassy Plains camp",
                "Snow Pine Area",
                "Cathedral of Arema",
            ],
            levels.Select(level => places.Get(level).Name)
        );
    }

    [Fact]
    public void ReportsEntitiesOfAKindWithoutPagesThatShareAName()
    {
        var item = Item();
        ActorRecord Entrance(string level) =>
            new(
                "Entrance",
                "/Game/Entrance.Entrance_C",
                Parse(
                    $$"""{"Location Name":{"$text":"Green Valley"},"Level to Load":"{{level}}"}"""
                )
            );
        var records = Records(
            [item],
            [
                new LevelRecord(
                    "/Game/Overworld",
                    [],
                    [Entrance("Valley_Map"), Entrance("Valley_North")]
                ),
                new LevelRecord(
                    "/Game/Valley_Map",
                    [],
                    [new ActorRecord("Sword", item.Path, new JsonObject())]
                ),
                new LevelRecord("/Game/Valley_North", [], []),
            ]
        );
        var entries = Build(records)
            .Report.Gaps.Single(gap => gap.Kind == GapKind.SharedName)
            .Entries;
        Assert.Equal(
            ["/Game/Valley_Map", "/Game/Valley_North"],
            entries.Select(entry => entry.Detail)
        );
        Assert.All(entries, entry => Assert.Equal("Place: Green Valley", entry.Label));
    }

    [Fact]
    public void FailedIconCopyLeavesPublishedSiteUntouched()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "bloodline-catalog-" + Guid.NewGuid().ToString("N")
        );
        try
        {
            var layout = new RecordLayout(directory, 1);
            Directory.CreateDirectory(layout.SiteDirectory);
            var sentinel = Path.Combine(layout.SiteDirectory, "existing.txt");
            File.WriteAllText(sentinel, "Previous site");
            Assert.Throws<DirectoryNotFoundException>(() =>
                CatalogWriter.Write(
                    layout,
                    new SiteBuild(1159290, 1, [], "abc", false, TestRelease),
                    Build(Records())
                )
            );
            Assert.Equal("Previous site", File.ReadAllText(sentinel));
            Assert.Single(Directory.EnumerateDirectories(layout.BuildDirectory));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void MissingTextureIndexEntryMakesTheIconUnknown()
    {
        var result = Build(Records() with { Textures = [] });
        Assert.Null(result.Items[0].Icon);
        Assert.Single(
            result.Report.Gaps.Single(gap => gap.Kind == GapKind.ItemWithoutIcon).Entries
        );
    }

    [Fact]
    public void EntranceTargetsUsePlacedOverridesAndInheritedDisplayNames()
    {
        var item = Item();
        var entranceClass = new ClassRecord(
            "/Game/Entrance.Entrance_C",
            [],
            Parse("""{"Location Name":{"$text":"Green Valley"},"Level to Load":"Unused"}""")
        );
        var entrance = new ActorRecord(
            "Entrance",
            entranceClass.Path,
            Parse("""{"Level to Load":"Valley_Map"}""")
        );
        var result = Build(
            Records(
                [item, entranceClass],
                [
                    new LevelRecord("/Game/Overworld", [], [entrance]),
                    new LevelRecord(
                        "/Game/Valley_Map",
                        [],
                        [new ActorRecord("Sword", item.Path, new JsonObject())]
                    ),
                ]
            )
        );
        Assert.Equal("Green Valley", Assert.Single(result.Items[0].Sources).Places[0].Name);
    }

    [Fact]
    public void SuccessfulOutputReplacementRemovesObsoleteItemPages()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "bloodline-catalog-" + Guid.NewGuid().ToString("N")
        );
        try
        {
            var layout = new RecordLayout(directory, 1);
            var oldPage = Path.Combine(layout.SiteDirectory, "items", "obsolete.json");
            CanonicalJson.Write(oldPage, new { name = "Obsolete item" });
            var catalog = Build(Records() with { Textures = [] });
            CatalogWriter.Write(
                layout,
                new SiteBuild(1159290, 1, [], "abc", false, TestRelease),
                catalog
            );
            Assert.False(File.Exists(oldPage));
            var itemFile = Assert.Single(
                Directory.EnumerateFiles(Path.Combine(layout.SiteDirectory, "items"))
            );
            Assert.Equal(catalog.Items[0].Key, CanonicalJson.Read<ItemPage>(itemFile).Key);
            Assert.Single(Directory.EnumerateDirectories(layout.BuildDirectory));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void UnrecordedPlacesRetainReadableNamesAndRemainVisibleAsUnknown()
    {
        var places = new Places(Records(), new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        var place = places.Get("/Game/HiddenValley");
        Assert.Equal("Hidden Valley", place.Name);
        Assert.False(places.HasName(place.Key!));
        Assert.Contains(place, places.All);
    }

    [Fact]
    public void DeduplicatesCoverageEntriesWithoutMergingDistinctAcquisitionData()
    {
        var item = Item();
        ClassRecord Enemy(string suffix, int weight) =>
            new(
                "/Game/Enemy" + suffix + ".Enemy" + suffix + "_C",
                [],
                Parse(
                    $$$"""
                    {"Enemy Name":{"$text":"Invented enemy"},"Loot Table":{"$map":[[{"$ref":"{{{item.Path}}}"},{{{weight}}}]]}}
                    """
                )
            );
        var gap = new GapRecord("/Game/Unread", null, "Invented error");
        var result = CatalogBuilder.Build(
            Records([item, Enemy("A", 1), Enemy("B", 2)], gaps: [gap, gap]),
            exclusions: [],
            mechanisms: [new EnemyDropMechanism()]
        );
        Assert.Equal(2, result.Items[0].Sources.Count);
        Assert.Single(
            result.Report.Gaps.Single(gap => gap.Kind == GapKind.SourceWithoutPlace).Entries
        );
        Assert.Single(result.Report.Gaps.Single(gap => gap.Kind == GapKind.UnreadObject).Entries);
    }
}
