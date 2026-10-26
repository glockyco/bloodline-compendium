using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal static class EntityKinds
{
    public static readonly IReadOnlyList<EntityKindDefinition> All =
    [
        new(EntityKind.Item, true, "items", "Items", "Thing"),
        new(EntityKind.Merchant, false, null, "Merchants", "Person"),
        new(EntityKind.Person, false, null, "People", "Person"),
        new(EntityKind.Creature, false, null, "Creatures", "Thing"),
        new(EntityKind.Place, false, null, "Places", "Place"),
        new(EntityKind.Container, false, null, "Containers", "Thing"),
        new(EntityKind.Recipe, false, null, "Recipes", "Recipe"),
        new(EntityKind.Building, false, null, "Buildings", "Place"),
        new(EntityKind.CalendarEvent, false, null, "Calendar events", "Event"),
        new(EntityKind.Quest, false, null, "Quests", "Thing"),
        new(EntityKind.Resource, false, null, "Resources", "Thing"),
        new(EntityKind.Activity, false, null, "Activities", "Thing"),
    ];
}
