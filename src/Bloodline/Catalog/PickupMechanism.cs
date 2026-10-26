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
                        null,
                        null,
                        [],
                        []
                    )
                    {
                        Entity = context.Places.Get(placement.Level),
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
