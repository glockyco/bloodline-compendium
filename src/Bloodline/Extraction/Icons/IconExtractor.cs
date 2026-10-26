using Bloodline.Records;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets.Exports.Texture;

namespace Bloodline.Extraction.Icons;

internal sealed record IconResult(
    IReadOnlyList<TextureRecord> Textures,
    IReadOnlyList<GapRecord> Gaps
);

internal static class IconExtractor
{
    public static IconResult Extract(
        IFileProvider provider,
        IEnumerable<string> texturePaths,
        string iconsDirectory
    )
    {
        Directory.CreateDirectory(iconsDirectory);
        var textures = new List<TextureRecord>();
        var gaps = new List<GapRecord>();
        foreach (
            var path in texturePaths.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)
        )
        {
            var separator = path.LastIndexOf('.');
            var packagePath = separator < 0 ? path : path[..separator];
            TextureRecord record;
            byte[] rgba;
            try
            {
                if (separator < 0)
                    throw new InvalidDataException("Texture path has no object name.");
                var name = path[(separator + 1)..];
                var texture =
                    provider
                        .LoadPackage(packagePath)
                        .GetExports()
                        .OfType<UTexture2D>()
                        .SingleOrDefault(x => x.Name == name)
                    ?? throw new InvalidDataException(
                        "Texture object was not found in the package."
                    );
                var mip =
                    texture.GetMip(0)
                    ?? throw new InvalidDataException(
                        "Texture first mip is missing or has no bulk data."
                    );
                if (mip.SizeZ != 1)
                    throw new NotSupportedException(
                        $"Expected a 2D texture, found mip depth {mip.SizeZ}."
                    );
                rgba = PixelDecoder.Decode(
                    texture.Format,
                    mip.SizeX,
                    mip.SizeY,
                    mip.BulkData!.Data
                );
                record = new TextureRecord(
                    path,
                    IconPng.PixelHash(mip.SizeX, mip.SizeY, rgba),
                    mip.SizeX,
                    mip.SizeY,
                    texture.Format.ToString()
                );
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                gaps.Add(
                    new GapRecord(
                        packagePath,
                        path,
                        $"{exception.GetType().Name}: {exception.Message}"
                    )
                );
                continue;
            }

            // Filesystem failures stop extraction rather than becoming misleading missing-texture gaps.
            var outputPath = Path.Combine(iconsDirectory, record.Hash + ".png");
            if (!File.Exists(outputPath))
            {
                using var output = File.Create(outputPath);
                IconPng.Write(output, record.Width, record.Height, rgba);
            }
            textures.Add(record);
        }
        return new IconResult(textures, gaps);
    }
}
