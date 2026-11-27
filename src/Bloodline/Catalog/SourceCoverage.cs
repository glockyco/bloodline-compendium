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

    // A settlement or a named event gives a route even if there is no fixed place.
    public static bool HasUnknownPlace(Source source) =>
        source.Places.Count == 0
        && !source.Settlement
        && source.Events.Count == 0
        && PlaceBound.Contains(source.Kind);
}
