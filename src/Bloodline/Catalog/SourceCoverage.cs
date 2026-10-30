using System.Collections.Frozen;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal static class SourceCoverage
{
    // Recipes and global pools have no location by nature. Only place-bound sources can lack one.
    private static readonly FrozenSet<SourceKind> PlaceBound = new[]
    {
        SourceKind.Shop,
        SourceKind.CurrencyShop,
        SourceKind.ContainerLoot,
        SourceKind.EnemyDrop,
        SourceKind.Pickup,
        SourceKind.Dialogue,
        SourceKind.Fishing,
        SourceKind.Gathering,
    }.ToFrozenSet();

    // A source in the player's settlement has its place, which no level holds.
    public static bool HasUnknownPlace(Source source) =>
        source.Places.Count == 0 && !source.Settlement && PlaceBound.Contains(source.Kind);
}
