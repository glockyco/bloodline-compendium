using System.Text.Json;
using System.Text.Json.Nodes;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

/// <summary>
/// The skill kinds' output: the discipline list and pages, the skill list, and the skill versions, which share a page
/// by slug. <see cref="Entries"/> keeps every skill class as itself, for checks keyed by class.
/// </summary>
internal sealed record SkillOutput(
    DisciplineIndex DisciplineIndex,
    IReadOnlyList<DisciplinePage> Disciplines,
    SkillIndex SkillIndex,
    IReadOnlyList<SkillPage> Skills,
    IReadOnlyList<SkillPage> Entries,
    IReadOnlyDictionary<string, EntityReference> References
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
        IReadOnlyList<SkillPage> Entries,
        IReadOnlyDictionary<string, EntityReference> References,
        IReadOnlyList<GraphEntity> GraphEntities
    );

    /// <summary>How skills of one name become versions of one page, as items do.</summary>
    public static readonly VersionPolicy<SkillPage> Versions = new(
        EntityKind.Skill,
        Shown: skill => new
        {
            skill.Icon,
            skill.Description,
            skill.Facts,
        },
        Merge: copies =>
            copies.Count == 1
                ? copies[0]
                : copies[0] with
                {
                    Sources = EntityVersions.Union(copies.SelectMany(skill => skill.Sources)),
                    Notes = EntityVersions.Union(copies.SelectMany(skill => skill.Notes)),
                    Status = EntityVersions.MergedStatus(copies.Select(skill => skill.Status)),
                },
        AsVersion: (skill, id, label, name) =>
            skill with
            {
                VersionId = id,
                VersionLabel = label,
                VersionName = name,
            },
        WithVersions: (skill, others) => skill with { Versions = others }
    );

    public static EntityReference DisciplineReference(SkillTree tree) =>
        new(EntityKind.Discipline, tree.Path, tree.Name, ItemIdentity.Slug(tree.Name), null);

    public static EntityReference DisciplineReference(DisciplinePage page) =>
        new(EntityKind.Discipline, page.Key, page.Name, page.Slug, page.Icon);

    /// <summary>
    /// The discipline whose level an entry checks, found by the level name the game tracks, such as Bow for Bows &amp;
    /// Crossbows. An entry can check another discipline than its own tree's, as one Druidism skill checks Unarmed.
    /// </summary>
    public static EntityReference LevelDiscipline(
        IReadOnlyList<SkillTree> trees,
        string levelName
    ) =>
        trees.Where(tree => (tree.LevelName ?? tree.Name) == levelName).ToArray() switch
        {
            [var tree] => DisciplineReference(tree),
            var found => throw new InvalidOperationException(
                $"{found.Length} disciplines track the level {levelName}, so an entry that checks it has no discipline."
            ),
        };

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
        var rows = SkillRows();
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
        var keys = TableSkills()
            .Concat(treeSkills)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var rowOf = keys.ToDictionary(key => key, key => RowOf(key, rows), StringComparer.Ordinal);
        foreach (var row in rows.Values.Where(row => !rowOf.Values.Contains(row)))
            coverage.Add(
                GapKind.UnreadSkillRow,
                row.Name,
                null,
                "No skill's SkillId names this row."
            );
        // A row with an empty name names nothing a player can read, so the skill takes its class's readable name.
        var named = keys.Select(key =>
                (
                    Key: key,
                    Name: RecordValues.Text(rowOf[key]?.Values["Name"]) is { Length: > 0 } name
                        ? name
                        : null
                )
            )
            .ToArray();
        var entries = new List<SkillPage>();
        foreach (var (key, name) in named)
        {
            var row = rowOf[key];
            var shown = name ?? Readable(key);
            var sources = CatalogContext.MergeSources(
                candidates[(EntityKind.Skill, key)],
                context.ExcludedLevels
            );
            var page = new SkillPage(
                key,
                ItemIdentity.Slug(shown),
                shown,
                RecordValues.Text(row?.Values["Description"]),
                Icon(row?.Values["Icon"]),
                Facts(key, row),
                sources
            )
            {
                Status =
                    sources.Any(source => !source.Unconfirmed) ? ItemStatus.Obtainable
                    : olderTreeSkills.Contains(key) ? ItemStatus.NotInNormalPlay
                    : ItemStatus.NoKnownSource,
                Notes = KnownIssues.RegistryNotes(context.Records.Issues, key),
            };
            if (name is null)
                coverage.Add(
                    GapKind.UnknownFact,
                    shown,
                    EntityVersions.Reference(page, EntityKind.Skill),
                    "Name"
                );
            entries.Add(page);
        }
        // Skills of one name share a page as an item's versions do: copies that show the same merge, and skills
        // that show something else become versions.
        var versioned = EntityVersions.Group(entries, Versions);
        var skills = versioned.Entities.ToList();
        var entities = new List<GraphEntity>();
        foreach (var skill in skills)
        {
            var reference = EntityVersions.Reference(skill, EntityKind.Skill);
            entities.Add(new GraphEntity(reference, skill.Sources));
            if (skill.Sources.All(source => source.Unconfirmed))
                coverage.Add(GapKind.EntityWithoutSource, reference.Name, reference);
        }
        var skillReferences = versioned.References;
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
                        LevelDiscipline(trees, CheckedLevel(widget) ?? tree.LevelName ?? tree.Name),
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
                                LevelDiscipline(
                                    trees,
                                    CheckedLevel(slot) ?? tree.LevelName ?? tree.Name
                                )
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
                    new CategoryLink(ItemIdentity.Slug(tree.Category), tree.Category),
                    tree.LevelName,
                    skillRows,
                    passiveRows,
                    spellsongs
                )
                {
                    Notes = KnownIssues.RegistryNotes(context.Records.Issues, tree.Path),
                    // The game draws no icon of its own for a tree, so its first skill's, or else its first passive's,
                    // stands for it in lists and search.
                    Icon =
                        skillRows
                            .SelectMany(row => row.Slots)
                            .Select(slot => slot.Skill.Icon)
                            .FirstOrDefault(icon => icon is not null)
                        ?? passiveRows
                            .SelectMany(row => row.Passives)
                            .Select(passive => passive.Icon)
                            .FirstOrDefault(icon => icon is not null),
                }
            );
        }
        return new Prepared(disciplines, skills, entries, skillReferences, entities);
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
            .Distinct()
            .Select(category => new CategorySummary(category.Slug, category.Name, category.Name, 0))
            .ToList();
        var categoryOf = disciplines
            .SelectMany(page =>
                page.SkillRows.SelectMany(row => row.Slots)
                    .Select(slot => (Skill: slot.Skill.Key!, Category: page.Category.Slug))
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
                    VersionId = page.VersionId,
                    VersionLabel = page.VersionLabel,
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
                    )
                    {
                        Icon = page.Icon,
                    })
                    .ToArray()
            ),
            disciplines,
            new SkillIndex(counted, summaries)
            {
                Facets = Facets(summaries, counted, disciplines),
                Columns =
                [
                    new ListColumn("requiredLevel", "Required Level", slugs, true, null)
                    {
                        Ascending = true,
                    },
                    .. NumericColumns.Select(column => new ListColumn(
                        column.Id,
                        StatVocabulary.Number(column.Stat, null).Label,
                        slugs,
                        true,
                        StatVocabulary.Number(column.Stat, null).Unit
                    )),
                ],
            },
            skills,
            prepared.Entries,
            prepared.References
        );
    }

    private static readonly (string Id, Stat Stat)[] NumericColumns =
    [
        ("baseDamage", Stat.BaseDamage),
        ("manaCost", Stat.ManaCost),
        ("staminaCost", Stat.StaminaCost),
        ("cooldown", Stat.Cooldown),
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
                Labels = ItemList.Availability.ToDictionary(
                    pair => Camel(pair.Key.ToString()),
                    pair => pair.Value
                ),
            },
        }
            .Where(facet => facet.Values.Count > 0)
            .ToArray();
    }

    /// <summary>The skills table's rows by name, which the game looks up without regard to case.</summary>
    private Dictionary<string, TableRow> SkillRows()
    {
        var table = context.Records.Tables.SingleOrDefault(table =>
            ItemIdentity.ClassName(table.Path) == SkillsTable
        );
        return (table?.Rows ?? []).ToDictionary(row => row.Name, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>The skill classes the skills table's rows name.</summary>
    private IEnumerable<string> TableSkills() =>
        SkillRows()
            .Values.Select(row => RecordValues.AnyReference(row.Values["Skill"]))
            .OfType<string>();

    /// <summary>
    /// The row the game reads for a skill: the row named by the skill class's SkillId, as the skill slot's tooltip
    /// looks it up (SkillSlot:HandleTooltip), checked for every tree skill in run skilltips1.
    /// </summary>
    private TableRow? RowOf(string skill, Dictionary<string, TableRow> rows) =>
        context.Classes.TryGetValue(skill, out var type)
        && Words(type.Properties["SkillId"]) is { } id
        && rows.TryGetValue(id, out var row)
            ? row
            : null;

    /// <summary>
    /// The facts the game's skill tooltip shows: the cooldown and cost from the skill's row, the cost in stamina when
    /// the skill class uses stamina, and the base damage of the skill class that the row names, shown only above
    /// zero.
    /// </summary>
    private IReadOnlyList<Fact> Facts(string skill, TableRow? row)
    {
        if (row is null)
            return [StatVocabulary.Unknown(Stat.Cooldown), StatVocabulary.Unknown(Stat.ManaCost)];
        var stamina =
            context.Classes.TryGetValue(skill, out var type)
            && type.Properties["Uses Stamina"]?.GetValue<bool>() == true;
        var damage =
            RecordValues.AnyReference(row.Values["Skill"]) is { } named
            && context.Classes.TryGetValue(named, out var source)
                ? source.Properties["SkillInfo"]?["Damage"]?.GetValue<double>()
                : null;
        return
        [
            .. damage is > 0
                ? [StatVocabulary.Number(Stat.BaseDamage, damage)]
                : Array.Empty<Fact>(),
            StatVocabulary.Number(Stat.Cooldown, row.Values["Cooldown"]?.GetValue<double>()),
            StatVocabulary.Number(
                stamina ? Stat.StaminaCost : Stat.ManaCost,
                row.Values["ManaCost"]?.GetValue<double>()
            ),
        ];
    }

    private string? Icon(JsonNode? reference) =>
        RecordValues.AnyReference(reference) is { } path && textures.TryGetValue(path, out var hash)
            ? hash
            : null;

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
