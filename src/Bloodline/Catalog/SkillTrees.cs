using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Bloodline.Code;
using Bloodline.Records;

namespace Bloodline.Catalog;

/// <summary>A tree the skill window opens, with its menu's category and its rows as the tree window lays them out.</summary>
internal sealed record SkillTree(
    string Path,
    string Name,
    string Category,
    int Order,
    IReadOnlyList<TreeRow> SkillRows,
    IReadOnlyList<TreeRow> PassiveRows
)
{
    /// <summary>The level name its slots and passives check, such as Pyromancy, when they agree on one.</summary>
    public string? LevelName { get; init; }

    /// <summary>The data table of spellsongs that the tree fills its panel from, when it shows them.</summary>
    public string? SpellsongTable { get; init; }
}

/// <summary>A row of a tree window: the level its label shows and its entries in the game's order.</summary>
internal sealed record TreeRow(int Level, IReadOnlyList<WidgetRecord> Entries);

/// <summary>
/// Reads the skill trees a player reaches. The skill window's category buttons open menus, whose buttons open
/// trees. Each button is followed through the click function the widget binds to it, to the widget class that
/// function creates, because the functions' names are not reliable. A tree is a widget class that holds skill slots
/// or passive buttons. Trees that no button opens are older trees the game does not show.
/// </summary>
internal sealed partial class SkillTrees
{
    public const string SlotClass = "SkillSlot_C";
    public const string PassiveClass = "PassiveSkillButton_C";

    private readonly CatalogContext context;

    public SkillTrees(CatalogContext context)
    {
        this.context = context;
        var widgetClasses = context
            .Classes.Values.Where(type => type.Widgets is not null)
            .ToDictionary(type => type.Path, StringComparer.Ordinal);
        TreeClasses = widgetClasses
            .Values.Where(type => type.Widgets!.Any(widget => IsEntry(widget)))
            .Select(type => type.Path)
            .ToHashSet(StringComparer.Ordinal);
        var functions = (context.Records.Code?.Functions ?? [])
            .GroupBy(function => (function.Owner, function.Name))
            .ToDictionary(group => group.Key, group => group.First());
        // Every button of every widget class, with the widget classes its click creates.
        var opens = new List<(ClassRecord Owner, string Button, string Target)>();
        foreach (var owner in widgetClasses.Values.Where(type => type.Bindings is not null))
        {
            // Buttons raise OnClicked, and the game's own button widgets raise OnClick.
            foreach (
                var binding in owner.Bindings!.Where(binding =>
                    binding.Event.Contains("Click", StringComparison.Ordinal)
                )
            )
            {
                if (!functions.TryGetValue((owner.Path, binding.Function), out var function))
                    continue;
                foreach (
                    var read in function.Reads.Where(read =>
                        read.Kind == CodeTargetKind.Object && widgetClasses.ContainsKey(read.Path)
                    )
                )
                    opens.Add((owner, binding.Component, read.Path));
            }
        }
        var trees = new List<SkillTree>();
        // A menu is a widget class whose buttons open trees, and a category button is one that opens a menu.
        foreach (
            var category in opens.Where(open =>
                opens.Any(inner =>
                    inner.Owner.Path == open.Target && TreeClasses.Contains(inner.Target)
                )
            )
        )
        {
            var categoryName = Label(category.Owner, category.Button);
            foreach (
                var open in opens.Where(open =>
                    open.Owner.Path == category.Target && TreeClasses.Contains(open.Target)
                )
            )
            {
                var tree = widgetClasses[open.Target];
                trees.Add(
                    new SkillTree(
                        tree.Path,
                        Label(open.Owner, open.Button) ?? ItemIdentity.Readable(open.Button),
                        categoryName ?? ItemIdentity.Readable(category.Button),
                        0,
                        Rows(tree, SlotClass),
                        Rows(tree, PassiveClass)
                    )
                    {
                        LevelName = LevelName(tree),
                        SpellsongTable = (context.Records.Code?.Functions ?? [])
                            .Where(function => function.Owner == tree.Path)
                            .SelectMany(function => function.Reads)
                            .Where(read =>
                                read.Kind == CodeTargetKind.DataTable
                                && context.Records.Tables.Any(table =>
                                    table.Path == read.Path
                                    && table.Rows.Any(row =>
                                        row.Values.ContainsKey("SpellsongTune")
                                    )
                                )
                            )
                            .Select(read => read.Path)
                            .FirstOrDefault(),
                    }
                );
                Order.Add(
                    tree.Path,
                    (Position(category.Owner, category.Button), Position(open.Owner, open.Button))
                );
            }
        }
        All = trees
            .OrderBy(tree => Order[tree.Path].Category, PositionComparer.Instance)
            .ThenBy(tree => Order[tree.Path].Button, PositionComparer.Instance)
            .Select((tree, index) => tree with { Order = index })
            .ToArray();
    }

    /// <summary>The trees a player reaches, in the order of the skill window's categories and menus.</summary>
    public IReadOnlyList<SkillTree> All { get; }

    /// <summary>Every widget class that holds skill slots or passive buttons, reached or not.</summary>
    public IReadOnlySet<string> TreeClasses { get; }

    private Dictionary<
        string,
        (IReadOnlyList<int> Category, IReadOnlyList<int> Button)
    > Order { get; } = new(StringComparer.Ordinal);

    public bool IsEntry(WidgetRecord widget) =>
        WorldSourceData.IsClass(context, widget.Class, SlotClass)
        || WorldSourceData.IsClass(context, widget.Class, PassiveClass);

