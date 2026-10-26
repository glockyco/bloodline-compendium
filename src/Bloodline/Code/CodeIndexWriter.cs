using System.Text.Json;
using Bloodline.Output;

namespace Bloodline.Code;

internal static class CodeIndexWriter
{
    public static void Write(string path, CodeIndex index)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var stream = File.Create(path);
        using (
            var writer = new Utf8JsonWriter(
                stream,
                new JsonWriterOptions
                {
                    Indented = true,
                    IndentSize = 2,
                    NewLine = "\n",
                    Encoder = CanonicalJson.Options.Encoder,
                }
            )
        )
        {
            // The index exceeds the other records by orders of magnitude. Keep only one entry's
            // canonical JSON tree in memory instead of cloning the entire cross-reference.
            writer.WriteStartObject();
            Entries("functions", index.Functions);
            Entries("gaps", index.Gaps);
            Entries("references", index.References);
            writer.WriteEndObject();

            void Entries<T>(string name, IReadOnlyList<T> values)
            {
                writer.WriteStartArray(name);
                foreach (var value in values)
                    CanonicalJson.WriteValue(writer, value);
                writer.WriteEndArray();
            }
        }
        stream.WriteByte((byte)'\n');
    }
}
