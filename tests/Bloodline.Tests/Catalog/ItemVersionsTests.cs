using Bloodline.Catalog;
using Bloodline.Output;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Tests.Catalog;

public sealed class ItemVersionsTests
{
    private static ItemPage Item(string id, params Fact[] facts) =>
        new(
            $"/Invented/{id}.{id}_C",
            "shared-name",
            "Shared Name",
            "An invented item.",
            new CategoryLink("weapon", "Weapon"),
            "same-icon",
            facts,
            []
        )
        {
            Type = "Sword",
        };

    private static Source Source(SourceKind kind, string name) =>
        new(kind, name, [], null, new Amount(1, 1), [], [])
        {
            Entity = new EntityReference(
                EntityKind.Activity,
                $"/Invented/{name}",
                name,
                null,
                null
            ),
        };

    [Fact]
    public void IdenticalCopiesMergeAllSourcesUsesAndIssueNotesWithStableCanonicalKey()
    {
        var a = Item("A", StatVocabulary.Number(Stat.Damage, 12)) with
        {
            Sources = [Source(SourceKind.Shop, "Trader")],
            Notes = [new IssueNote("first", "First effect")],
        };
        var b = a with
        {
            Key = Item("B").Key,
            Sources = [Source(SourceKind.Dialogue, "Person")],
            Notes = [new IssueNote("second", "Second effect")],
            Uses = [new ItemUse(SourceKind.Crafting, "Recipe", CatalogGraph.Reference(a), 1)],
        };
        var grouped = ItemVersions.Group([b, a]);
        var item = Assert.Single(grouped.Items);
        Assert.Equal(a.Key, item.Key);
        Assert.Equal(2, item.Sources.Count);
        Assert.Equal(2, item.Notes.Count);
        Assert.Single(item.Uses);
        Assert.Equal(grouped.References[a.Key], grouped.References[b.Key]);
        Assert.Null(item.VersionId);
        Assert.Null(item.VersionLabel);
        Assert.Null(item.VersionName);
        Assert.Empty(item.Versions);
    }

    [Fact]
    public void SharedPageKeepsRootFirstByKeyAndDoesNotNestVersions()
    {
        var grouped = ItemVersions.Group([
            Item("B", StatVocabulary.Number(Stat.Damage, 24)),
            Item("A", StatVocabulary.Number(Stat.Damage, 12)),
        ]);
        Assert.Equal(2, grouped.Items.Count);
        var page = Assert.Single(ItemVersions.Pages(grouped.Items));
        Assert.Equal("a", page.VersionId);
        Assert.Equal("12 Damage", page.VersionLabel);
        Assert.Equal("b", Assert.Single(page.Versions).VersionId);
        Assert.Empty(page.Versions[0].Versions);
        Assert.All(
            grouped.References.Values,
            reference => Assert.Equal("shared-name", reference.Slug)
        );
        Assert.Equal("a", grouped.References[page.Key].Fragment);
    }

    [Theory]
    [InlineData("BodyHitDamage", 125, 50, "125 Damage", "50 Damage")]
    [InlineData("ArmorRating", 12, 4, "12 Armor rating", "4 Armor rating")]
    [InlineData("DamageContribution", 24, 23, "+24 Damage", "+23 Damage")]
    [InlineData("SpeedContribution", 0.2, 0, "+0.2 Speed", "+0 Speed")]
    public void LabelsUseTheFirstDifferingAppliedStat(
        string statName,
        double first,
        double second,
        string firstLabel,
        string secondLabel
    )
    {
        var stat = Enum.Parse<Stat>(statName);
        var grouped = ItemVersions.Group([
            Item("A", StatVocabulary.Number(Stat.Value, 500), StatVocabulary.Number(stat, first)),
            Item("B", StatVocabulary.Number(Stat.Value, 100), StatVocabulary.Number(stat, second)),
        ]);
        Assert.Equal([firstLabel, secondLabel], grouped.Items.Select(item => item.VersionLabel));
    }

    [Theory]
    [InlineData("ArmorType", "Custom", "Heavy", "Custom Armor", "Heavy Armor")]
    [InlineData("ArmorType", "Medium", "Unarmored", "Medium Armor", "Unarmored")]
    [InlineData("Speed", "Above Average", "Standard", "Above Average Speed", "Standard Speed")]
    public void TextStatsUseApprovedLabels(
        string statName,
        string first,
        string second,
        string firstLabel,
        string secondLabel
    )
    {
        var stat = Enum.Parse<Stat>(statName);
        var grouped = ItemVersions.Group([
            Item("A", StatVocabulary.Text(stat, first)),
            Item("B", StatVocabulary.Text(stat, second)),
        ]);
        Assert.Equal([firstLabel, secondLabel], grouped.Items.Select(item => item.VersionLabel));
    }

