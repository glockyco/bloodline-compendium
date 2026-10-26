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
                [],
                details.Notes
            )
            {
                Odds = details.Picks is { } picks
                    ? LootOdds.Of(OddsEvent.Container, 0.25, picks)
                    : LootOdds.Unknown(OddsEvent.Container, "Unknown."),
            },
            details
        );
    }

    [Fact]
    public void CombinesAppearanceRangesOfContainersWithTheSameOddsAfterExclusions()
    {
        var a = Container("/Invented/A", 0.25, new Amount(1, 5));
        var b = Container("/Invented/B", 0.75, new Amount(1, 5));
        var excluded = Container("/Invented/Test", 0.01, new Amount(1, 5));
        var merged = Assert.Single(
            CatalogContext.MergeSources(
                [a, b, excluded],
                new HashSet<string>(["/Invented/Test"], StringComparer.OrdinalIgnoreCase)
            )
        );
        Assert.Equal(a.Source.Odds, merged.Odds);
        Assert.Equal(
            new[] { "/Invented/A", "/Invented/B" },
            merged.Places.Select(place => place.Key)
        );
        var numbers = Regex
            .Matches(string.Join(" ", merged.Notes), @"\d+(?:\.\d+)?")
            .Select(match => double.Parse(match.Value, CultureInfo.InvariantCulture))
            .Order();
        Assert.Equal(new[] { 25d, 75d }, numbers);
    }

    [Theory]
    [InlineData("Picks")]
    [InlineData("Unknown picks")]
    [InlineData("Amount")]
    [InlineData("Price")]
    [InlineData("Inputs")]
    public void KeepsDifferentAcquisitionValuesApart(string difference)
    {
        var a = Container("/Invented/A", 1, new Amount(1, 5));
        var changed = difference switch
        {
            "Picks" => Container("/Invented/A", 1, new Amount(2, 5)).Source,
            "Unknown picks" => Container("/Invented/A", 1, null).Source,
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
                && source.Odds == changed.Odds
                && source.Price == changed.Price
                && source.Inputs.SequenceEqual(changed.Inputs)
        );
    }

    [Fact]
    public void RetainsOtherContainerConditionsAlongsideTheCombinedRange()
    {
        var a = Container("/Invented/A", 1, new Amount(1, 5));
        var b = Container("/Invented/B", 1, new Amount(1, 5));
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
}
