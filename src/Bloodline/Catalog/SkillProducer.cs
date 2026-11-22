using System.Text.Json;
using System.Text.Json.Nodes;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

/// <summary>The skill kinds' output: the discipline list and pages, and the skill list and pages.</summary>
internal sealed record SkillOutput(
    DisciplineIndex DisciplineIndex,
    IReadOnlyList<DisciplinePage> Disciplines,
    SkillIndex SkillIndex,
    IReadOnlyList<SkillPage> Skills
);

/// <summary>
/// Produces disciplines, skills, passives and spellsongs. A discipline is a tree the skill window opens, laid out
/// as its window. A skill is every skill class the skills table or a live tree names, with the sources mechanisms
/// find for it. Passives and spellsongs are entries on their discipline's page.
/// </summary>
internal sealed class SkillProducer
{
    private const string SkillsTable = "SkillsDataTable";

    private readonly CatalogContext context;
    private readonly IReadOnlyDictionary<string, string> textures;

    public SkillProducer(CatalogContext context, string? iconsDirectory)
    {
        this.context = context;
        textures = context
            .Records.Textures.Where(texture =>
                iconsDirectory is null
                || File.Exists(Path.Combine(iconsDirectory, texture.Hash + ".png"))
            )
            .ToDictionary(
                texture => texture.Path,
                texture => texture.Hash,
                StringComparer.OrdinalIgnoreCase
            );
    }

    /// <summary>The prepared skill kinds, before the shared graph adds their relations.</summary>
    internal sealed record Prepared(
        IReadOnlyList<DisciplinePage> Disciplines,
        IReadOnlyList<SkillPage> Skills,
        IReadOnlyList<GraphEntity> GraphEntities
    );

    public static EntityReference DisciplineReference(SkillTree tree) =>
        new(EntityKind.Discipline, tree.Path, tree.Name, ItemIdentity.Slug(tree.Name), null);

