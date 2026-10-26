using System.Globalization;
using System.Text;
using Bloodline.Records;

namespace Bloodline.Extraction;

internal sealed record SteamManifest(BuildRecord Build, string InstallDirectory)
{
    public static SteamManifest Parse(string text)
    {
        var document = new KeyValuesReader(text).Read();
        var app = Object(document, "AppState");
        var appId = PositiveInteger(Value(app, "appid"), "appid");
        if (appId > int.MaxValue)
        {
            throw new InvalidDataException("Steam manifest appid exceeds the supported range.");
        }
        var buildId = PositiveInteger(Value(app, "buildid"), "buildid");
        var installDirectory = Value(app, "installdir");
        if (
            string.IsNullOrWhiteSpace(installDirectory)
            || installDirectory is "." or ".."
            || installDirectory.IndexOfAny(['/', '\\', ':', '\0']) >= 0
        )
        {
            throw new InvalidDataException(
                "Steam manifest installdir must name one directory under steamapps/common."
            );
        }
        var depots = Object(app, "InstalledDepots")
            .Select(entry =>
            {
                var depotId = PositiveInteger(entry.Key, "InstalledDepots depot ID");
                if (entry.Value is not Dictionary<string, object> depot)
                {
                    throw new InvalidDataException(
                        $"Steam manifest depot {entry.Key} must be an object."
                    );
                }
                var manifest = Value(depot, "manifest");
                if (
                    !ulong.TryParse(
                        manifest,
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out var id
                    )
                    || id == 0
                )
                {
                    throw new InvalidDataException(
                        $"Steam manifest depot {entry.Key} has an invalid manifest ID."
                    );
                }
                return new DepotRecord(depotId, id.ToString(CultureInfo.InvariantCulture));
            })
            .OrderBy(depot => depot.DepotId)
            .ToArray();
        if (depots.Select(depot => depot.DepotId).Distinct().Count() != depots.Length)
        {
            throw new InvalidDataException("Steam manifest contains duplicate depot IDs.");
        }
        return new SteamManifest(new BuildRecord((int)appId, buildId, depots), installDirectory);
    }

    private static long PositiveInteger(string value, string field)
    {
        if (
            !long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number)
            || number <= 0
        )
        {
            throw new InvalidDataException($"Steam manifest {field} must be a positive integer.");
        }
        return number;
    }

    private static string Value(Dictionary<string, object> node, string key) =>
        node.TryGetValue(key, out var value) && value is string text
            ? text
            : throw new InvalidDataException($"Steam manifest is missing string {key}.");

    private static Dictionary<string, object> Object(Dictionary<string, object> node, string key) =>
        node.TryGetValue(key, out var value) && value is Dictionary<string, object> child
            ? child
            : throw new InvalidDataException($"Steam manifest is missing object {key}.");

    private sealed class KeyValuesReader(string text)
    {
        private int position;

        public Dictionary<string, object> Read() => ReadObject(nested: false);

        private Dictionary<string, object> ReadObject(bool nested)
        {
            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            while (true)
            {
                SkipWhitespace();
                if (position == text.Length)
                {
                    if (nested)
                    {
                        throw Error("Unclosed object");
                    }
                    return values;
                }
                if (text[position] == '}')
                {
                    if (!nested)
                    {
                        throw Error("Unexpected closing brace");
                    }
                    position++;
                    return values;
                }
                var key = ReadToken();
                SkipWhitespace();
                object value;
                if (position < text.Length && text[position] == '{')
                {
                    position++;
                    value = ReadObject(nested: true);
                }
                else
                {
                    value = ReadToken();
                }
                if (!values.TryAdd(key, value))
                {
                    throw Error($"Duplicate key {key}");
                }
            }
        }

        private string ReadToken()
        {
            SkipWhitespace();
            if (position == text.Length || text[position] is '{' or '}')
            {
                throw Error("Expected a key or value");
            }
            if (text[position] != '"')
            {
                var start = position;
                while (
                    position < text.Length
                    && !char.IsWhiteSpace(text[position])
                    && text[position] is not '{' and not '}'
                )
                {
                    position++;
                }
                return text[start..position];
            }
            position++;
            var result = new StringBuilder();
            while (position < text.Length)
            {
                var character = text[position++];
                if (character == '"')
                {
                    return result.ToString();
                }
                if (character == '\\' && position < text.Length)
                {
                    var escaped = text[position++];
                    switch (escaped)
                    {
                        case 'n':
                            result.Append('\n');
                            break;
                        case 'r':
                            result.Append('\r');
                            break;
                        case 't':
                            result.Append('\t');
                            break;
                        case '"':
                            result.Append('"');
                            break;
                        case '\\':
                            result.Append('\\');
                            break;
                        default:
                            result.Append('\\').Append(escaped);
                            break;
                    }
                }
                else
                {
                    result.Append(character);
                }
            }
            throw Error("Unclosed quoted string");
        }

        private void SkipWhitespace()
        {
            while (position < text.Length)
            {
                if (char.IsWhiteSpace(text[position]) || text[position] == '\uFEFF')
                {
                    position++;
                }
                else if (
                    text[position] == '/'
                    && position + 1 < text.Length
                    && text[position + 1] == '/'
                )
                {
                    while (position < text.Length && text[position] != '\n')
                    {
                        position++;
                    }
                }
                else
                {
                    return;
                }
            }
        }

        private InvalidDataException Error(string message) =>
            new($"Invalid Steam KeyValues manifest at character {position}: {message}.");
    }
}
