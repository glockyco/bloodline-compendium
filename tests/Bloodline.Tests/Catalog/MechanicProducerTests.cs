using Bloodline.Catalog;
using Bloodline.Output;
using Bloodline.SiteData;

namespace Bloodline.Tests.Catalog;

public sealed class MechanicProducerTests
{
    [Fact]
    public void SectionThatNamesAndWritesATableFailsToLoad()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");
        try
        {
            CanonicalJson.Write(
                path,
                new[]
                {
                    new MechanicContent(
                        "Invented page",
                        "",
                        [
                            new MechanicSection("Events", [])
                            {
                                Data = "worldMapEvents",
                                Table = new MechanicTable(["Event"], []),
                            },
                        ],
                        ["invented"]
                    ),
                }
            );

            var error = Assert.Throws<InvalidDataException>(() => MechanicProducer.Load(path));

            Assert.Contains("Invented page", error.Message, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
