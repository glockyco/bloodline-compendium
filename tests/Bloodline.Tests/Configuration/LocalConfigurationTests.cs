using Bloodline.Configuration;

namespace Bloodline.Tests.Configuration;

public sealed class LocalConfigurationTests : IDisposable
{
    private readonly DirectoryInfo directory = Directory.CreateTempSubdirectory(
        "bloodline-config-"
    );

    public void Dispose() => directory.Delete(recursive: true);

    private string WriteConfig(string json)
    {
        var path = Path.Combine(directory.FullName, LocalConfiguration.DefaultFile);
        File.WriteAllText(path, json);
        return path;
    }

    [Fact]
    public void ResolvesRelativeLocationsAgainstTheFile()
    {
        var library = Directory.CreateDirectory(
            Path.Combine(directory.FullName, "Steam", "steamapps")
        );
        File.WriteAllText(Path.Combine(library.FullName, "appmanifest_1159290.acf"), "");

        var configuration = LocalConfiguration.Load(WriteConfig("""{"steamLibrary": "Steam"}"""));

        Assert.Equal(Path.Combine(directory.FullName, "Steam"), configuration.SteamLibrary);
        Assert.Equal(Path.Combine(directory.FullName, "artifacts"), configuration.OutputDirectory);
    }

    [Fact]
    public void NamesAMissingFile()
    {
        var path = Path.Combine(directory.FullName, "absent.json");

        var error = Assert.Throws<ConfigurationException>(() => LocalConfiguration.Load(path));

        Assert.Contains(path, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NamesAMissingSetting()
    {
        var error = Assert.Throws<ConfigurationException>(() =>
            LocalConfiguration.Load(WriteConfig("{}"))
        );

        Assert.Contains("steamLibrary", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NamesALibraryWithoutTheGame()
    {
        Directory.CreateDirectory(Path.Combine(directory.FullName, "Empty"));

        var error = Assert.Throws<ConfigurationException>(() =>
            LocalConfiguration.Load(WriteConfig("""{"steamLibrary": "Empty"}"""))
        );

        Assert.Contains("steamLibrary", error.Message, StringComparison.Ordinal);
        Assert.Contains("appmanifest_1159290.acf", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsUnknownSettings()
    {
        Assert.Throws<ConfigurationException>(() =>
            LocalConfiguration.Load(WriteConfig("""{"steamLibary": "Steam"}"""))
        );
    }
}