    /// <summary>The text a player reads on a widget: its own text, or the first text widget inside it.</summary>
    private static string? Label(ClassRecord owner, string widgetName)
    {
        var root = "WidgetTree." + widgetName;
        // A button widget of the game's own, such as a category button, holds its label itself.
        if (
            owner.Widgets!.FirstOrDefault(widget => widget.Path == root) is { } self
            && RecordValues.Text(self.Properties["Text"]) is { Length: > 0 } own
        )
            return own.Trim();
        var children = owner.Widgets!.ToLookup(widget => widget.Parent, StringComparer.Ordinal);
        var pending = new Queue<string>([root]);
        while (pending.Count > 0)
        {
            foreach (var child in children[pending.Dequeue()].OrderBy(child => child.Position))
            {
                if (RecordValues.Text(child.Properties["Text"]) is { Length: > 0 } text)
                    return text.Trim();
                pending.Enqueue(child.Path);
            }
        }
        return null;
    }

    /// <summary>A widget's positions from the root of its tree, which order it among its kind.</summary>
    private static IReadOnlyList<int> Position(ClassRecord owner, string widgetName)
    {
        var widgets = owner.Widgets!.ToDictionary(widget => widget.Path, StringComparer.Ordinal);
        var positions = new List<int>();
        for (
            var current = widgets.GetValueOrDefault("WidgetTree." + widgetName);
            current is not null;
            current = current.Parent is { } parent ? widgets.GetValueOrDefault(parent) : null
        )
            positions.Insert(0, current.Position ?? 0);
        return positions;
    }

    /// <summary>
    /// The rows of a tree's entries of one class. A row is the nearest panel above an entry that holds a text widget,
    /// its label, such as "Pyromancy Level 10:". Entries in a row follow their positions below it.
    /// </summary>
    private IReadOnlyList<TreeRow> Rows(ClassRecord tree, string entryClass)
    {
        var widgets = tree.Widgets!.ToDictionary(widget => widget.Path, StringComparer.Ordinal);
        var children = tree.Widgets!.ToLookup(widget => widget.Parent, StringComparer.Ordinal);
        var rows = new Dictionary<
            string,
            (int Level, List<(List<int> Order, WidgetRecord Entry)> Entries)
        >(StringComparer.Ordinal);
        foreach (
            var entry in tree.Widgets!.Where(widget =>
                WorldSourceData.IsClass(context, widget.Class, entryClass)
            )
        )
        {
            var order = new List<int>();
            var current = entry;
            string? label = null;
            string? row = null;
            while (current.Parent is { } parent && widgets.TryGetValue(parent, out var panel))
            {
                order.Insert(0, current.Position ?? 0);
                label = children[parent]
                    .Select(child => RecordValues.Text(child.Properties["Text"]))
                    .FirstOrDefault(text => text is not null && LevelNumber().IsMatch(text));
                if (label is not null)
                {
                    row = parent;
                    break;
                }
                current = panel;
            }
            if (row is null || label is null)
                throw new InvalidDataException(
                    $"{tree.Path}: {entry.Path} lies in no row with a level label."
                );
            if (!rows.TryGetValue(row, out var found))
            {
                found = (int.Parse(LevelNumber().Match(label).Groups[1].Value), []);
                rows.Add(row, found);
            }
            found.Entries.Add((order, Effective(entry)));
        }
        return rows
            .Values.OrderBy(row => row.Level)
            .Select(row => new TreeRow(
                row.Level,
                row.Entries.OrderBy(
                        entry => (IReadOnlyList<int>)entry.Order,
                        PositionComparer.Instance
                    )
                    .Select(entry => entry.Entry)
                    .ToArray()
            ))
            .ToArray();
    }

    /// <summary>
    /// An entry with the values the game reads: its own overrides over its class's defaults. A passive that sets only
    /// its level, such as Fire Affinity, costs the class's 1 point, rising by 1, over its 5 ranks.
    /// </summary>
    private WidgetRecord Effective(WidgetRecord entry)
    {
        if (context.Classes.GetValueOrDefault(entry.Class) is not { } type)
            return entry;
        var properties = (JsonObject)type.Properties.DeepClone();
        foreach (var (name, value) in entry.Properties)
            properties[name] = value?.DeepClone();
        return entry with { Properties = properties };
    }

    /// <summary>The level name that most of a tree's entries check, such as Pyromancy.</summary>
    private string? LevelName(ClassRecord tree) =>
        tree.Widgets!.Where(IsEntry)
            .Select(widget =>
                RecordValues.Text(widget.Properties["Level To Check"]) ?? EnumText(widget)
            )
            .OfType<string>()
            .GroupBy(name => name, StringComparer.Ordinal)
            .OrderByDescending(group => group.Count())
            .Select(group => group.Key)
            .FirstOrDefault();

    private static string? EnumText(WidgetRecord widget) => SkillProducer.CheckedLevel(widget);

    [GeneratedRegex(@"(\d+)\s*:?\s*$")]
    private static partial Regex LevelNumber();

    private sealed class PositionComparer : IComparer<IReadOnlyList<int>>
    {
        public static readonly PositionComparer Instance = new();

        public int Compare(IReadOnlyList<int>? x, IReadOnlyList<int>? y)
        {
            for (var index = 0; index < Math.Min(x!.Count, y!.Count); index++)
            {
                var difference = x[index].CompareTo(y[index]);
                if (difference != 0)
                    return difference;
            }
            return x.Count.CompareTo(y.Count);
        }
    }
}