    [Fact]
    public void TypePrecedesStatsAndStatsPrecedeIcons()
    {
        var a = Item("A", StatVocabulary.Number(Stat.Damage, 12));
        var b = Item("B", StatVocabulary.Number(Stat.Damage, 24)) with { Icon = "another" };
        Assert.Equal("12 Damage", ItemVersions.Group([a, b]).Items[0].VersionLabel);
        b = b with { Type = "Note" };
        Assert.Equal(
            ["Sword", "Note"],
            ItemVersions.Group([a, b]).Items.Select(item => item.VersionLabel)
        );
    }

    [Fact]
    public void IconOnlyTabsHaveDistinctDescriptionNamesAndNoVisibleLabel()
    {
        var grouped = ItemVersions.Group([
            Item("A") with
            {
                Description = "An amber orb. More words.",
            },
            Item("B") with
            {
                Icon = "another",
                Description = "An azure orb. Other words.",
            },
        ]);
        Assert.All(grouped.Items, item => Assert.Null(item.VersionLabel));
        Assert.Equal(
            ["An amber orb", "An azure orb"],
            grouped.Items.Select(item => item.VersionName)
        );
    }

    [Theory]
    [InlineData("Shop", "Armorer", "Sold by Armorer")]
    [InlineData("ContainerLoot", "Chest", "Found in containers")]
    [InlineData("RandomLoot", "General random loot", "Random loot")]
    [InlineData("Dialogue", "Father Linn", "Given by Father Linn")]
    [InlineData("BountyNote", "Farmstead Invaders", "From the bounty Farmstead Invaders")]
    [InlineData("Smithing", "Terrashard Wand Core", "Smithed as Terrashard Wand Core")]
    public void DescriptionsCanDifferWhileSourcesLabelVersions(
        string kindName,
        string name,
        string label
    )
    {
        var kind = Enum.Parse<SourceKind>(kindName);
        var grouped = ItemVersions.Group([
            Item("A") with
            {
                Sources = [Source(kind, name)],
            },
            Item("B") with
            {
                Description = "A different description.",
            },
        ]);
        Assert.Equal([label, "No known source"], grouped.Items.Select(item => item.VersionLabel));
    }

    [Fact]
    public void FirstSourceLabelsVersionsEvenWhenItsAvailabilityIsUnconfirmed()
    {
        var grouped = ItemVersions.Group([
            Item("A") with
            {
                Sources = [Source(SourceKind.Dialogue, "Father Linn") with { Unconfirmed = true }],
            },
            Item("B") with
            {
                Description = "Other.",
            },
        ]);
        Assert.Equal(
            ["Given by Father Linn", "No known source"],
            grouped.Items.Select(item => item.VersionLabel)
        );
    }

    [Fact]
    public void PickupUsesPlaceAndSameKindSourcesUseTheirNames()
    {
        var pickup = Source(SourceKind.Pickup, "World pickup") with
        {
            Places = [new EntityReference(EntityKind.Place, "/Woodhurst", "Woodhurst", null, null)],
        };
        Assert.Equal(
            "Picked up in Woodhurst",
            ItemVersions
                .Group([
                    Item("A") with
                    {
                        Sources = [pickup],
                    },
                    Item("B") with
                    {
                        Description = "Other.",
                    },
                ])
                .Items[0]
                .VersionLabel
        );
        Assert.Equal(
            ["Sold by Armorer", "Sold by Ironstich Merchant"],
            ItemVersions
                .Group([
                    Item("A") with
                    {
                        Sources = [Source(SourceKind.Shop, "Armorer")],
                    },
                    Item("B") with
                    {
                        Description = "Other.",
                        Sources = [Source(SourceKind.Shop, "Ironstich Merchant")],
                    },
                ])
                .Items.Select(item => item.VersionLabel)
        );
    }

    [Fact]
    public void ValueIsLastAndUnlabelledDistinctVersionsFailTheCatalog()
    {
        Assert.Equal(
            ["Worth 950 gold", "Worth 750 gold"],
            ItemVersions
                .Group([
                    Item("A", StatVocabulary.Number(Stat.Value, 950)),
                    Item("B", StatVocabulary.Number(Stat.Value, 750)),
                ])
                .Items.Select(item => item.VersionLabel)
        );
        Assert.Throws<InvalidDataException>(() =>
            ItemVersions.Group([Item("A"), Item("B") with { Description = "Other." }])
        );
    }

