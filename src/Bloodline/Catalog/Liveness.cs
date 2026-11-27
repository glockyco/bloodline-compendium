using System.Text.Json.Nodes;
using Bloodline.Code;
using Bloodline.Records;

namespace Bloodline.Catalog;

/// <summary>
/// A function that names a class only to look for objects of it, read with <c>bloodline script</c>, so its read
/// brings nothing into the world. <see cref="Fingerprint"/> is the reviewed function's fingerprint, so a game update
/// that changes it fails the catalog until the function is read again.
/// </summary>
internal sealed record LookUp(string Citation, string Fingerprint);

/// <summary>
/// What can be in the world a player walks: everything a level in play holds, and everything those objects name in
/// their settings, tables, conversations and scripts, followed through what they name in turn. A class outside this
/// set cannot be met, however many unused classes or excluded levels name it. A script counts with every class it
/// reads, because one function often chooses the class that another spawns.
/// </summary>
internal sealed class Liveness
{
    internal static readonly LookUp[] LookUps =
    [
        // The game instance saves where the armies on the world map are, and no army stands on the map in play.
        new(
            "/Game/BloodlinesAlpha/BP_PersistantGameInstance:Save Overworld Enemy Locations and Data",
            "9e2e77ef21ff"
        ),
        // A Statue of Grief heals and damages the Demon of Grief it finds.
        new(
            "/Game/AIBehaviorToolkit/Blueprints/StatueOfGrief:Damage Demon of Grief",
            "1274bc9934c5"
        ),
        new("/Game/AIBehaviorToolkit/Blueprints/StatueOfGrief:Heal Demon of Grief", "e9775990951f"),
        // An arena button compares its enemy with every class it has a name for.
        new("/Game/Cities/HarbingerStronghold/Arena_Enemy_Button:Get Enemy Name", "2943778742ea"),
        new(
            "/Game/Cities/HarbingerStronghold/Arena_Chosen_Enemy_Button:Get Enemy Name",
            "a10a2822e8cd"
        ),
        // The Brittlebean dungeon looks for Bandit Chests to move, and run bandit-chests found none there.
        new(
            "/Game/Overworld/Villages/VillageBuildings/BanditEventsAndStructures/Brittlebean_Dungeon_Manager:Spawn Cropland Chests",
            "0eddff894161"
        ),
    ];

    private const string Animation = "/Script/Engine.AnimInstance";
    private readonly HashSet<string> live = new(StringComparer.OrdinalIgnoreCase);

    public bool IsLive(string path) => live.Contains(Package(path));

    public Liveness(CatalogContext context, IReadOnlyList<LookUp>? lookUps = null)
    {
        var code = context.Records.Code?.Functions ?? [];
        var skipped = new HashSet<string>(StringComparer.Ordinal);
        foreach (var lookUp in lookUps ?? LookUps)
            if (
                ReadFunction.Find(context, lookUp.Citation, lookUp.Fingerprint, "Liveness.LookUps")
                is not null
            )
                skipped.Add(lookUp.Citation);

        var edges = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        HashSet<string> From(string package)
        {
            if (!edges.TryGetValue(package, out var targets))
                edges[package] = targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            return targets;
        }
        void Walk(JsonNode? node, HashSet<string> targets, string? owner)
        {
            if (node is JsonObject obj)
            {
                if (RecordValues.AnyReference(obj) is { } reference)
                    targets.Add(Package(reference));
                if (obj.ContainsKey(RecordFormat.Map))
                {
                    // A weight of zero means the table never draws the entry.
                    foreach (var (key, value) in RecordValues.MapEntries(obj))
                    {
                        if (RecordValues.Number(value) is <= 0)
                            continue;
                        Walk(key, targets, null);
                        Walk(value, targets, null);
                    }
                    return;
                }
                foreach (var (field, value) in obj)
                {
                    // An Enemies table that no script of its class draws from spawns nothing, and neither do the
                    // tables by theme that the dungeon populator copies into it.
                    if (
                        owner is not null
                        && field.EndsWith("Enemies", StringComparison.Ordinal)
                        && context.Classes.TryGetValue(owner, out var type)
                        && type.Properties.ContainsKey("Enemies")
                        && !context.SpawnSites.DrawsEnemies(owner)
                    )
                        continue;
                    Walk(value, targets, null);
                }
            }
            else if (node is JsonArray array)
            {
                foreach (var value in array)
                    Walk(value, targets, null);
            }
        }

        var reads = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        // An animation blueprint's scripts move a body and check what it belongs to, as every skeleton asks whether
        // its owner is a Suicide Goblin.
        var animations = context
            .Records.Classes.Where(type => type.Ancestors.Contains(Animation))
            .Select(type => Package(type.Path))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var function in code)
        {
            // What a stream's chat spawns is left out, because a player does not meet it.
            if (
                skipped.Contains(function.Citation)
                || animations.Contains(function.Package)
                || function.Name.Contains("TwitchIntegrator", StringComparison.Ordinal)
            )
                continue;
            if (!reads.TryGetValue(function.Package, out var targets))
                reads[function.Package] = targets = new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );
            foreach (var read in function.Reads)
                if (read.Kind is CodeTargetKind.Object or CodeTargetKind.DataTable)
                    targets.Add(Package(read.Path));
        }
        foreach (var (package, targets) in reads)
            From(package).UnionWith(targets);
        foreach (var type in context.Records.Classes)
        {
            var targets = From(Package(type.Path));
            Walk(type.Properties, targets, type.Path);
            foreach (var widget in type.Widgets ?? [])
                Walk(widget.Properties, targets, null);
            // An object runs the scripts of the classes it derives from, which does not put those classes in the world.
            foreach (var ancestor in type.Ancestors)
                if (reads.TryGetValue(Package(ancestor), out var inherited))
                    targets.UnionWith(inherited);
        }
        foreach (var table in context.Records.Tables)
        foreach (var row in table.Rows)
            Walk(row.Values, From(Package(table.Path)), null);
        foreach (var dialogue in context.Records.Dialogues)
        {
            var targets = From(Package(dialogue.Path));
            Walk(dialogue.Properties, targets, null);
            foreach (var inner in dialogue.Objects)
                Walk(inner.Properties, targets, null);
        }

        var todo = new Queue<string>();
        void Root(string path)
        {
            if (live.Add(Package(path)))
                todo.Enqueue(Package(path));
        }
        foreach (var level in context.Records.Levels)
            if (!context.ExcludedLevels.Contains(level.Path))
                Root(level.Path);
        foreach (var placement in context.Placements)
        {
            Root(placement.Actor.Class);
            Walk(placement.Actor.Properties, From(Package(placement.Level)), placement.Actor.Class);
        }
        while (todo.TryDequeue(out var current))
            foreach (var target in edges.GetValueOrDefault(current) ?? [])
                Root(target);
    }

    private static string Package(string path) => path.Split('.')[0].Split(':')[0];
}
