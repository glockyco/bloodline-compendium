using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

/// <summary>
/// The Christmas presents in Whitebrook Haven and Thronhild, read with <c>bloodline script</c>. A present opens when
/// the player holds at least its <c>Required Cookies</c>, takes that many from the cookies held and gives its
/// <c>Gift Class</c>. Cookies come from the 100 cookie pickups in Whitebrook Haven, each adding one, and a present
/// opens once per character, because both towns' presents of a gift share one completion tag.
/// </summary>
internal sealed class ChristmasPresentMechanism : ISourceMechanism
{
    private const string List = "ChristmasPresentMechanism";
    private const string Present = "/Game/Christmas_Assets/ChristmasPresent_01";
    private const string Cookie = "/Game/Christmas_Assets/Cookie_Pickup";

    public IEnumerable<EntitySource> FindSources(CatalogContext context)
    {
        if (
            ReadFunction.Find(
                context,
                Present + ":ExecuteUbergraph_ChristmasPresent_01",
                "396c34f7dc6c",
                List
            )
                is null
            || ReadFunction.Find(
                context,
                Cookie + ":ExecuteUbergraph_Cookie_Pickup",
                "63b5ca0b874c",
                List
            )
                is null
        )
            yield break;
        var cookiePlaces = context
            .Placements.Where(placement => placement.Actor.Class.Split('.')[0] == Cookie)
            .Select(placement => context.Places.Get(placement.Level).Name)
            .Distinct()
            .ToArray();
        if (cookiePlaces.Length != 1)
            throw new InvalidDataException(
                $"Christmas cookies lie in {cookiePlaces.Length} areas, and the presents' text names one. Update {List}."
            );
        foreach (var placement in context.Placements)
        {
            if (placement.Actor.Class.Split('.')[0] != Present)
                continue;
            if (
                RecordValues.AnyReference(placement.Properties["Gift Class"]) is not { } gift
                || RecordValues.Integer(placement.Properties["Required Cookies"]) is not { } cookies
            )
                continue;
            yield return new EntitySource(
                gift,
                new Source(
                    SourceKind.SeasonalGift,
                    "Christmas Present",
                    [context.Places.Get(placement.Level)],
                    null,
                    new Amount(1, 1),
                    [],
                    []
                )
                {
                    Entity = WorldSourceData.Reference(
                        EntityKind.Container,
                        Present + ".ChristmasPresent_01_C",
                        "Christmas Present"
                    ),
                    Triggers =
                    [
                        FormattableString.Invariant(
                            $"Open it with {cookies} Christmas cookies, which you collect in {cookiePlaces[0]}."
                        ),
                    ],
                }
            );
        }
    }
}
