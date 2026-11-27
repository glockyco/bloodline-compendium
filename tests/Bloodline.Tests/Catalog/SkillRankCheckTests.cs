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
            // An earlier sweep is replaced as a whole by a later one.
            var earlier = Directory.CreateDirectory(
                Path.Combine(runtime, "2026-01-01-skill-ranks")
            );
            File.WriteAllLines(
                Path.Combine(earlier.FullName, "run.jsonl"),
                [
                    Cast("Spire", 0, [30]),
                    Cast("Spire", 5, [52.5]),
                    Cast("Gone", 0, [5]),
                    Cast("Gone", 5, [5]),
                ]
            );
            var run = Directory.CreateDirectory(Path.Combine(runtime, "2026-01-02-skill-ranks"));
            // A scaled impact with fixed burn ticks. Spears that ignore the ranks, two of which land in one frame at
            // rank 0, so the lost Health alone would read 60. A hit that only shows as lost Health. A skill that
            // never hit, whose empty hit tables are written as objects.
            File.WriteAllLines(
                Path.Combine(run.FullName, "run.jsonl"),
                [
                    Cast("Flick", 0, [35, 10, 10]),
                    Cast("Flick", 5, [61.25, 10, 10]),
                    Cast("Spire", 0, [30, 30, 30], [60, 30]),
                    Cast("Spire", 5, [30, 30, 30], [30, 30, 30]),
                    Cast("Lost", 0, null, [40]),
                    Cast("Lost", 5, null, [70]),
                    Cast("Armor", 0, null),
                    Cast("Armor", 5, null),
                    // A skill that missed at the first distance and is cast again from another.
                    Cast("Far", 0, null),
                    Cast("Far", 5, null),
                    Cast("Far", 0, [20]),
                    Cast("Far", 5, [35]),
                    Cast("Far", 0, null),
                    Cast("Far", 5, null),
                ]
            );
            // A run under another name is not a sweep.
            var other = Directory.CreateDirectory(Path.Combine(runtime, "2026-01-03-other"));
            File.WriteAllLines(
                Path.Combine(other.FullName, "run.jsonl"),
                [Cast("Spire", 0, [30]), Cast("Spire", 5, [52.5])]
            );
            var casts = SkillRankCheck.Load(runtime);
            Assert.True(casts["Flick"].Rose(0.75));
            Assert.False(casts["Flick"].Unchanged);
            Assert.False(casts["Spire"].Rose(0.75));
            Assert.True(casts["Spire"].Unchanged);
            Assert.True(casts["Lost"].Rose(0.75));
            Assert.False(casts["Armor"].Rose(0.75));
            Assert.False(casts["Armor"].Unchanged);
            Assert.True(casts["Far"].Rose(0.75));
            Assert.DoesNotContain("Gone", casts.Keys);
        }
        finally
        {
            Directory.Delete(runtime, true);
        }
    }

    private static string Cast(string skill, int rank, double[]? received, double[]? lost = null)
    {
        static JsonNode List(double[]? values, string field) =>
            values is null
                ? new JsonObject()
                : new JsonArray(
                    values.Select(value => (JsonNode)new JsonObject { [field] = value }).ToArray()
                );
        return new JsonObject
        {
            ["ok"] = true,
            ["args"] = new JsonObject
            {
                ["skill"] = skill,
                ["ranks"] = new JsonObject { ["First"] = rank, ["Second"] = rank },
            },
            ["result"] = new JsonObject
            {
                ["castAt"] = 0.2,
                ["receives"] = List(received, "damage"),
                ["hits"] = List(lost ?? received, "loss"),
            },
        }.ToJsonString();
    }
}
