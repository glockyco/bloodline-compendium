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
            CanonicalJson.Write(Path.Combine(staging, "coverage.json"), catalog.Coverage);
            Directory.CreateDirectory(Path.Combine(staging, "items"));
            Directory.CreateDirectory(Path.Combine(staging, "icons"));
            foreach (var item in catalog.Items)
            {
                CanonicalJson.Write(Path.Combine(staging, "items", item.Slug + ".json"), item);
            }
            foreach (
                var icon in catalog
                    .Items.Select(item => item.Icon)
                    .OfType<string>()
                    .Distinct(StringComparer.Ordinal)
            )
            {
                File.Copy(
                    Path.Combine(layout.IconsDirectory, icon + ".png"),
                    Path.Combine(staging, "icons", icon + ".png")
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
