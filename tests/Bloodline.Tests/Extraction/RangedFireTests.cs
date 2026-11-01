using System.Text.Json.Nodes;
using Bloodline.Code;

namespace Bloodline.Tests.Extraction;

public sealed class RangedFireTests
{
    private const string Owner = "/Game/Invented/Ranged.Ranged_C";
    private const string Projectile = "/Game/Invented/Projectile.Projectile_C";

    private static JsonObject Literal(double value) =>
        new() { ["Token"] = "EX_FloatConst", ["Value"] = value };

    private static JsonObject Variable(string name) =>
        new()
        {
            ["Token"] = "EX_LocalVariable",
            ["Variable"] = new JsonObject { ["$field"] = Owner + ":Fire:" + name },
        };

    private static JsonObject Call(string name, params JsonNode[] parameters) =>
        new()
        {
            ["Token"] = "EX_CallMath",
            ["StackNode"] = new JsonObject
            {
                ["$object"] = "/Script/Engine.KismetMathLibrary:" + name,
            },
            ["Parameters"] = new JsonArray(parameters),
        };

    private static JsonObject Node(
        int offset,
        string token,
        params (string Key, JsonNode Value)[] fields
    )
    {
        var result = new JsonObject { ["Token"] = token, ["StatementIndex"] = offset };
        foreach (var (key, value) in fields)
            result[key] = value;
        return result;
    }

    private static JsonObject Assign(int offset, string name, JsonNode value) =>
        Node(offset, "EX_Let", ("Variable", Variable(name)), ("Assignment", value));

    private static JsonObject Spawn(int offset) =>
        Assign(
            offset,
            "projectile",
            Call(
                "BeginDeferredActorSpawnFromClass",
                new JsonObject { ["Token"] = "EX_Self" },
                new JsonObject
                {
                    ["Token"] = "EX_ObjectConst",
                    ["Value"] = new JsonObject { ["$object"] = Projectile },
                }
            )
        );

    private static RangedFire Analyze(params JsonObject[] statements)
    {
        var function = new ScriptFunction(
            "/Game/Invented/Ranged",
            Owner,
            "Custom Fire Event",
            false,
            statements
        );
        return RangedFireAnalysis.Analyze(
            function,
            new Dictionary<string, ScriptFunction> { [function.Path] = function },
            new JsonArray(statements.Select(statement => statement.DeepClone()).ToArray())
        );
    }

    [Fact]
    public void AProjectileKeepsItsAssignedDamage()
    {
        var result = Analyze(
            Spawn(0),
            Node(
                10,
                "EX_CallMath",
                (
                    "StackNode",
                    new JsonObject
                    {
                        ["$object"] = "/Script/Engine.KismetSystemLibrary:SetFloatPropertyByName",
                    }
                ),
                (
                    "Parameters",
                    new JsonArray(
                        Variable("projectile"),
                        new JsonObject { ["Token"] = "EX_NameConst", ["Value"] = "Damage" },
                        Literal(100)
                    )
                )
            ),
            Node(20, "EX_Return")
        );
        var projectile = Assert.Single(result.Projectiles!);
        Assert.Equal(Projectile, projectile.Class);
        Assert.True(projectile.Assigned);
        Assert.Equal(100, projectile.Damage!.At(85, 50));
    }

    [Fact]
    public void AnInclusiveLoopCountsEveryProjectileRatherThanEverySpawnStatement()
    {
        var result = Analyze(
            Assign(0, "counter", Literal(1)),
            Assign(10, "condition", Call("LessEqual_IntInt", Variable("counter"), Literal(15))),
            Node(
                20,
                "EX_JumpIfNot",
                ("BooleanExpression", Variable("condition")),
                ("CodeOffset", JsonValue.Create(70)!)
            ),
            Spawn(30),
            Assign(40, "counter", Call("Add_IntInt", Variable("counter"), Literal(1))),
            Node(50, "EX_Jump", ("CodeOffset", JsonValue.Create(10)!)),
            Node(70, "EX_Return")
        );
        Assert.Equal(15, result.Projectiles!.Count);
        Assert.All(result.Projectiles, projectile => Assert.False(projectile.Assigned));
    }

    [Fact]
    public void BranchesWithDifferentProjectileCountsStayUnknown()
    {
        var result = Analyze(
            Node(
                0,
                "EX_JumpIfNot",
                ("BooleanExpression", Variable("unknown")),
                ("CodeOffset", JsonValue.Create(30)!)
            ),
            Spawn(10),
            Node(20, "EX_Return"),
            Node(30, "EX_Return")
        );
        Assert.Null(result.Projectiles);
    }

    [Fact]
    public void AViewChoiceDoesNotChangeInsideABoundedVolley()
    {
        var view = Variable("firstPerson");
        view["Token"] = "EX_InstanceVariable";
        var result = Analyze(
            Assign(0, "counter", Literal(1)),
            Assign(10, "condition", Call("LessEqual_IntInt", Variable("counter"), Literal(15))),
            Node(
                20,
                "EX_JumpIfNot",
                ("BooleanExpression", Variable("condition")),
                ("CodeOffset", JsonValue.Create(80)!)
            ),
            Node(
                25,
                "EX_JumpIfNot",
                ("BooleanExpression", view),
                ("CodeOffset", JsonValue.Create(50)!)
            ),
            Spawn(30),
            Node(40, "EX_Jump", ("CodeOffset", JsonValue.Create(60)!)),
            Spawn(50),
            Assign(60, "counter", Call("Add_IntInt", Variable("counter"), Literal(1))),
            Node(70, "EX_Jump", ("CodeOffset", JsonValue.Create(10)!)),
            Node(80, "EX_Return")
        );
        Assert.Equal(15, result.Projectiles!.Count);
    }