    public static int? Integer(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue<double>(out var number) ? (int)number : null;

    /// <summary>The level name an entry checks, such as Unarmed, from its Level To Check field.</summary>
    public static string? CheckedLevel(WidgetRecord widget) =>
        RecordValues.Text(widget.Properties["Level To Check"])
        ?? (widget.Properties["Level To Check"] as JsonObject)?["display"]?.GetValue<string>()
        ?? (widget.Properties["Level To Check"] as JsonValue)?.GetValue<string>();

    /// <summary>The spellsong rows of the tables that trees show, keyed by table path and row name.</summary>
    public static IEnumerable<(string Key, TableRow Row, SkillTree Tree)> SpellsongRows(
        CatalogContext context
    )
    {
        foreach (var tree in context.SkillTrees.All.Where(tree => tree.SpellsongTable is not null))
        {
            var table = context.Records.Tables.Single(table => table.Path == tree.SpellsongTable);
            foreach (var row in table.Rows)
                yield return (table.Path + ":" + row.Name, row, tree);
        }
    }

    public Prepared Prepare(
        ILookup<(EntityKind, string), EntitySource> candidates,
        Coverage coverage
    )
    {
        var trees = context.SkillTrees.All;
        var rows = SkillRows(coverage);
        var skillNames = new Dictionary<string, string>(StringComparer.Ordinal);
        var treeSkills = trees
            .SelectMany(tree => tree.SkillRows.SelectMany(row => row.Entries))
            .Select(slot => RecordValues.AnyReference(slot.Properties["Skill"]))
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);
        var olderTreeSkills = context
            .SkillTrees.TreeClasses.Except(trees.Select(tree => tree.Path))
            .SelectMany(path => context.Classes[path].Widgets!)
            .Select(widget => RecordValues.AnyReference(widget.Properties["Skill"]))
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);
        var keys = rows.Keys.Concat(treeSkills).Distinct(StringComparer.Ordinal).ToArray();
        var named = keys.Select(key =>
                (
                    Key: key,
                    Name: rows.TryGetValue(key, out var row)
                        ? RecordValues.Text(row.Values["Name"])
                        : null
                )
            )
            .ToArray();
        var slugs = Slugs(named.Select(entry => (entry.Key, entry.Name ?? Readable(entry.Key))));
        var skills = new List<SkillPage>();
        var entities = new List<GraphEntity>();
        foreach (var (key, name) in named)
        {
            var row = rows.GetValueOrDefault(key);
            var reference = new EntityReference(
                EntityKind.Skill,
                key,
                name ?? Readable(key),
                slugs[key],
                Icon(row?.Values["Icon"])
            );
            if (name is null)
                coverage.Add(GapKind.UnknownFact, reference.Name, reference, "Name");
            var sources = CatalogContext.MergeSources(
                candidates[(EntityKind.Skill, key)],
                context.ExcludedLevels
            );
            var confirmed = sources.Count(source => !source.Unconfirmed);
            var status =
                confirmed > 0 ? ItemStatus.Obtainable
                : olderTreeSkills.Contains(key) ? ItemStatus.NotInNormalPlay
                : ItemStatus.NoKnownSource;
            skills.Add(
                new SkillPage(
                    key,
                    slugs[key],
                    reference.Name,
                    RecordValues.Text(row?.Values["Description"]),
                    reference.Icon,
                    Facts(row),
                    sources
                )
                {
                    Status = status,
                }
            );
            entities.Add(new GraphEntity(reference, sources));
            if (confirmed == 0)
                coverage.Add(GapKind.EntityWithoutSource, reference.Name, reference);
        }
        var skillReferences = entities.ToDictionary(
            entity => entity.Reference.Key!,
            entity => entity.Reference
        );
        var disciplines = new List<DisciplinePage>();
        var songRows = SpellsongRows(context).ToArray();
        foreach (var tree in trees)
        {
            var discipline = DisciplineReference(tree);
            entities.Add(new GraphEntity(discipline, []));
            var anchors = new HashSet<string>(StringComparer.Ordinal);
            var passiveRows = new List<PassiveRow>();
            foreach (var row in tree.PassiveRows)
            {
                var passives = new List<PassiveEntry>();
                foreach (var widget in row.Entries)
                {
                    var name =
                        RecordValues.Text(widget.Properties["Passive Name"])
                        ?? Readable(widget.Path);
                    var anchor = Anchor("passive-" + ItemIdentity.Slug(name), anchors);
                    var entry = new PassiveEntry(
                        tree.Path + ":" + widget.Path,
                        anchor,
                        name,
                        RecordValues.Text(widget.Properties["Passive Description"]),
                        Integer(widget.Properties["Required Level"]) ?? 0,
                        CheckedLevel(widget) ?? tree.LevelName ?? tree.Name,
                        Integer(widget.Properties["Point Cost"]) ?? 0,
                        Integer(widget.Properties["Point Increase Per Level"]) ?? 0,
                        widget.Properties["MultiLevel"]?.GetValue<bool>() == true
                            ? Integer(widget.Properties["Max MultiLevel"]) ?? 1
                            : 1,
                        RecordValues.Text(widget.Properties["MultiLevel Passive Name"])
                            is { Length: > 0 } rank
                        && rank != name
                            ? rank
                            : null,
                        Icon(widget.Properties["Icon Image"]?["ResourceObject"])
                    );
                    passives.Add(entry);
                    entities.Add(
                        new GraphEntity(
                            new EntityReference(
                                EntityKind.Passive,
                                entry.Key,
                                name,
                                null,
                                entry.Icon
                            )
                            {
                                Fragment = anchor,
                                Host = discipline,
                            },
                            []
                        )
                    );
                    if (entry.RequiredLevel != row.Level)
                        coverage.Add(
                            GapKind.RowLevelDifference,
                            tree.Name + ": " + name,
                            discipline,
                            $"Requires level {entry.RequiredLevel} in the row of level {row.Level}."
                        );
                }
                passiveRows.Add(new PassiveRow(row.Level, passives));
            }
            var skillRows = tree
                .SkillRows.Select(row => new SkillRow(
                    row.Level,
                    row.Entries.Where(slot =>
                            RecordValues.AnyReference(slot.Properties["Skill"]) is not null
                        )
                        .Select(slot =>
                        {
                            var level = Integer(slot.Properties["Required Level"]) ?? 0;
                            if (level != row.Level)
                                coverage.Add(
                                    GapKind.RowLevelDifference,
                                    tree.Name,
                                    discipline,
                                    $"A skill requires level {level} in the row of level {row.Level}."
                                );
                            return new SkillSlotEntry(
                                skillReferences[
                                    RecordValues.AnyReference(slot.Properties["Skill"])!
                                ],
                                level,
                                CheckedLevel(slot) ?? tree.LevelName ?? tree.Name
                            );
                        })
                        .ToArray()
                ))
                .ToArray();
            var spellsongs = new List<SpellsongEntry>();
            foreach (var (key, row, _) in songRows.Where(song => song.Tree.Path == tree.Path))
            {
                var name = Words(row.Values["SpellsongName"]) ?? row.Name;
                var anchor = Anchor("spellsong-" + ItemIdentity.Slug(name), anchors);
                var sources = CatalogContext.MergeSources(
                    candidates[(EntityKind.Spellsong, key)],
                    context.ExcludedLevels
                );
                spellsongs.Add(
                    new SpellsongEntry(
                        key,
                        anchor,
                        name,
                        Words(row.Values["SpellsongDescription"]),
                        (Words(row.Values["SpellsongTune"]) ?? "")
                            .Split(
                                ',',
                                StringSplitOptions.RemoveEmptyEntries
                                    | StringSplitOptions.TrimEntries
                            )
                            .Select(int.Parse)
                            .ToArray(),
                        Integer(row.Values["Charges"]) ?? 0,
                        row.Values["SpellsongEXP"]?.GetValue<double>() ?? 0,
                        sources
                    )
                );
                var reference = new EntityReference(EntityKind.Spellsong, key, name, null, null)
                {
                    Fragment = anchor,
                    Host = discipline,
                };
                entities.Add(new GraphEntity(reference, sources));
                if (sources.Count == 0)
                    coverage.Add(GapKind.EntityWithoutSource, name, reference);
            }
            disciplines.Add(
                new DisciplinePage(
                    tree.Path,
                    discipline.Slug!,
                    tree.Name,
                    tree.Category,
                    tree.LevelName,
                    skillRows,
                    passiveRows,
                    spellsongs
                )
            );
        }
        return new Prepared(disciplines, skills, entities);
    }

    public SkillOutput Finish(Prepared prepared, GraphResult graph)
    {
        EntityDocument? Document(EntityKind kind, string key) =>
            graph.Documents.GetValueOrDefault((kind, key));
        var disciplines = prepared
            .Disciplines.Select(page =>
                page with
                {
                    Relations = Document(EntityKind.Discipline, page.Key)?.Relations ?? [],
                    Spellsongs = page
                        .Spellsongs.Select(song =>
                            song with
                            {
                                Sources =
                                    graph.Sources.GetValueOrDefault(
                                        (EntityKind.Spellsong, song.Key)
                                    ) ?? song.Sources,
                            }
                        )
                        .ToArray(),
                }
            )
            .ToArray();
        var categories = disciplines
            .Select(page => page.Category)
            .Distinct(StringComparer.Ordinal)
            .Select(name => new CategorySummary(ItemIdentity.Slug(name), name, name, 0))
            .ToList();
        var categoryOf = disciplines
            .SelectMany(page =>
                page.SkillRows.SelectMany(row => row.Slots)
                    .Select(slot =>
                        (Skill: slot.Skill.Key!, Category: ItemIdentity.Slug(page.Category))
                    )
            )
            .GroupBy(entry => entry.Skill, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.First().Category,
                StringComparer.Ordinal
            );
        const string Other = "other";
        var skills = prepared
            .Skills.Select(page =>
                page with
                {
                    Relations = Document(EntityKind.Skill, page.Key)?.Relations ?? [],
                    Sources =
                        graph.Sources.GetValueOrDefault((EntityKind.Skill, page.Key))
                        ?? page.Sources,
                }
            )
            .OrderBy(page => page.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(page => page.Key, StringComparer.Ordinal)
            .ToArray();
        if (skills.Any(page => !categoryOf.ContainsKey(page.Key)))
            categories.Add(new CategorySummary(Other, "Other", "Other", 0));
        var summaries = skills
            .Select(page =>
            {
                var category = categoryOf.GetValueOrDefault(page.Key) ?? Other;
                var facets = new Dictionary<string, IReadOnlyList<string>>
                {
                    ["category"] = [category],
                    ["discipline"] = Distinct(
                        page.Sources.Where(source => source.Kind == SourceKind.SkillTree)
                            .Select(source => source.Name)
                    ),
                    ["source"] = Distinct(
                        page.Sources.Where(source => !source.Unconfirmed)
                            .Select(source => Camel(source.Kind.ToString()))
                    ),
                    ["place"] = Distinct(
                        page.Sources.Where(source => !source.Unconfirmed)
                            .SelectMany(source => source.Places)
                            .Select(place => place.Name)
                    ),
                    ["availability"] = [Camel(page.Status.ToString())],
                };
                var cells = new Dictionary<string, ListCell>();
                if (
                    page
                        .Sources.Where(source => source.Kind == SourceKind.SkillTree)
                        .SelectMany(source => source.Conditions)
                        .Select(condition => condition.Value)
                        .OfType<double>()
                        .DefaultIfEmpty(double.NaN)
                        .Min()
                        is var level
                    && !double.IsNaN(level)
                )
                    cells["requiredLevel"] = new ListCell(level, null);
                foreach (var (id, stat) in NumericColumns)
                {
                    if (page.Facts.FirstOrDefault(fact => fact.Stat == stat) is { } fact)
                        cells[id] = new ListCell(fact.Number, null);
                }
                return new SkillSummary(
                    page.Key,
                    page.Slug,
                    page.Name,
                    page.Icon,
                    category,
                    page.Status
                )
                {
                    Facets = facets
                        .Where(pair => pair.Value.Count > 0)
                        .ToDictionary(pair => pair.Key, pair => pair.Value),
                    Cells = cells,
                    PlaceSources = facets["place"]
                        .Select(place =>
                            (IReadOnlyList<string>)
                                page
                                    .Sources.Where(source =>
                                        !source.Unconfirmed
                                        && source.Places.Any(entry => entry.Name == place)
                                    )
                                    .Select(source => Camel(source.Kind.ToString()))
                                    .Distinct(StringComparer.Ordinal)
                                    .ToArray()
                        )
                        .ToArray(),
                };
            })
            .ToArray();
        var counted = categories
            .Select(category =>
                category with
                {
                    Count = summaries.Count(skill => skill.Category == category.Slug),
                }
            )
            .ToArray();
        var slugs = counted.Select(category => category.Slug).ToArray();
        return new SkillOutput(
            new DisciplineIndex(
                disciplines
                    .Select(page => new DisciplineSummary(
                        page.Key,
                        page.Slug,
                        page.Name,
                        page.Category,
                        page.SkillRows.Sum(row => row.Slots.Count),
                        page.PassiveRows.Sum(row => row.Passives.Count),
                        page.Spellsongs.Count
                    ))
                    .ToArray()
            ),
            disciplines,
            new SkillIndex(counted, summaries)
            {
                Facets = Facets(summaries, counted, disciplines),
                Columns =
                [
                    new ListColumn("requiredLevel", "Required Level", slugs, true, null),
                    .. NumericColumns.Select(column => new ListColumn(
                        column.Id,
                        StatVocabulary.Number(column.Stat, null).Label,
                        slugs,
                        true,
                        StatVocabulary.Number(column.Stat, null).Unit
                    )),
                ],
            },
            skills
        );
    }

    private static readonly (string Id, Stat Stat)[] NumericColumns =
    [
        ("manaCost", Stat.ManaCost),
        ("cooldown", Stat.Cooldown),
        ("castTime", Stat.CastTime),
    ];

    private static IReadOnlyList<ListFacet> Facets(
        IReadOnlyList<SkillSummary> skills,
        IReadOnlyList<CategorySummary> categories,
        IReadOnlyList<DisciplinePage> disciplines
    )
    {
        string[] ValuesOf(string id) =>
            skills
                .SelectMany(skill => skill.Facets.GetValueOrDefault(id) ?? [])
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        ListFacet Facet(string id, string label, IEnumerable<string> order) =>
            new(id, label, [], order.Where(ValuesOf(id).Contains).ToArray());
        return new[]
        {
            Facet("category", "Category", categories.Select(category => category.Slug)) with
            {
                Labels = categories.ToDictionary(
                    category => category.Slug,
                    category => category.Name
                ),
            },
            Facet("discipline", "Discipline", disciplines.Select(page => page.Name)),
            Facet("source", "How to Get", Enum.GetNames<SourceKind>().Select(Camel)),
            Facet(
                "place",
                "Where to Get",
                ValuesOf("place").Order(StringComparer.OrdinalIgnoreCase)
            ),
            Facet("availability", "Availability", Enum.GetNames<ItemStatus>().Select(Camel)) with
            {
                Labels = new Dictionary<string, string>
                {
                    [Camel(nameof(ItemStatus.Obtainable))] = "A way to get it is known",
                    [Camel(nameof(ItemStatus.NoKnownSource))] = "No known source",
                    [Camel(nameof(ItemStatus.NotInNormalPlay))] = "Not in normal play",
                },
            },
        }.Where(facet => facet.Values.Count > 0).ToArray();
    }

    /// <summary>
    /// The skills table's row for each skill class. A class with several rows takes its first row by name, and the
    /// others go to the maintainer report. Rows without a class go there too.
    /// </summary>
    private Dictionary<string, TableRow> SkillRows(Coverage coverage)
    {
        var result = new Dictionary<string, TableRow>(StringComparer.Ordinal);
        var table = context.Records.Tables.SingleOrDefault(table =>
            ItemIdentity.ClassName(table.Path) == SkillsTable
        );
        if (table is null)
            return result;
        foreach (
            var group in table
                .Rows.GroupBy(row => RecordValues.AnyReference(row.Values["Skill"]) ?? "")
                .OrderBy(group => group.Key, StringComparer.Ordinal)
        )
        {
            var ordered = group.OrderBy(row => row.Name, StringComparer.Ordinal).ToArray();
            if (group.Key.Length == 0)
            {
                foreach (var row in ordered)
                    coverage.Add(
                        GapKind.UnreadSkillRow,
                        row.Name,
                        null,
                        "The row names no skill class."
                    );
                continue;
            }
            result[group.Key] = ordered[0];
            foreach (var row in ordered.Skip(1))
                coverage.Add(
                    GapKind.UnreadSkillRow,
                    row.Name,
                    null,
                    $"A second row for {ItemIdentity.ClassName(group.Key)}, after {ordered[0].Name}."
                );
        }
        return result;
    }

    private static IReadOnlyList<Fact> Facts(TableRow? row)
    {
        if (row is null)
            return [StatVocabulary.Unknown(Stat.ManaCost), StatVocabulary.Unknown(Stat.Cooldown)];
        return
        [
            StatVocabulary.Number(Stat.ManaCost, row.Values["ManaCost"]?.GetValue<double>()),
            StatVocabulary.Number(Stat.Cooldown, row.Values["Cooldown"]?.GetValue<double>()),
            StatVocabulary.Number(Stat.CastTime, row.Values["CastngTime"]?.GetValue<double>()),
            StatVocabulary.Text(
                Stat.SkillType,
                (row.Values["SpellType"] as JsonObject)?["display"]?.GetValue<string>()
            ),
        ];
    }

    private string? Icon(JsonNode? reference) =>
        RecordValues.AnyReference(reference) is { } path && textures.TryGetValue(path, out var hash)
            ? hash
            : null;

    /// <summary>Slugs from names, qualified by an ordinal only where names repeat.</summary>
    private static Dictionary<string, string> Slugs(IEnumerable<(string Key, string Name)> entries)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (
            var group in entries
                .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                .GroupBy(entry => ItemIdentity.Slug(entry.Name))
        )
        {
            var index = 0;
            foreach (var (key, _) in group)
                result[key] = index++ == 0 ? group.Key : $"{group.Key}-{index}";
        }
        return result;
    }

    private static string Anchor(string wanted, HashSet<string> used)
    {
        var anchor = wanted;
        for (var index = 2; !used.Add(anchor); index++)
            anchor = $"{wanted}-{index}";
        return anchor;
    }

    /// <summary>A text or plain string value, which tables use alike for names and descriptions.</summary>
    public static string? Words(JsonNode? node) =>
        RecordValues.Text(node)
        ?? (node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null);

    private static string Readable(string path) =>
        ItemIdentity.Readable(
            ItemIdentity.ClassName(path).Replace("Skill_", "").Replace("_Skill", "")
        );

    private static IReadOnlyList<string> Distinct(IEnumerable<string> values) =>
        values.Distinct(StringComparer.Ordinal).ToArray();

    private static string Camel(string name) => JsonNamingPolicy.CamelCase.ConvertName(name);
}
