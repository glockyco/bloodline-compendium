using Bloodline.Catalog;
using Bloodline.SiteData;

namespace Bloodline.Tests.Catalog;

public sealed class RenderIndexTests : IDisposable
{
    private readonly string directory = Directory.CreateTempSubdirectory("renders-").FullName;

    public void Dispose() => Directory.Delete(directory, recursive: true);

    private void Index(string json) =>
        File.WriteAllText(Path.Combine(directory, "index.json"), json);

    [Fact]
    public void PublishesEachBodyWithItsSizeMaleFirst()
    {
        File.WriteAllText(Path.Combine(directory, "aa.webp"), "");
        File.WriteAllText(Path.Combine(directory, "bb.webp"), "");
        Index(
            """
            {"/Invented/Coat.Coat_C": {
              "female": {"hash": "bb", "width": 300, "height": 400},
              "male": {"hash": "aa", "width": 320, "height": 410}
            }}
            """
        );

        var renders = RenderIndex.Load(directory)["/invented/coat.coat_c"];

        Assert.Equal(
            [
                new ItemRender(RenderBody.Male, "aa", 320, 410),
                new ItemRender(RenderBody.Female, "bb", 300, 400),
            ],
            renders
        );
    }

    [Fact]
    public void RefusesARenderWhoseFileIsMissing()
    {
        Index("""{"/Invented/Coat.Coat_C": {"male": {"hash": "cc", "width": 1, "height": 1}}}""");

        Assert.Throws<InvalidDataException>(() => RenderIndex.Load(directory));
    }

    [Fact]
    public void HasNoRendersWithoutAnIndex() => Assert.Empty(RenderIndex.Load(directory));
}
