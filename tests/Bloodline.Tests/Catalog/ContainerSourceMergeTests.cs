using Bloodline.Catalog;
using Bloodline.SiteData;

namespace Bloodline.Tests.Catalog;

public sealed class ContainerSourceMergeTests
{
    private static ItemSource Container(
        string level,
        double? appearance = null,
        double chance = 0.552,
        bool loose = false
    )
    {
        var details = new ContainerDetails(appearance, new Amount(1, 5), Loose: loose);
        return new ItemSource(
            "/Invented/Item.Item_C",
            new Source(
                loose ? SourceKind.Pickup : SourceKind.ContainerLoot,
                "Filled Barrels",
                [new EntityReference(EntityKind.Place, level, level, null, null)],
                null,
                new Amount(2, 2),
                [],
                []
            )
            {
                Odds = new Odds(
                    loose ? OddsEvent.LootSpot : OddsEvent.Container,
                    chance,
                    chance * 2,
                    null
                ),
            },
            details
        );
    }

    [Fact]
    public void CombinesOddsAndSpawnRangesAtTheSamePlace()
    {
        var a = Container("/Invented/A", 0.25);
        var b = Container("/Invented/A", null, 0.866);
        var merged = Assert.Single(CatalogContext.MergeSources([a, b], new HashSet<string>()));
        Assert.Equal(0.552, merged.Odds!.Chance);
        Assert.Equal(0.866, merged.Odds.HighestChance);
        Assert.Equal(1.732, merged.Odds.Expected);
        Assert.Equal(new ChanceRange(0.25, 1), merged.Spawn);
        Assert.Single(merged.Places);
        Assert.Empty(merged.Notes);
    }

    [Fact]
    public void KeepsDifferentRangesAtDifferentPlacesAndExcludesTestLevels()
    {
        var a = Container("/Invented/A", 0.25);
        var b = Container("/Invented/B", 0.75, 0.866);
        var excluded = Container("/Invented/Test", 0.01, 0.01);
        var sources = CatalogContext.MergeSources(
            [a, b, excluded],
            new HashSet<string>(["/Invented/Test"], StringComparer.OrdinalIgnoreCase)
        );
        Assert.Equal(2, sources.Count);
        Assert.Contains(sources, source => source.Spawn == new ChanceRange(0.25, 0.25));
        Assert.Contains(sources, source => source.Spawn == new ChanceRange(0.75, 0.75));
        Assert.DoesNotContain(
            sources,
            source => source.Places.Any(place => place.Key == "/Invented/Test")
        );
    }

    [Fact]
    public void RecombinesIdenticalRangesAcrossPlaces()
    {
        var sources = new[]
        {
            Container("/Invented/A", 0.25),
            Container("/Invented/A", null, 0.866),
            Container("/Invented/B", 0.25),
            Container("/Invented/B", 1, 0.866),
        };
        var merged = Assert.Single(CatalogContext.MergeSources(sources, new HashSet<string>()));
        Assert.Equal(
            new[] { "/Invented/A", "/Invented/B" },
            merged.Places.Select(place => place.Key)
        );
        Assert.Equal(new ChanceRange(0.25, 1), merged.Spawn);
        Assert.Equal(0.552, merged.Odds!.Chance);
        Assert.Equal(0.866, merged.Odds.HighestChance);
    }