    [Theory]
    [InlineData("category")]
    [InlineData("on-hit")]
    [InlineData("projectile")]
    [InlineData("tooltip")]
    public void PlayerVisibleDifferencesDoNotMerge(string difference)
    {
        var a = Item("A", StatVocabulary.Number(Stat.Damage, 12)) with
        {
            Sources = [Source(SourceKind.Shop, "First trader")],
        };
        var b = a with
        {
            Key = Item("B").Key,
            Sources = [Source(SourceKind.Shop, "Second trader")],
        };
        b = difference switch
        {
            "category" => b with { Category = new CategoryLink("material", "Material") },
            "on-hit" => b with
            {
                OnHitEffects = [new OnHitEffect("Fire", "Hit", 1, 5, null, null, null)],
            },
            "projectile" => b with { ProjectileGroups = [new ProjectileGroup(2, 12, 24, "bolt")] },
            "tooltip" => b with
            {
                Facts = [b.Facts[0] with { GameShows = "24", GameShowsIssue = "invented" }],
            },
            _ => throw new InvalidOperationException(),
        };
        Assert.Equal(2, ItemVersions.Group([a, b]).Items.Count);
    }

    [Fact]
    public void EveryLabelMustBeUniqueAcrossTheWholeGroup()
    {
        Assert.Throws<InvalidDataException>(() =>
            ItemVersions.Group([
                Item("A", StatVocabulary.Number(Stat.Damage, 12)),
                Item("B", StatVocabulary.Number(Stat.Damage, 24)),
                Item("C", StatVocabulary.Number(Stat.Damage, 24)) with
                {
                    Description = "Other.",
                },
            ])
        );
    }

    [Fact]
    public void EveryOriginalClassReferenceResolvesToItsCanonicalVersionInTheGraph()
    {
        var a = Item("A", StatVocabulary.Number(Stat.Damage, 12));
        var duplicate = a with { Key = Item("Duplicate").Key };
        var b = Item("B", StatVocabulary.Number(Stat.Damage, 24));
        var output = Item("Output") with
        {
            Slug = "output",
            Name = "Output",
            Sources =
            [
                Source(SourceKind.Crafting, "Recipe") with
                {
                    Inputs = [new ItemAmount(CatalogGraph.Reference(duplicate), 2)],
                    Price = new Price(3, CatalogGraph.Reference(b)),
                    Conditions =
                    [
                        new RelationCondition(
                            ConditionKind.GameState,
                            CatalogGraph.Reference(b),
                            null,
                            "Ready"
                        ),
                    ],
                },
            ],
        };
        var grouped = ItemVersions.Group([duplicate, output, b, a]);
        var graph = CatalogGraph.Build(grouped.Items, itemReferences: grouped.References);
        var source = Assert.Single(graph.Items.Single(item => item.Key == output.Key).Sources);
        Assert.Equal(a.Key, source.Inputs[0].Item.Key);
        Assert.Equal("a", source.Inputs[0].Item.Fragment);
        Assert.Equal("b", source.Price!.Currency!.Fragment);
        Assert.Equal("b", source.Conditions[0].Entity!.Fragment);
        Assert.Single(graph.Items.Single(item => item.Key == a.Key).Uses);
        Assert.Single(graph.Items.Single(item => item.Key == b.Key).Uses);
        Assert.DoesNotContain(graph.Graph.Entities, entity => entity.Entity.Key == duplicate.Key);
    }

    [Fact]
    public void WriterPublishesOneJsonPerNameWithAllVersions()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var items = ItemVersions
                .Group([
                    Item("B", StatVocabulary.Number(Stat.Damage, 24)) with
                    {
                        Icon = null,
                    },
                    Item("A", StatVocabulary.Number(Stat.Damage, 12)) with
                    {
                        Icon = null,
                    },
                ])
                .Items;
            var layout = new RecordLayout(directory, 1);
            var result = new CatalogResult(
                new ItemIndex([], []),
                items,
                new MaintainerReport([], []),
                0,
                new EntityGraph([], [])
            );
            CatalogWriter.Write(
                layout,
                new SiteBuild(
                    1,
                    1,
                    [],
                    "invented",
                    false,
                    new SiteRelease("Invented", "2026-01-01", "https://example.com", "2026-01-01")
                ),
                result
            );
            var file = Assert.Single(
                Directory.GetFiles(Path.Combine(layout.SiteDirectory, "items"))
            );
            var page = CanonicalJson.Read<ItemPage>(file);
            Assert.Equal("a", page.VersionId);
            Assert.Equal("b", Assert.Single(page.Versions).VersionId);
            Assert.Empty(page.Versions[0].Versions);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData(typeof(SiteDataFiles))]
    [InlineData(typeof(ItemPage))]
    public void SchemaUsesANamedReferenceForRecursiveVersions(Type type)
    {
        var schema = SiteDataSchema.Export(type);
        var page = schema["$defs"]!["ItemPage"]!;
        Assert.Equal(
            "#/$defs/ItemPage",
            page["properties"]!["versions"]!["items"]!["$ref"]!.GetValue<string>()
        );
    }
}
