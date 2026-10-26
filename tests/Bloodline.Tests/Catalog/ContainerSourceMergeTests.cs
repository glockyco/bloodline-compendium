using System.Globalization;
using System.Text.RegularExpressions;
using Bloodline.Catalog;
using Bloodline.SiteData;

namespace Bloodline.Tests.Catalog;

public sealed class ContainerSourceMergeTests
{
    private static ItemSource Container(string level, double? appearance, Amount? limits)
    {
        var details = new ContainerDetails(appearance, limits);
        return new ItemSource(
            "/Invented/Item.Item_C",
            new Source(
                SourceKind.ContainerLoot,
                "Filled Barrels",
                [new EntityReference(EntityKind.Place, level, level, null, null)],
                null,
                new Amount(2, 2),
                new Weight(0.5, 18.02),
                null,
                [],
                details.Notes
            )
            {
                ChanceBasis = "Chance per pick",
                Picks = details.Picks,
            },
            details
        );
    }

    [Fact]
    public void CombinesCountAndAppearanceRangesAcrossPlacesAfterExclusions()
    {
        var a = Container("/Invented/A", 0.25, new Amount(1, 5));
        var b = Container("/Invented/B", 0.75, new Amount(3, 8));
        var excluded = Container("/Invented/Test", 0.01, new Amount(0, 100));
        var merged = Assert.Single(
            CatalogContext.MergeSources(
                [a, b, excluded],
                new HashSet<string>(["/Invented/Test"], StringComparer.OrdinalIgnoreCase)
            )
        );
        Assert.Equal(new Amount(2, 2), merged.Amount);
        Assert.Equal(new Weight(0.5, 18.02), merged.Weight);
        Assert.Equal(new Amount(2, 9), merged.Picks);
        Assert.Equal("Chance per pick", merged.ChanceBasis);
        Assert.Equal(
            new[] { "/Invented/A", "/Invented/B" },
            merged.Places.Select(place => place.Key)
        );
        var numbers = Regex
            .Matches(string.Join(" ", merged.Notes), @"\d+(?:\.\d+)?")
            .Select(match => double.Parse(match.Value, CultureInfo.InvariantCulture))
            .Order();
        Assert.Equal(new[] { 2d, 9d, 25d, 75d }, numbers);
    }

    [Theory]
    [InlineData("Weight")]
    [InlineData("Weight total")]
    [InlineData("Chance")]
    [InlineData("Chance basis")]
    [InlineData("Amount")]
    [InlineData("Price")]
    [InlineData("Inputs")]
    public void KeepsDifferentAcquisitionValuesApart(string difference)
    {
        var a = Container("/Invented/A", 1, new Amount(1, 5));
        var changed = difference switch
        {
            "Weight" => a.Source with { Weight = new Weight(1, 18.02) },
            "Weight total" => a.Source with { Weight = new Weight(0.5, 20) },
            "Chance" => a.Source with { Chance = 0.2 },
            "Chance basis" => a.Source with { ChanceBasis = "Chance per roll" },
            "Amount" => a.Source with { Amount = new Amount(3, 3) },
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
            _ => throw new InvalidOperationException(),
        };
        var b = a with { Source = changed };
        var sources = CatalogContext.MergeSources([a, b], new HashSet<string>());
        Assert.Equal(2, sources.Count);
        Assert.Contains(
            sources,
            source =>
                source.Amount == changed.Amount
                && source.Weight == changed.Weight
                && source.Chance == changed.Chance
                && source.ChanceBasis == changed.ChanceBasis
                && source.Price == changed.Price
                && source.Inputs.SequenceEqual(changed.Inputs)
        );
    }

    [Fact]
    public void RetainsOtherContainerConditionsAlongsideTheCombinedRange()
    {
        var a = Container("/Invented/A", 1, new Amount(1, 5));
        var b = Container("/Invented/B", 1, new Amount(2, 8));
        const string keyCondition = "Requires the silver key.";
        const string nightCondition = "Available at night.";
        a = a with { Source = a.Source with { Notes = [.. a.Source.Notes, keyCondition] } };
        b = b with { Source = b.Source with { Notes = [.. b.Source.Notes, nightCondition] } };
        var source = Assert.Single(CatalogContext.MergeSources([a, b], new HashSet<string>()));
        Assert.Contains(
            source.Notes,
            note => note.Contains(keyCondition, StringComparison.Ordinal)
        );
        Assert.Contains(
            source.Notes,
            note => note.Contains(nightCondition, StringComparison.Ordinal)
        );
    }

    [Fact]
    public void CombinesLooseLootBoundsWithoutTurningTheNominalMinimumIntoAGuarantee()
    {
        ItemSource Loose(string level, int min, int max)
        {
            var candidate = Container(level, null, null);
            var details = new ContainerDetails(null, new Amount(min, max), Loose: true);
            return candidate with
            {
                Container = details,
                Source = candidate.Source with { Notes = details.Notes, Picks = details.Picks },
            };
        }
        var source = Assert.Single(
            CatalogContext.MergeSources(
                [Loose("/Invented/A", 1, 5), Loose("/Invented/B", 3, 8)],
                new HashSet<string>()
            )
        );
        var numbers = Regex
            .Matches(string.Join(" ", source.Notes), @"\d+(?:\.\d+)?")
            .Select(match => double.Parse(match.Value, CultureInfo.InvariantCulture))
            .Order();
        Assert.Equal(new[] { 5d, 8d }, numbers);
        Assert.Equal(2, source.Places.Count);
        Assert.Equal(new Amount(5, 8), source.Picks);
    }

    [Fact]
    public void ALocatedContainerDoesNotHideAnUnlocatedVariant()
    {
        var located = Container("/Invented/A", 1, new Amount(1, 5));
        var unlocated = located with { Source = located.Source with { Places = [] } };
        var source = Assert.Single(
            CatalogContext.MergeSources([located, unlocated], new HashSet<string>())
        );
        Assert.Single(source.Places);
        Assert.Contains(
            source.Notes,
            note =>
                note.Contains("location", StringComparison.OrdinalIgnoreCase)
                && note.Contains("no known", StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public void UnknownPickCountIsNotReplacedByKnownVariantBounds()
    {
        var known = Container("/Invented/A", 1, new Amount(1, 5));
        var unknown = Container("/Invented/B", 1, null);
        var source = Assert.Single(
            CatalogContext.MergeSources([known, unknown], new HashSet<string>())
        );
        Assert.Null(source.Picks);
        Assert.Equal(2, source.Places.Count);
        Assert.Contains(source.Notes, note => note.Contains("unknown", StringComparison.Ordinal));
    }
}