    [Fact]
    public void MergesSameNamedContainersAcrossClassesWithAStableIdentity()
    {
        var a = Container("/Invented/A", 0.25);
        var b = Container("/Invented/A", null, 0.866);
        a = a with
        {
            Source = a.Source with
            {
                Entity = new EntityReference(
                    EntityKind.Container,
                    "/Invented/Z",
                    "Filled Barrels",
                    null,
                    null
                ),
            },
        };
        b = b with
        {
            Source = b.Source with { Entity = a.Source.Entity with { Key = "/Invented/A" } },
        };
        var atB = a with
        {
            Source = a.Source with
            {
                Entity = a.Source.Entity with { Key = "/Invented/B" },
                Places =
                [
                    new EntityReference(EntityKind.Place, "/Invented/B", "/Invented/B", null, null),
                ],
            },
        };
        var otherAtB = b with { Source = b.Source with { Places = atB.Source.Places } };
        var merged = Assert.Single(
            CatalogContext.MergeSources([a, b, atB, otherAtB], new HashSet<string>())
        );
        Assert.Equal("/Invented/A", merged.Entity.Key);
        Assert.Equal(2, merged.Places.Count);
        Assert.Equal(0.552, merged.Odds!.Chance);
        Assert.Equal(0.866, merged.Odds.HighestChance);
        Assert.Equal(new ChanceRange(0.25, 1), merged.Spawn);
    }

    [Fact]
    public void SplitsSourcesWithSeveralPlacesBeforeComputingRanges()
    {
        var a = Container("/Invented/A", null, 0.866);
        var b = Container("/Invented/B", 0.25);
        a = a with { Source = a.Source with { Places = [.. a.Source.Places, .. b.Source.Places] } };
        var sources = CatalogContext.MergeSources([a, b], new HashSet<string>());
        Assert.Equal(2, sources.Count);
        var atA = Assert.Single(sources, source => source.Places.Single().Key == "/Invented/A");
        Assert.Null(atA.Odds!.HighestChance);
        Assert.Null(atA.Spawn);
        var atB = Assert.Single(sources, source => source.Places.Single().Key == "/Invented/B");
        Assert.Equal(0.552, atB.Odds!.Chance);
        Assert.Equal(0.866, atB.Odds.HighestChance);
        Assert.Equal(new ChanceRange(0.25, 1), atB.Spawn);
    }

    [Fact]
    public void KeepsLooseLootApartFromRealContainersWithTheSameName()
    {
        var container = Container("/Invented/A", 0.25);
        var loose = Container("/Invented/A", null, loose: true);
        var sources = CatalogContext.MergeSources([container, loose], new HashSet<string>());
        Assert.Equal(2, sources.Count);
        var looseSource = Assert.Single(sources, source => source.Kind == SourceKind.Pickup);
        Assert.Empty(looseSource.Notes);
        Assert.Null(looseSource.Spawn);
        Assert.Equal(
            new ChanceRange(0.25, 0.25),
            Assert.Single(sources, source => source.Kind == SourceKind.ContainerLoot).Spawn
        );
    }

    [Fact]
    public void CombinesEquivalentLoosePickupsWithoutLosingTheirOddsOrPlaces()
    {
        var a = Container("/Invented/A", chance: 0.4, loose: true);
        var b = Container("/Invented/A", chance: 0.8, loose: true);
        var atB = Container("/Invented/B", chance: 0.4, loose: true);
        var otherAtB = Container("/Invented/B", chance: 0.8, loose: true);
        var merged = Assert.Single(
            CatalogContext.MergeSources([a, b, atB, otherAtB], new HashSet<string>())
        );
        Assert.Equal(SourceKind.Pickup, merged.Kind);
        Assert.Equal(OddsEvent.LootSpot, merged.Odds!.Event);
        Assert.Equal(0.4, merged.Odds.Chance);
        Assert.Equal(0.8, merged.Odds.HighestChance);
        Assert.Equal(1.6, merged.Odds.Expected);
        Assert.Equal(
            new[] { "/Invented/A", "/Invented/B" },
            merged.Places.Select(place => place.Key)
        );
        Assert.Empty(merged.Notes);
        Assert.Null(merged.Spawn);
    }

    [Fact]
    public void CombinesDifferentAmountsIntoTheirUnion()
    {
        var a = Container("/Invented/A");
        var b = a with { Source = a.Source with { Amount = new Amount(3, 5) } };
        var merged = Assert.Single(CatalogContext.MergeSources([a, b], new HashSet<string>()));
        Assert.Equal(new Amount(2, 5), merged.Amount);
    }