    [Fact]
    public void DelayedContinuationsCountTheirLaterProjectiles()
    {
        var result = Analyze(
            Spawn(0),
            Node(
                10,
                "EX_CallMath",
                (
                    "StackNode",
                    new JsonObject { ["$object"] = "/Script/Engine.KismetSystemLibrary:Delay" }
                ),
                (
                    "Parameters",
                    new JsonArray(
                        new JsonObject { ["Token"] = "EX_Self" },
                        Literal(0.03),
                        new JsonObject
                        {
                            ["Token"] = "EX_StructConst",
                            ["Struct"] = new JsonObject
                            {
                                ["$object"] = "/Script/Engine.LatentActionInfo",
                            },
                            ["Properties"] = new JsonArray(
                                new JsonObject { ["Token"] = "EX_SkipOffsetConst", ["Value"] = 30 },
                                Literal(1),
                                new JsonObject
                                {
                                    ["Token"] = "EX_NameConst",
                                    ["Value"] = "Custom Fire Event",
                                },
                                new JsonObject { ["Token"] = "EX_Self" }
                            ),
                        }
                    )
                )
            ),
            Node(20, "EX_PopExecutionFlow"),
            Spawn(30),
            Node(40, "EX_Return")
        );
        Assert.Equal(2, result.Projectiles!.Count);
    }

    [Fact]
    public void TheBowOverrideReceivesTheArrowItsCallerJustSpawned()
    {
        const string arrow = "/Game/BloodlinesAlpha/Weapons/Arrow.Arrow_C";
        var graph = new ScriptFunction(
            "/Game/Invented/Ranged",
            Owner,
            "ExecuteUbergraph_Ranged",
            false,
            [
                Assign(
                    0,
                    "cast",
                    new JsonObject
                    {
                        ["Token"] = "EX_DynamicCast",
                        ["ClassPtr"] = new JsonObject { ["$object"] = arrow },
                        ["Target"] = Variable("arrow"),
                    }
                ),
                Assign(
                    10,
                    "valid",
                    new JsonObject
                    {
                        ["Token"] = "EX_Cast",
                        ["ConversionType"] = "CST_ObjectToBool2",
                        ["Target"] = Variable("cast"),
                    }
                ),
                Node(
                    20,
                    "EX_JumpIfNot",
                    ("BooleanExpression", Variable("valid")),
                    ("CodeOffset", JsonValue.Create(60)!)
                ),
                Spawn(30),
                Node(40, "EX_Return"),
                Node(60, "EX_Return"),
            ]
        );
        var wrapper = new ScriptFunction(
            "/Game/Invented/Ranged",
            Owner,
            "Custom Shoot Bow Event",
            false,
            [
                Node(
                    0,
                    "EX_LetValueOnPersistentFrame",
                    ("DestinationProperty", Variable("arrow")["Variable"]!.DeepClone()),
                    ("AssignmentExpression", Variable("input"))
                ),
                Node(
                    18,
                    "EX_LocalFinalFunction",
                    ("StackNode", new JsonObject { ["$object"] = graph.Path }),
                    (
                        "Parameters",
                        new JsonArray(new JsonObject { ["Token"] = "EX_IntConst", ["Value"] = 0 })
                    )
                ),
                Node(33, "EX_Return"),
            ]
        );
        var result = RangedFireAnalysis.Analyze(
            wrapper,
            new Dictionary<string, ScriptFunction>
            {
                [wrapper.Path] = wrapper,
                [graph.Path] = graph,
            },
            new JsonArray(graph.Statements.Select(statement => statement.DeepClone()).ToArray())
        );
        Assert.Single(result.Projectiles!);
    }

    [Fact]
    public void ChargeFormulaUsesItemDamageAndTheAuthoredMultiplier()
    {
        JsonObject ItemDamage() =>
            new()
            {
                ["Token"] = "EX_StructMemberContext",
                ["Property"] = new JsonObject
                {
                    ["$field"] = "/Game/Invented/ItemInfo.ItemInfo:Damage",
                },
                ["StructExpression"] = new JsonObject
                {
                    ["Token"] = "EX_InstanceVariable",
                    ["Variable"] = new JsonObject { ["$field"] = Owner + ":ItemInfo" },
                },
            };
        var result = Analyze(
            Spawn(0),
            Assign(
                10,
                "damage",
                Call(
                    "Add_FloatFloat",
                    ItemDamage(),
                    Call("Multiply_IntFloat", ItemDamage(), Variable("Multiplier"))
                )
            ),
            Node(
                20,
                "EX_CallMath",
                (
                    "StackNode",
                    new JsonObject
                    {
                        ["$object"] = "/Script/Engine.KismetSystemLibrary:SetFloatPropertyByName",
                    }
                ),
                (
                    "Parameters",
                    new JsonArray(
                        Variable("projectile"),
                        new JsonObject { ["Token"] = "EX_NameConst", ["Value"] = "Damage" },
                        Variable("damage")
                    )
                )
            ),
            Node(30, "EX_Return")
        );
        var damage = Assert.Single(result.Projectiles!).Damage!;
        Assert.Equal(85, damage.At(85, 100));
        Assert.Equal(110.5, damage.At(85, 100, 0.3));
    }
}
