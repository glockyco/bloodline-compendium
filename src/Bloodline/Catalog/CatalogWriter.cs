using Bloodline.Output;
using Bloodline.Records;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

internal static class CatalogWriter
{
    public static void Write(RecordLayout layout, SiteBuild build, CatalogResult catalog)
    {
        Directory.CreateDirectory(layout.BuildDirectory);
        var staging = Path.Combine(layout.BuildDirectory, ".site-" + Guid.NewGuid().ToString("N"));
        var previous = Path.Combine(
            layout.BuildDirectory,
            ".site-previous-" + Guid.NewGuid().ToString("N")
        );
        try
        {
            CanonicalJson.Write(Path.Combine(staging, "build.json"), build);
            CanonicalJson.Write(Path.Combine(staging, "items.json"), catalog.Index);
            CanonicalJson.Write(Path.Combine(staging, "kinds.json"), EntityKinds.All);
            CanonicalJson.Write(Path.Combine(staging, "graph.json"), catalog.Graph);
            CanonicalJson.Write(Path.Combine(staging, "issues.json"), catalog.Issues);
            CanonicalJson.Write(Path.Combine(staging, "links.json"), catalog.Links);
            Directory.CreateDirectory(Path.Combine(staging, "items"));
            Directory.CreateDirectory(Path.Combine(staging, "icons"));
            Directory.CreateDirectory(Path.Combine(staging, "renders"));
            foreach (var item in ItemVersions.Pages(catalog.Items, catalog.Index.Categories))
            {
                CanonicalJson.Write(Path.Combine(staging, "items", item.Slug + ".json"), item);
            }
            CanonicalJson.Write(
                Path.Combine(staging, "disciplines.json"),
                catalog.Skills.DisciplineIndex
            );
            CanonicalJson.Write(Path.Combine(staging, "skills.json"), catalog.Skills.SkillIndex);
            CanonicalJson.Write(
                Path.Combine(staging, "planner.json"),
                SkillProducer.Planner(build.BuildId, catalog.Skills)
            );
            CanonicalJson.Write(
                Path.Combine(staging, "professions.json"),
                catalog.Professions.Index
            );
            CanonicalJson.Write(Path.Combine(staging, "mechanics.json"), catalog.Mechanics.Index);
            Directory.CreateDirectory(Path.Combine(staging, "mechanics"));
            foreach (var mechanic in catalog.Mechanics.Pages)
                CanonicalJson.Write(
                    Path.Combine(staging, "mechanics", mechanic.Slug + ".json"),
                    mechanic
                );
            Directory.CreateDirectory(Path.Combine(staging, "professions"));
            foreach (var profession in catalog.Professions.Pages)
                CanonicalJson.Write(
                    Path.Combine(staging, "professions", profession.Slug + ".json"),
                    profession
                );
            CanonicalJson.Write(Path.Combine(staging, "resources.json"), catalog.Resources.Index);
            Directory.CreateDirectory(Path.Combine(staging, "resources"));
            foreach (var resource in catalog.Resources.Pages)
                CanonicalJson.Write(
                    Path.Combine(staging, "resources", resource.Slug + ".json"),
                    resource
                );
            CanonicalJson.Write(Path.Combine(staging, "creatures.json"), catalog.Creatures.Index);
            Directory.CreateDirectory(Path.Combine(staging, "creatures"));
            foreach (var creature in catalog.Creatures.Pages)
                CanonicalJson.Write(
                    Path.Combine(staging, "creatures", creature.Slug + ".json"),
                    creature
                );
            Directory.CreateDirectory(Path.Combine(staging, "disciplines"));
            Directory.CreateDirectory(Path.Combine(staging, "skills"));
            foreach (var discipline in catalog.Skills.Disciplines)
            {
                CanonicalJson.Write(
                    Path.Combine(staging, "disciplines", discipline.Slug + ".json"),
                    discipline
                );
            }
            foreach (
                var skill in EntityVersions.Pages(catalog.Skills.Skills, SkillProducer.Versions)
            )
            {
                CanonicalJson.Write(Path.Combine(staging, "skills", skill.Slug + ".json"), skill);
            }
            foreach (
                var icon in catalog
                    .Items.Select(item => item.Icon)
                    .Concat(catalog.Skills.Skills.Select(skill => skill.Icon))
                    .Concat(
                        catalog.Skills.Disciplines.SelectMany(page =>
                            page.PassiveRows.SelectMany(row => row.Passives)
                                .Select(passive => passive.Icon)
                        )
                    )
                    .OfType<string>()
                    .Distinct(StringComparer.Ordinal)
            )
            {
                File.Copy(
                    Path.Combine(layout.IconsDirectory, icon + ".png"),
                    Path.Combine(staging, "icons", icon + ".png")
                );
            }
            foreach (
                var render in catalog
                    .Items.SelectMany(item => item.Renders)
                    .Select(render => render.Hash)
                    .Distinct(StringComparer.Ordinal)
            )
            {
                File.Copy(
                    Path.Combine(layout.RendersDirectory, render + ".webp"),
                    Path.Combine(staging, "renders", render + ".webp")
                );
            }
            if (Directory.Exists(layout.SiteDirectory))
            {
                Directory.Move(layout.SiteDirectory, previous);
            }
            try
            {
                Directory.Move(staging, layout.SiteDirectory);
            }
            catch
            {
                if (Directory.Exists(previous))
                {
                    Directory.Move(previous, layout.SiteDirectory);
                }
                throw;
            }
            CanonicalJson.Write(layout.ClaimsFile, new ClaimReport(build.BuildId, catalog.Claims));
            CanonicalJson.Write(layout.ReportFile, catalog.Report);
            if (catalog.Tooltips is null)
            {
                File.Delete(layout.TooltipReportFile);
            }
            else
            {
                CanonicalJson.Write(layout.TooltipReportFile, catalog.Tooltips);
            }
        }
        finally
        {
            if (Directory.Exists(staging))
            {
                Directory.Delete(staging, recursive: true);
            }
            if (Directory.Exists(previous) && Directory.Exists(layout.SiteDirectory))
            {
                Directory.Delete(previous, recursive: true);
            }
        }
    }
}
