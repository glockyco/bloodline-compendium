using System.Text.Json;

namespace Bloodline.Configuration;

/// <summary>
/// Locations that differ per machine, read from a Git-ignored JSON file. <c>bloodline.example.json</c> in the
/// repository shows the format. Relative paths resolve against the file's directory.
/// </summary>
internal sealed record LocalConfiguration(string SteamLibrary, string OutputDirectory)
{
    public const int AppId = 1159290;

    public const string DefaultFile = "bloodline.local.json";

    /// <summary>The app manifest that Steam keeps for the installed build.</summary>
    public string AppManifest =>
        Path.Combine(SteamLibrary, "steamapps", $"appmanifest_{AppId}.acf");

    public static LocalConfiguration Load(string file)
    {
        var path = Path.GetFullPath(file);
        if (!File.Exists(path))
        {
            throw new ConfigurationException(
                $"Configuration file {path} does not exist. Copy bloodline.example.json to {DefaultFile} and set its locations."
            );
        }

        FileSettings settings;
        try
        {
            settings =
                JsonSerializer.Deserialize<FileSettings>(File.ReadAllText(path), JsonOptions)
                ?? throw new ConfigurationException($"Configuration file {path} is empty.");
        }
        catch (JsonException error)
        {
            throw new ConfigurationException(
                $"Configuration file {path} is not valid JSON: {error.Message}"
            );
        }

        var directory = Path.GetDirectoryName(path)!;
        if (string.IsNullOrWhiteSpace(settings.SteamLibrary))
        {
            throw new ConfigurationException(
                $"Setting steamLibrary in {path} is missing. Set it to the Steam library folder that contains steamapps."
            );
        }

        var configuration = new LocalConfiguration(
            Path.GetFullPath(settings.SteamLibrary, directory),
            Path.GetFullPath(
                string.IsNullOrWhiteSpace(settings.OutputDirectory)
                    ? "artifacts"
                    : settings.OutputDirectory,
                directory
            )
        );
        if (!File.Exists(configuration.AppManifest))
        {
            throw new ConfigurationException(
                $"Setting steamLibrary in {path} names {configuration.SteamLibrary}, which has no {Path.Combine("steamapps", $"appmanifest_{AppId}.acf")}. Install The Bloodline in that library or correct the setting."
            );
        }
        return configuration;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>The file's settings. Only the lab runtime in <c>tools/runtime</c> reads <c>labDirectory</c>.</summary>
    private sealed record FileSettings(
        string? SteamLibrary,
        string? OutputDirectory,
        string? LabDirectory
    );
}

/// <summary>A missing or invalid machine-local setting. The message names the setting and how to fix it.</summary>
internal sealed class ConfigurationException(string message) : Exception(message);
