using Bloodline.Records;

namespace Bloodline.Catalog;

/// <summary>
/// The console text that gives each item, as <c>FirstPersonGameMode:Recieve Console Command</c> reads it. The game
/// drops the first eight characters of an <c>AddItem</c> line, removes spaces, underscores and digits from the rest,
/// and looks the remainder up as a row name of <c>AllLootSpawnRates</c>, ignoring case. A row whose name keeps a
/// digit or an underscore therefore cannot be typed, and an item without a reachable row cannot be given. Run
/// console-items in build 24464547 observed both cases.
/// </summary>
internal sealed class ConsoleCommands
{
    private readonly Dictionary<string, string> commands = new(StringComparer.Ordinal);

    public ConsoleCommands(IReadOnlyList<TableRecord>? tables)
    {
        foreach (
            var table in (tables ?? []).Where(table =>
                ItemIdentity.ClassName(table.Path) == "AllLootSpawnRates"
            )
        )
        foreach (var row in table.Rows)
        {
            if (Typable(row.Name) && RecordValues.AnyReference(row.Values["ItemClass"]) is { } item)
                commands.TryAdd(item, "AddItem " + row.Name);
        }
    }

    /// <summary>The console line that gives the item, or null when the console cannot give it.</summary>
    public string? For(string itemPath) => commands.GetValueOrDefault(itemPath);

    private static bool Typable(string rowName) =>
        rowName.Length > 0
        && rowName.All(character => character is not ('_' or ' ') && !char.IsDigit(character));
}
