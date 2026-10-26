using Bloodline.Configuration;
using Bloodline.Extraction;

namespace Bloodline.Tests.Extraction;

public sealed class ManifestTests : IDisposable
{
    private readonly DirectoryInfo directory = Directory.CreateTempSubdirectory(
        "bloodline-manifest-"
    );

    public void Dispose() => directory.Delete(recursive: true);

    [Fact]
    public void ParsesBuildAndSortedDepotManifestsWithoutMachinePaths()
    {
        var manifest = SteamManifest.Parse(
            """
            // Invented build and depot IDs.
            "AppState"
            {
                "appid" "1159290"
                "name" "An ignored \"title\""
                "buildid" "1234"
                "installdir" "Invented Game"
                "InstalledDepots"
                {
                    "200" { "manifest" "18446744073709551615" "size" "42" }
                    "100" { "manifest" "9007199254740993" }
                }
                "UserConfig" { "language" "english" }
            }
            """
        );

        Assert.Equal(1159290, manifest.Build.AppId);
        Assert.Equal(1234L, manifest.Build.BuildId);
        Assert.Equal("Invented Game", manifest.InstallDirectory);
        Assert.Collection(
            manifest.Build.Depots,
            depot =>
            {
                Assert.Equal(100L, depot.DepotId);
                Assert.Equal("9007199254740993", depot.ManifestId);
            },
            depot =>
            {
                Assert.Equal(200L, depot.DepotId);
                Assert.Equal("18446744073709551615", depot.ManifestId);
            }
        );
    }

    [Fact]
    public void ParsesUnquotedKeysAndCaseInsensitiveFieldNames()
    {
        var manifest = SteamManifest.Parse(
            """
            appstate { AppId 1159290 BuildId 1234 InstallDir Fixture InstalledDepots { 12 { Manifest 99 } } }
            """
        );

        Assert.Equal(1234L, manifest.Build.BuildId);
        Assert.Equal("99", Assert.Single(manifest.Build.Depots).ManifestId);
    }

    [Theory]
    [InlineData("AppState { appid 1159290 buildid 1 installdir Fixture InstalledDepots {")]
    [InlineData("AppState { appid 1159290 buildid nope installdir Fixture InstalledDepots {} }")]
    [InlineData("AppState { appid 1159290 buildid 1 installdir ../Fixture InstalledDepots {} }")]
    [InlineData(
        "AppState { appid 1159290 buildid 1 installdir Fixture InstalledDepots { 12 { manifest nope } } }"
    )]
    [InlineData(
        "AppState { appid 1159290 appid 1159290 buildid 1 installdir Fixture InstalledDepots {} }"
    )]
    public void RejectsInvalidManifests(string text)
    {
        Assert.Throws<InvalidDataException>(() => SteamManifest.Parse(text));
    }

    [Fact]
    public void DiscoversTheSingleContainerDirectoryRecursively()
    {
        var configuration = CreateInstallation();
        var snapshot = InstallationSnapshot.Capture(configuration);

        Assert.Equal(PaksDirectory(configuration), snapshot.PaksDirectory);
        Assert.Equal(1234L, snapshot.Manifest.Build.BuildId);
        snapshot.AssertUnchanged();
    }

