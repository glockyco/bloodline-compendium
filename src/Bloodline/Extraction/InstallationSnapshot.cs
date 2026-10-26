using Bloodline.Configuration;

namespace Bloodline.Extraction;

internal sealed class InstallationSnapshot
{
    private const string MainContainer = "TheBloodline-WindowsNoEditor.utoc";
    private readonly LocalConfiguration configuration;
    private readonly string installationDirectory;
    private readonly SortedDictionary<string, ContainerState> containers;

    private InstallationSnapshot(
        LocalConfiguration configuration,
        SteamManifest manifest,
        string installationDirectory,
        string paksDirectory,
        SortedDictionary<string, ContainerState> containers
    )
    {
        this.configuration = configuration;
        Manifest = manifest;
        this.installationDirectory = installationDirectory;
        PaksDirectory = paksDirectory;
        this.containers = containers;
    }

    public SteamManifest Manifest { get; }

    public string PaksDirectory { get; }

    public static InstallationSnapshot Capture(LocalConfiguration configuration)
    {
        var manifest = SteamManifest.Parse(File.ReadAllText(configuration.AppManifest));
        if (manifest.Build.AppId != LocalConfiguration.AppId)
        {
            throw new InvalidDataException(
                $"Steam manifest appid is {manifest.Build.AppId}, expected {LocalConfiguration.AppId}."
            );
        }
        var installationDirectory = Path.Combine(
            configuration.SteamLibrary,
            "steamapps",
            "common",
            manifest.InstallDirectory
        );
        var paksDirectory = FindPaksDirectory(installationDirectory);
        var snapshot = new InstallationSnapshot(
            configuration,
            manifest,
            installationDirectory,
            paksDirectory,
            ReadContainers(installationDirectory)
        );
        snapshot.AssertUnchanged();
        return snapshot;
    }

    public void AssertUnchanged()
    {
        SteamManifest current;
        try
        {
            current = SteamManifest.Parse(File.ReadAllText(configuration.AppManifest));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            throw new IOException(
                "Steam app manifest changed or became unreadable during extraction.",
                error
            );
        }
        if (current.Build.AppId != Manifest.Build.AppId)
        {
            throw Changed($"appid changed from {Manifest.Build.AppId} to {current.Build.AppId}");
        }
        if (current.Build.BuildId != Manifest.Build.BuildId)
        {
            throw Changed(
                $"buildid changed from {Manifest.Build.BuildId} to {current.Build.BuildId}"
            );
        }
        if (current.InstallDirectory != Manifest.InstallDirectory)
        {
            throw Changed(
                $"installdir changed from {Manifest.InstallDirectory} to {current.InstallDirectory}"
            );
        }
        var previousDepots = Manifest.Build.Depots.ToDictionary(depot => depot.DepotId);
        var currentDepots = current.Build.Depots.ToDictionary(depot => depot.DepotId);
        foreach (var id in previousDepots.Keys.Union(currentDepots.Keys).Order())
        {
            if (!previousDepots.TryGetValue(id, out var previous))
            {
                throw Changed($"depot {id} was added");
            }
            if (!currentDepots.TryGetValue(id, out var next))
            {
                throw Changed($"depot {id} was removed");
            }
            if (previous.ManifestId != next.ManifestId)
            {
                throw Changed(
                    $"depot {id} manifest changed from {previous.ManifestId} to {next.ManifestId}"
                );
            }
        }
        SortedDictionary<string, ContainerState> currentContainers;
        try
        {
            currentContainers = ReadContainers(installationDirectory);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            throw new IOException(
                "Steam container files changed or became unreadable during extraction.",
                error
            );
        }
        foreach (
            var name in containers.Keys.Union(currentContainers.Keys).Order(StringComparer.Ordinal)
        )
        {
            if (!containers.TryGetValue(name, out var previous))
            {
                throw Changed($"container {name} was added");
            }
            if (!currentContainers.TryGetValue(name, out var next))
            {
                throw Changed($"container {name} was removed");
            }
            if (previous.Length != next.Length)
            {
                throw Changed(
                    $"container {name} size changed from {previous.Length} to {next.Length}"
                );
            }
            if (previous.LastWriteTimeUtc != next.LastWriteTimeUtc)
            {
                throw Changed($"container {name} last-write time changed");
            }
        }
    }

    private static IOException Changed(string detail) =>
        new($"Steam installation changed during extraction: {detail}.");

    private static string FindPaksDirectory(string installationDirectory)
    {
        var candidates = Directory
            .EnumerateFiles(installationDirectory, "*", SearchOption.AllDirectories)
            .Where(file =>
                string.Equals(
                    Path.GetFileName(file),
                    MainContainer,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            .Select(file => Path.GetDirectoryName(file)!)
            .ToArray();
        return candidates.Length == 1
            ? candidates[0]
            : throw new InvalidDataException(
                $"Expected exactly one directory containing {MainContainer} under the installed game, found {candidates.Length}."
            );
    }

    private static SortedDictionary<string, ContainerState> ReadContainers(
        string installationDirectory
    )
    {
        var result = new SortedDictionary<string, ContainerState>(StringComparer.Ordinal);
        foreach (
            var path in Directory.EnumerateFiles(
                installationDirectory,
                "*",
                SearchOption.AllDirectories
            )
        )
        {
            var extension = Path.GetExtension(path);
            if (
                !extension.Equals(".utoc", StringComparison.OrdinalIgnoreCase)
                && !extension.Equals(".ucas", StringComparison.OrdinalIgnoreCase)
                && !extension.Equals(".pak", StringComparison.OrdinalIgnoreCase)
            )
            {
                continue;
            }
            var file = new FileInfo(path);
            result.Add(
                Path.GetRelativePath(installationDirectory, path).Replace('\\', '/'),
                new ContainerState(file.Length, file.LastWriteTimeUtc)
            );
        }
        return result;
    }

    private sealed record ContainerState(long Length, DateTime LastWriteTimeUtc);
}
