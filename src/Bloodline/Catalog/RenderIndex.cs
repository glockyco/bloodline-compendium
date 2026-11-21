using System.Text.Json;
using Bloodline.SiteData;

namespace Bloodline.Catalog;

/// <summary>
/// Reads the checked model renders that <c>tools/runtime/publish-renders.mjs</c> wrote: <c>index.json</c> maps each
/// item's class path to its renders by body, each with its hash, width and height, and each render is
/// <c>&lt;Hash&gt;.webp</c> beside it.
/// </summary>
internal static class RenderIndex
{
    private sealed record Entry(string Hash, int Width, int Height);

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static IReadOnlyDictionary<string, IReadOnlyList<ItemRender>> Load(string directory)
    {
        var file = Path.Combine(directory, "index.json");
        if (!File.Exists(file))
        {
            return new Dictionary<string, IReadOnlyList<ItemRender>>();
        }
        var index =
            JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, Entry>>>(
                File.ReadAllText(file),
                Options
            ) ?? throw new InvalidDataException($"{file} is empty.");
        var renders = new Dictionary<string, IReadOnlyList<ItemRender>>(
            StringComparer.OrdinalIgnoreCase
        );
        foreach (var (item, bodies) in index)
        {
            foreach (var entry in bodies.Values)
            {
                if (!File.Exists(Path.Combine(directory, entry.Hash + ".webp")))
                {
                    throw new InvalidDataException(
                        $"{file} names {entry.Hash}.webp, which does not exist."
                    );
                }
            }
            renders[item] = bodies
                .Select(render => new ItemRender(
                    Body(render.Key, file),
                    render.Value.Hash,
                    render.Value.Width,
                    render.Value.Height
                ))
                .OrderBy(render => render.Body)
                .ToArray();
        }
        return renders;
    }

    private static RenderBody Body(string name, string file) =>
        name switch
        {
            "male" => RenderBody.Male,
            "female" => RenderBody.Female,
            _ => throw new InvalidDataException($"{file} names the unknown body {name}."),
        };
}
