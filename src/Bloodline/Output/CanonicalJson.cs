using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Bloodline.Output;

/// <summary>
/// Writes records and site data so that the same input gives the same bytes on every platform: object keys
/// in ordinal order, two-space indentation, LF line endings, UTF-8 without a byte order mark and a final newline.
/// Arrays keep their order, because order carries meaning in rows, pools and map entries.
/// </summary>
internal static class CanonicalJson
{
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    public static string Serialize<T>(T value) =>
        Sort(JsonSerializer.SerializeToNode(value, Options))?.ToJsonString(Options) + "\n";

    public static void Write<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, Serialize(value), Utf8);
    }

    public static T Read<T>(string path) =>
        JsonSerializer.Deserialize<T>(File.ReadAllText(path, Utf8), Options)
        ?? throw new InvalidDataException($"{path} holds null instead of {typeof(T).Name}.");

    private static JsonNode? Sort(JsonNode? node) =>
        node switch
        {
            JsonObject obj => new JsonObject(
                obj.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Select(pair => KeyValuePair.Create(pair.Key, Sort(pair.Value)))
            ),
            JsonArray array => new JsonArray([.. array.Select(Sort)]),
            _ => node?.DeepClone(),
        };

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            IndentSize = 2,
            NewLine = "\n",
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            RespectNullableAnnotations = true,
            RespectRequiredConstructorParameters = true,
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}
