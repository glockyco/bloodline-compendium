using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed class PickupMechanism : ISourceMechanism
{
    public IEnumerable<ItemSource> FindSources(CatalogContext context)
    {
        foreach (var placement in context.Placements)
        {
            if (context.Items.ContainsKey(placement.Actor.Class))
            {
                yield return new ItemSource(
                    placement.Actor.Class,
                    new Source(
                        SourceKind.Pickup,
                        "World pickup",
                        [context.Places.Get(placement.Level)],
                        null,
                        null,
                        [],
                        []
                    )
                    {
                        Entity = context.Places.Get(placement.Level),
                        // An item placed in a level as itself is there whenever the level loads.
                        Odds = new Odds(OddsEvent.LootSpot, 1, 1, null),
                        Conditions = WorldSourceData.Conditions(
                            context,
                            placement.Properties,
                            placement.Level
                        ),
                    }
                );
            }
        }
    }
}
