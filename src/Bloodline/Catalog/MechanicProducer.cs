using Bloodline.Output;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal sealed record MechanicOutput(MechanicIndex Index, IReadOnlyList<MechanicPage> Pages);

/// <summary>
/// Reads the hand-written mechanic pages. Each page is a reference other text can link, and its own text links
/// entities as text written for the site does, which <see cref="Links"/> resolves.
/// </summary>
internal static class MechanicProducer
{
    public static IReadOnlyList<MechanicContent> Load(string path)
    {
        var content = CanonicalJson.Read<MechanicContent[]>(path);
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var page in content)
        {
            if (!names.Add(ItemIdentity.Slug(page.Name)))
                throw new InvalidDataException($"Two mechanic pages are named {page.Name}.");
            foreach (var section in page.Sections)
            {
                if (section.Table is not null && section.Data is not null)
                    throw new InvalidDataException(
                        $"A section of the mechanic page {page.Name} names a table to build and also writes one."
                    );
                if (
                    section.Table is { } table
                    && table.Rows.Any(row => row.Count != table.Headings.Count)
                )
                    throw new InvalidDataException(
                        $"A table of the mechanic page {page.Name} has a row without a cell for every heading."
                    );
            }
            if (page.Sections.Count == 0 || page.Evidence.Count == 0)
                throw new InvalidDataException(
                    $"The mechanic page {page.Name} needs sections and the evidence for them."
                );
        }
        return content;
    }

    public static EntityReference Reference(MechanicContent content) =>
        new(
            EntityKind.Mechanic,
            "Mechanic:" + content.Name,
            content.Name,
            ItemIdentity.Slug(content.Name),
            null
        );

    public static MechanicOutput Produce(IReadOnlyList<MechanicContent> content)
    {
        var pages = content
            .Select(page =>
            {
                var reference = Reference(page);
                return new MechanicPage(
                    reference.Key!,
                    reference.Slug!,
                    page.Name,
                    page.Intro,
                    page.Sections.Select(section =>
                            section with
                            {
                                Anchor = ItemIdentity.Slug(section.Heading),
                            }
                        )
                        .ToArray()
                );
            })
            .ToArray();
        return new MechanicOutput(
            new MechanicIndex(
                pages
                    .Select(page => new MechanicSummary(page.Key, page.Slug, page.Name, page.Intro))
                    .ToArray()
            ),
            pages
        );
    }

    /// <summary>Resolves the links of every sentence a mechanic page shows, adding each target to <paramref name="used"/>.</summary>
    public static void Links(
        IReadOnlyList<MechanicContent> content,
        IReadOnlyDictionary<string, EntityReference?> targets,
        IDictionary<string, EntityReference> used
    )
    {
        foreach (var page in content)
        {
            // A heading and a page name are titles, which hold no links.
            if (
                page
                    .Sections.Select(section => section.Heading)
                    .Append(page.Name)
                    .Any(LinkedText.Link().IsMatch)
            )
                throw new InvalidDataException(
                    $"The mechanic page {page.Name} links from a title."
                );
            foreach (var text in Texts(page))
                LinkedText.Resolve("The mechanic page " + page.Name, text, targets, used);
        }
    }

    /// <summary>Every sentence a mechanic page shows apart from its titles, where links may stand.</summary>
    public static IEnumerable<string> Texts(MechanicContent page) =>
        [
            page.Intro,
            .. page.Sections.SelectMany(section => section.Paragraphs),
            .. page.Sections.SelectMany(section =>
                section.Table?.Rows.SelectMany(row => row) ?? []
            ),
        ];
}