    [Theory]
    [InlineData("Price")]
    [InlineData("Inputs")]
    [InlineData("Conditions")]
    [InlineData("Name")]
    public void KeepsDifferentAcquisitionRequirementsApart(string difference)
    {
        var a = Container("/Invented/A");
        var changed = difference switch
        {
            "Price" => a.Source with { Price = new Price(5, null) },
            "Inputs" => a.Source with
            {
                Inputs =
                [
                    new ItemAmount(
                        new EntityReference(
                            EntityKind.Item,
                            "/Invented/Key.Key_C",
                            "Key",
                            "key",
                            null
                        ),
                        1
                    ),
                ],
            },
            "Conditions" => a.Source with
            {
                Conditions =
                [
                    new RelationCondition(ConditionKind.GameState, null, null, "At night."),
                ],
            },
            "Name" => a.Source with { Name = "Crates" },
            _ => throw new InvalidOperationException(),
        };
        var sources = CatalogContext.MergeSources(
            [a, a with { Source = changed }],
            new HashSet<string>()
        );
        Assert.Equal(2, sources.Count);
    }

    [Fact]
    public void PreservesUnknownOddsWhenOneVariantHasUnknownPicks()
    {
        var a = Container("/Invented/A");
        var unknown = a with
        {
            Source = a.Source with
            {
                Odds = LootOdds.Unknown(OddsEvent.Container, "Unknown picks."),
            },
        };
        var merged = Assert.Single(
            CatalogContext.MergeSources([a, unknown], new HashSet<string>())
        );
        Assert.Null(merged.Odds!.Chance);
        Assert.Null(merged.Odds.HighestChance);
        Assert.Equal("Unknown picks.", merged.Odds.Unknown);
        Assert.Equal(a.Source.Odds!.Expected, merged.Odds.Expected);
    }

    [Fact]
    public void RetainsOtherContainerNotesAlongsideTheCombinedRange()
    {
        var a = Container("/Invented/A");
        var b = Container("/Invented/B");
        const string keyNote = "Requires the silver key.";
        const string nightNote = "Available at night.";
        a = a with { Source = a.Source with { Notes = [keyNote] } };
        b = b with { Source = b.Source with { Notes = [nightNote] } };
        var source = Assert.Single(CatalogContext.MergeSources([a, b], new HashSet<string>()));
        Assert.Contains("Some containers: " + keyNote, source.Notes);
        Assert.Contains("Some containers: " + nightNote, source.Notes);
    }

    [Fact]
    public void QualifiesPartialNotesOnlyOnceWhenRecombiningPlaces()
    {
        var a = Container("/Invented/A");
        var noted = a with { Source = a.Source with { Notes = ["Requires a key."] } };
        var b = Container("/Invented/B");
        var source = Assert.Single(
            CatalogContext.MergeSources([a, noted, b], new HashSet<string>())
        );
        Assert.Equal("Some containers: Requires a key.", Assert.Single(source.Notes));
    }

    [Fact]
    public void ALocatedContainerDoesNotHideAnIdenticalUnlocatedVariant()
    {
        var located = Container("/Invented/A");
        var unlocated = located with { Source = located.Source with { Places = [] } };
        var source = Assert.Single(
            CatalogContext.MergeSources([located, unlocated], new HashSet<string>())
        );
        Assert.Single(source.Places);
        Assert.Contains("Some containers have no known location.", source.Notes);
    }

    [Fact]
    public void KeepsAnUnlocatedVariantWithDifferentOddsSeparate()
    {
        var located = Container("/Invented/A");
        var unlocated = Container("/Invented/A", 0.25, 0.866);
        unlocated = unlocated with { Source = unlocated.Source with { Places = [] } };
        var sources = CatalogContext.MergeSources([located, unlocated], new HashSet<string>());
        Assert.Equal(2, sources.Count);
        Assert.Single(sources, source => source.Places.Count == 0);
    }
}
