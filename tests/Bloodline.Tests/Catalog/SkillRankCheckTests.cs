using System.Text.Json.Nodes;
using Bloodline.Catalog;

namespace Bloodline.Tests.Catalog;

public sealed class SkillRankCheckTests
{
    [Fact]
    public void ReadsTheLatestPairOfCastsAndTellsARaisedHitFromAFixedOne()
    {
        var runtime = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var run = Directory.CreateDirectory(Path.Combine(runtime, "2026-01-01-skill-ranks"));
            // A scaled impact with fixed burn ticks, a skill whose hits ignore the ranks, and one that never hit.
            File.WriteAllLines(
                Path.Combine(run.FullName, "run.jsonl"),
                [
                    Cast("Flick", 0, 35, 10, 10),
                    Cast("Flick", 5, 61.25, 10, 10),
                    Cast("Spire", 0, 30, 30),
                    Cast("Spire", 5, 30, 30, 30),
                    Cast("Armor", 0),
                    Cast("Armor", 5),
                ]
            );
            // A run under another name is not a sweep.
            var other = Directory.CreateDirectory(Path.Combine(runtime, "2026-01-02-other"));
            File.WriteAllLines(
                Path.Combine(other.FullName, "run.jsonl"),
                [Cast("Spire", 0, 30), Cast("Spire", 5, 52.5)]
            );
            var casts = SkillRankCheck.Load(runtime);
            Assert.True(casts["Flick"].Rose(0.75));
            Assert.False(casts["Flick"].Unchanged);
            Assert.False(casts["Spire"].Rose(0.75));
            Assert.True(casts["Spire"].Unchanged);
            Assert.False(casts["Armor"].Rose(0.75));
            Assert.False(casts["Armor"].Unchanged);
        }
        finally
        {
            Directory.Delete(runtime, true);
        }
    }

    private static string Cast(string skill, int rank, params double[] losses) =>
        new JsonObject
        {
            ["ok"] = true,
            ["args"] = new JsonObject
            {
                ["skill"] = skill,
                ["ranks"] = new JsonObject { ["First"] = rank, ["Second"] = rank },
            },
            ["result"] = new JsonObject
            {
                ["hits"] = new JsonArray(
                    losses.Select(loss => (JsonNode)new JsonObject { ["loss"] = loss }).ToArray()
                ),
            },
        }.ToJsonString();
}
