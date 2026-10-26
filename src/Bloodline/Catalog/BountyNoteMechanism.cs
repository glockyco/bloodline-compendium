using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed class BountyNoteMechanism : ISourceMechanism
{
    public IEnumerable<ItemSource> FindSources(CatalogContext context)
    {
        foreach (
            var row in context
                .Records.Tables.Where(table => WorldSourceData.IsTable(table, "DT_UQC_Quests"))
                .SelectMany(table => table.Rows)
        )
        {
            if (WorldSourceData.EnumIndex(context, row.Values["Type"]) is not (5 or 6))
            {
                continue;
            }
            if (RecordValues.AnyReference(row.Values["ItemBountyNoteClass"]) is not { } note)
            {
                continue;
            }
            var quantity = context.Classes.TryGetValue(note, out var type)
                ? WorldSourceData.Integer(RecordValues.At(type.Properties, "ItemInfo.Quantity"))
                : null;
            yield return new ItemSource(
                note,
                new Source(
                    SourceKind.BountyNote,
                    RecordValues.Text(row.Values["DisplayName"]) ?? ItemIdentity.Readable(row.Name),
                    [],
                    null,
                    quantity is > 0 ? new Amount(quantity.Value, quantity.Value) : null,
                    null,
                    null,
                    [],
                    [
                        "Given when the quest is accepted. Required bounty items are completion requirements, not rewards or inputs to receiving the note.",
                    ]
                )
            );
        }
    }
}
