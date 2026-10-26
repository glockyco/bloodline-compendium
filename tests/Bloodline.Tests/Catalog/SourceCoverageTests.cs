using Bloodline.Catalog;
using Bloodline.SiteData;

namespace Bloodline.Tests.Catalog;

public sealed class SourceCoverageTests
{
    [Theory]
    [InlineData((int)SourceKind.Shop)]
    [InlineData((int)SourceKind.CurrencyShop)]
    [InlineData((int)SourceKind.ContainerLoot)]
    [InlineData((int)SourceKind.EnemyDrop)]
    [InlineData((int)SourceKind.Pickup)]
    [InlineData((int)SourceKind.Dialogue)]
    [InlineData((int)SourceKind.Fishing)]
    [InlineData((int)SourceKind.Gathering)]
    public void PlaceBoundSourcesOnlyHaveAPlaceGapWhenUnplaced(int kind)
    {
        var source = new Source(
            (SourceKind)kind,
            "Invented source",
            [],
            null,
            null,
            null,
            null,
            [],
            []
        );
        Assert.True(SourceCoverage.HasUnknownPlace(source));
        Assert.False(
            SourceCoverage.HasUnknownPlace(
                source with
                {
                    Places =
                    [
                        new EntityReference(EntityKind.Place, "/Game/Valley", "Valley", null, null),
                    ],
                }
            )
        );
    }

    [Theory]
    [InlineData((int)SourceKind.Smithing)]
    [InlineData((int)SourceKind.Crafting)]
    [InlineData((int)SourceKind.Cooking)]
    [InlineData((int)SourceKind.Farming)]
    [InlineData((int)SourceKind.RandomLoot)]
    [InlineData((int)SourceKind.BountyNote)]
    [InlineData((int)SourceKind.SettlementDelivery)]
    public void LocationIndependentSourcesDoNotClaimAnUnknownPlace(int kind)
    {
        Assert.False(
            SourceCoverage.HasUnknownPlace(
                new Source((SourceKind)kind, "Invented source", [], null, null, null, null, [], [])
            )
        );
    }
}