    [Fact]
    public void RejectsMultipleMatchingContainerDirectories()
    {
        var configuration = CreateInstallation();
        var second = Path.Combine(
            configuration.SteamLibrary,
            "steamapps",
            "common",
            "Fixture",
            "Other"
        );
        Directory.CreateDirectory(second);
        File.WriteAllText(Path.Combine(second, "TheBloodline-WindowsNoEditor.utoc"), "invented");

        var error = Assert.Throws<InvalidDataException>(() =>
            InstallationSnapshot.Capture(configuration)
        );

        Assert.Contains("found 2", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsMissingMainContainer()
    {
        var configuration = CreateInstallation();
        File.Delete(
            Path.Combine(PaksDirectory(configuration), "TheBloodline-WindowsNoEditor.utoc")
        );

        var error = Assert.Throws<InvalidDataException>(() =>
            InstallationSnapshot.Capture(configuration)
        );

        Assert.Contains("found 0", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NamesTheChangedBuild()
    {
        var configuration = CreateInstallation();
        var snapshot = InstallationSnapshot.Capture(configuration);
        File.WriteAllText(configuration.AppManifest, Manifest(build: "1235"));

        var error = Assert.Throws<IOException>(snapshot.AssertUnchanged);

        Assert.Contains(
            "buildid changed from 1234 to 1235",
            error.Message,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void NamesTheChangedDepotManifest()
    {
        var configuration = CreateInstallation();
        var snapshot = InstallationSnapshot.Capture(configuration);
        File.WriteAllText(configuration.AppManifest, Manifest(depotManifest: "9877"));

        var error = Assert.Throws<IOException>(snapshot.AssertUnchanged);

        Assert.Contains(
            "depot 12 manifest changed from 9876 to 9877",
            error.Message,
            StringComparison.Ordinal
        );
    }

    [Theory]
    [InlineData(".utoc")]
    [InlineData(".ucas")]
    [InlineData(".pak")]
    public void NamesChangedContainerSizes(string extension)
    {
        var configuration = CreateInstallation();
        var snapshot = InstallationSnapshot.Capture(configuration);
        File.AppendAllText(
            Path.Combine(PaksDirectory(configuration), "TheBloodline-WindowsNoEditor" + extension),
            "changed"
        );

        var error = Assert.Throws<IOException>(snapshot.AssertUnchanged);

        Assert.Contains(
            "TheBloodline-WindowsNoEditor" + extension,
            error.Message,
            StringComparison.Ordinal
        );
        Assert.Contains("size changed", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DetectsTimestampOnlyChanges()
    {
        var configuration = CreateInstallation();
        var path = Path.Combine(PaksDirectory(configuration), "TheBloodline-WindowsNoEditor.ucas");
        var snapshot = InstallationSnapshot.Capture(configuration);
        File.SetLastWriteTimeUtc(path, File.GetLastWriteTimeUtc(path).AddMinutes(5));

        var error = Assert.Throws<IOException>(snapshot.AssertUnchanged);

        Assert.Contains("last-write time changed", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NamesAddedAndRemovedContainers(bool added)
    {
        var configuration = CreateInstallation();
        var snapshot = InstallationSnapshot.Capture(configuration);
        var name = added ? "new.pak" : "TheBloodline-WindowsNoEditor.pak";
        var path = Path.Combine(PaksDirectory(configuration), name);
        if (added)
        {
            File.WriteAllText(path, "invented");
        }
        else
        {
            File.Delete(path);
        }

        var error = Assert.Throws<IOException>(snapshot.AssertUnchanged);

        Assert.Contains(
            name + (added ? " was added" : " was removed"),
            error.Message,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void FailedExtractionLeavesPreviousRecordsUntouched()
    {
        var target = CreatePreviousRecords();
        string pending;

        Assert.Throws<InvalidDataException>(
            (Action)(
                () =>
                {
                    using var output = new AtomicRecordsDirectory(target);
                    pending = output.DirectoryPath;
                    File.WriteAllText(
                        Path.Combine(pending, "partial.json"),
                        "invented partial output"
                    );
                    throw new InvalidDataException("Invented extraction failure.");
                }
            )
        );

        Assert.Equal("previous", File.ReadAllText(Path.Combine(target, "previous.json")));
        Assert.Single(Directory.EnumerateDirectories(Path.GetDirectoryName(target)!));
    }

    [Fact]
    public void FailedCommitRestoresPreviousRecords()
    {
        var target = CreatePreviousRecords();
        using var output = new AtomicRecordsDirectory(target);
        Directory.Delete(output.DirectoryPath);

        Assert.Throws<DirectoryNotFoundException>(output.Commit);

        Assert.Equal("previous", File.ReadAllText(Path.Combine(target, "previous.json")));
    }

    [Fact]
    public void PublishingRecordsLeavesTheSiteDirectoryUntouched()
    {
        var target = CreatePreviousRecords();
        var site = Path.Combine(Path.GetDirectoryName(target)!, "site");
        Directory.CreateDirectory(site);
        var page = Path.Combine(site, "fixture.json");
        File.WriteAllText(page, "invented site data");
        using (var output = new AtomicRecordsDirectory(target))
        {
            File.WriteAllText(Path.Combine(output.DirectoryPath, "build.json"), "new records");
            output.Commit();
        }
        Assert.Equal("invented site data", File.ReadAllText(page));
    }

    [Fact]
    public void SuccessfulCommitReplacesRecordsOnlyAtCommit()
    {
        var target = CreatePreviousRecords();
        string pending;
        using (var output = new AtomicRecordsDirectory(target))
        {
            pending = output.DirectoryPath;
            File.WriteAllText(Path.Combine(pending, "new.json"), "new");
            Assert.Equal("previous", File.ReadAllText(Path.Combine(target, "previous.json")));
            output.Commit();
            Assert.Equal("new", File.ReadAllText(Path.Combine(target, "new.json")));
            Assert.False(File.Exists(Path.Combine(target, "previous.json")));
        }
        Assert.False(Directory.Exists(pending));
        Assert.Single(Directory.EnumerateDirectories(Path.GetDirectoryName(target)!));
    }

    private string CreatePreviousRecords()
    {
        var target = Path.Combine(directory.FullName, "output", "1234", "records");
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "previous.json"), "previous");
        return target;
    }

    private LocalConfiguration CreateInstallation()
    {
        var configuration = new LocalConfiguration(
            Path.Combine(directory.FullName, "Steam"),
            Path.Combine(directory.FullName, "output")
        );
        var paks = PaksDirectory(configuration);
        Directory.CreateDirectory(paks);
        File.WriteAllText(configuration.AppManifest, Manifest());
        foreach (var extension in new[] { ".utoc", ".ucas", ".pak" })
        {
            File.WriteAllText(
                Path.Combine(paks, "TheBloodline-WindowsNoEditor" + extension),
                "invented container"
            );
        }
        return configuration;
    }

    private static string PaksDirectory(LocalConfiguration configuration) =>
        Path.Combine(
            configuration.SteamLibrary,
            "steamapps",
            "common",
            "Fixture",
            "Game",
            "Content",
            "Paks"
        );

    private static string Manifest(string build = "1234", string depotManifest = "9876") =>
        $$"""
            "AppState"
            {
                "appid" "1159290"
                "buildid" "{{build}}"
                "installdir" "Fixture"
                "InstalledDepots" { "12" { "manifest" "{{depotManifest}}" } }
            }
            """;
}
