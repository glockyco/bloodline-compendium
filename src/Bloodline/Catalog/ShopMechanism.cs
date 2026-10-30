using System.Text.Json.Nodes;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed class ShopMechanism : ISourceMechanism
{
    public IEnumerable<ItemSource> FindSources(CatalogContext context)
    {
        var shops = context
            .Records.Tables.Where(table => ItemIdentity.ClassName(table.Path) == "DT_AllShops")
            .SelectMany(table => table.Rows)
            .ToDictionary(row => row.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var placement in context.Placements)
        {
            var name = VendorName(placement.Properties, placement.Actor.Class);
            var building = placement.Actor.Class.Contains(
                "/Construction_Buildings/Buildings_Finished/",
                StringComparison.OrdinalIgnoreCase
            );
            var entity = WorldSourceData.PlacementEntity(
                context,
                placement,
                building ? EntityKind.Building : EntityKind.Merchant,
                name
            );
            IReadOnlyList<RelationCondition> conditions = building
                ?
                [
                    .. WorldSourceData.Conditions(context, placement.Properties, placement.Level),
                    WorldSourceData.BuildingCondition(entity),
                ]
                : WorldSourceData.Conditions(context, placement.Properties, placement.Level);
            foreach (var field in new[] { "Shop Contents", "Vendor Contents" })
            {
                foreach (
                    var item in Stock(placement.Properties[field])
                        .Where(item => !building || Offered(context, item))
                )
                {
                    yield return Sale(
                        context,
                        item,
                        entity,
                        [context.Places.Get(placement.Level)],
                        conditions
                    );
                }
            }
        }
        // Settlement buildings create their vendors at runtime, so a missing actor placement is not absence of stock.
        foreach (
            var building in context.Records.Classes.Where(type =>
                type.Path.Contains(
                    "/Construction_Buildings/Buildings_Finished/",
                    StringComparison.OrdinalIgnoreCase
                )
                && !context.IsExcludedClass(type.Path)
                && !context.Placements.Any(placement =>
                    string.Equals(
                        placement.Actor.Class,
                        type.Path,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
            )
        )
        {
            foreach (var field in new[] { "Shop Contents", "Vendor Contents" })
            {
                foreach (
                    var item in Stock(building.Properties[field])
                        .Where(item => Offered(context, item))
                )
                {
                    var vendor = WorldSourceData.Reference(
                        EntityKind.Building,
                        building.Path,
                        VendorName(building.Properties, building.Path)
                    );
                    var sale = Sale(
                        context,
                        item,
                        vendor,
                        [],
                        [WorldSourceData.BuildingCondition(vendor)]
                    );
                    // Finished buildings stand only in the player's settlement.
                    yield return sale with
                    {
                        Source = sale.Source with { Settlement = true },
                    };
                }
            }
        }
        foreach (var (placement, dialogue, instance, properties) in Events(context))
        {
            var eventClass = ItemIdentity.ClassName(instance.Class);
            if (eventClass is not ("OPENdialogueSHOP" or "OPENdialogueSHOP_New"))
            {
                continue;
            }
            var name = VendorName(placement.Properties, placement.Actor.Class);
            JsonNode? stock;
            if (eventClass == "OPENdialogueSHOP_New")
            {
                // The event replaces its serialized preview stock with the selected table row.
                var selector = RecordValues.String(properties["Shop Name"]);
                stock =
                    selector is not null && shops.TryGetValue(selector, out var row)
                        ? row.Values["ShopContents"]
                        : null;
            }
            else
            {
                stock = properties["Shop Items"];
            }
            var requirements = WorldSourceData.DialogueConditions(context, dialogue, instance);
            foreach (var item in Stock(stock))
            {
                yield return Sale(
                    context,
                    item,
                    WorldSourceData.PlacementEntity(context, placement, EntityKind.Merchant, name),
                    [context.Places.Get(placement.Level)],
                    [
                        .. WorldSourceData.Conditions(
                            context,
                            placement.Properties,
                            placement.Level
                        ),
                        .. requirements.Shown,
                    ]
                ) with
                {
                    Unresolved = requirements.Unresolved,
                };
            }
        }
    }

    // A settlement building hands its shopkeeper each item's default item info, and the shop event offers only
    // entries whose item class is set, so an item whose defaults leave it empty never appears in that shop.
    private static bool Offered(CatalogContext context, string item) =>
        context.Classes.TryGetValue(item, out var type)
        && RecordValues.At(type.Properties, "ItemInfo.ItemClass") is JsonObject;

    // Window_Trading_Shop:Get Current City names a city only in these persistent levels, and Get City Respect
    // reads saved respect only for them. Every other level, settlements included, trades at neutral respect.
    private static readonly HashSet<string> RespectLevels = new(StringComparer.OrdinalIgnoreCase)
    {
        "Elderglen_Improved",
        "Italian_City_2019-07-31-18-08-03_2019-10-14-17-40-40_Auto2",
        "Khenmoore",
        "Woodhurst",
    };

    private static ItemSource Sale(
        CatalogContext context,
        string item,
        EntityReference entity,
        IReadOnlyList<EntityReference> places,
        IReadOnlyList<RelationCondition> conditions
    )
    {
        var value = context.Classes.TryGetValue(item, out var type)
            ? RecordValues.Integer(RecordValues.At(type.Properties, "ItemInfo.ItemValue"))
            : null;
        // Every merchant's dialogue opens Window_Trading_Shop through OPENdialogueSHOP, and that window prices by
        // respect. The settlement Blacksmith's Discount Multiplier is read only by the unused Shop_Window_Base.
        var city = places.FirstOrDefault(place =>
            place.Key is { } key && RespectLevels.Contains(key.Split('/')[^1].Split('.')[0])
        );
        return new ItemSource(
            item,
            new Source(
                SourceKind.Shop,
                entity.Name,
                places,
                value is { } price
                    ? new Price(checked((int)price), null) { Rule = PriceRule.Trading, City = city }
                    : null,
                new Amount(1, 1),
                [],
                []
            )
            {
                Entity = entity,
                Conditions = conditions,
            }
        );
    }

    private static IEnumerable<string> Stock(JsonNode? node)
    {
        foreach (var entry in RecordValues.Elements(node))
        {
            var item =
                RecordValues.AnyReference(entry)
                ?? RecordValues.AnyReference(RecordValues.At(entry, "ItemClass"));
            if (item is not null)
            {
                yield return item;
            }
        }
    }

    internal static string VendorName(JsonObject properties, string classPath)
    {
        foreach (var field in new[] { "Full Name", "Building Name" })
        {
            var name =
                RecordValues.Text(properties[field]) ?? RecordValues.String(properties[field]);
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
        }
        if (
            classPath.Contains(
                "/Construction_Buildings/Buildings_Finished/",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            var building = ItemIdentity.ClassName(classPath);
            var name = building switch
            {
                "WizardTower_Finished" => "Wizard Tower",
                "Fletcher_Finished" => "Fletcher",
                "Residence_02" => "Residence",
                "EngineerWorkshop" => "Engineers Workshop",
                _ => ItemIdentity.Readable(
                    building.EndsWith("_Finished", StringComparison.Ordinal)
                        ? building[..^"_Finished".Length]
                        : building
                ),
            };
            return name;
        }
        return WorldSourceData.Name(properties, classPath);
    }

    internal static IEnumerable<(
        Placement Placement,
        AssetRecord Dialogue,
        InnerObjectRecord Instance,
        JsonObject Properties
    )> Events(CatalogContext context)
    {
        var dialogues = context.Records.Dialogues.ToDictionary(
            asset => asset.Path.Split('.')[0],
            StringComparer.OrdinalIgnoreCase
        );
        foreach (var placement in context.Placements)
        {
            var reference = RecordValues.AnyReference(placement.Properties["Dialogue"]);
            if (
                reference is null
                || !dialogues.TryGetValue(reference.Split('.')[0], out var dialogue)
            )
            {
                continue;
            }
            var active = RecordValues
                .Elements(dialogue.Properties["Data"])
                .SelectMany(node => RecordValues.Elements(RecordValues.At(node, "Events")))
                .Select(RecordValues.AnyReference)
                .OfType<string>()
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (
                var instance in dialogue.Objects.Where(instance => active.Contains(instance.Path))
            )
            {
                if (context.IsExcludedClass(instance.Class))
                {
                    continue;
                }
                var properties = context.Classes.TryGetValue(instance.Class, out var type)
                    ? RecordMerge.OverlayProperties(type.Properties, instance.Properties)
                    : instance.Properties;
                yield return (placement, dialogue, instance, properties);
            }
        }
    }
}
