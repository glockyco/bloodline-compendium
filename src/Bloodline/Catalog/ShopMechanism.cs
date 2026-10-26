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
            foreach (var field in new[] { "Shop Contents", "Vendor Contents" })
            {
                foreach (var item in Stock(placement.Properties[field]))
                {
                    yield return Sale(context, item, name, [context.Places.Get(placement.Level)]);
                }
            }
        }
        // Settlement buildings create their vendors at runtime, so a missing actor placement is not absence of stock.
        foreach (
            var building in context.Records.Classes.Where(type =>
                type.Path.Contains(
                    "/Construction_Buildings/Buildings_Finished/",
                    StringComparison.OrdinalIgnoreCase
                ) && !context.IsExcludedClass(type.Path)
            )
        )
        {
            foreach (var field in new[] { "Shop Contents", "Vendor Contents" })
            {
                foreach (var item in Stock(building.Properties[field]))
                {
                    var sale = Sale(
                        context,
                        item,
                        VendorName(building.Properties, building.Path),
                        []
                    );
                    yield return sale with
                    {
                        Source = sale.Source with
                        {
                            Notes = ["Stock from a constructed settlement building."],
                        },
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
            foreach (var item in Stock(stock))
            {
                yield return Sale(context, item, name, [context.Places.Get(placement.Level)]);
            }
        }
    }

    private static ItemSource Sale(
        CatalogContext context,
        string item,
        string name,
        IReadOnlyList<Place> places
    )
    {
        var value = context.Classes.TryGetValue(item, out var type)
            ? RecordValues.Integer(RecordValues.At(type.Properties, "ItemInfo.ItemValue"))
            : null;
        return new ItemSource(
            item,
            new Source(
                SourceKind.Shop,
                name,
                places,
                value is { } price ? new Price(checked((int)price), null) : null,
                new Amount(1, 1),
                null,
                null,
                [],
                []
            )
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

    internal static string VendorName(JsonObject properties, string classPath) =>
        RecordValues.Text(properties["Full Name"])
        ?? RecordValues.Text(properties["Building Name"])
        ?? ItemIdentity.Readable(ItemIdentity.ClassName(classPath));

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
