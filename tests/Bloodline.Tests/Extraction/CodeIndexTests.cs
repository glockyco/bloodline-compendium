using System.Text.Json.Nodes;
using Bloodline.Code;

namespace Bloodline.Tests.Extraction;

public sealed class CodeIndexTests
{
    private const string Package = "/Game/Invented/Panel";
    private const string Owner = Package + ".Panel_C";
    private const string Table = "/Game/Invented/Equipment.Equipment";
    private const string Struct = "/Game/Invented/Protection.Protection";

    private static JsonObject Node(
        string token,
        int offset,
        params (string Key, JsonNode? Value)[] fields
    )
    {
        var node = new JsonObject { ["Token"] = token, ["StatementIndex"] = offset };
        foreach (var field in fields)
            node[field.Key] = field.Value;
        return node;
    }

    private static JsonObject Constant(int value) =>
        new() { ["Token"] = "EX_IntConst", ["Value"] = value };

    private static JsonObject Object(string value) => new() { ["$object"] = value };

    private static JsonObject Field(string value) => new() { ["$field"] = value };

    private static JsonObject Read(int offset, string field) =>
        Node(
            "EX_StructMemberContext",
            offset,
            ("Property", Field(Struct + ":" + field)),
            ("StructExpression", Node("EX_Self", offset + 1))
        );

    private static ScriptFunction Function(string name, params JsonObject[] nodes) =>
        new(Package, Owner, name, true, nodes);

    private static CodeFunction Analyze(ScriptFunction function, params ScriptFunction[] others)
    {
        var functions = others.Append(function).ToDictionary(function => function.Path);
        return new FunctionAnalysis(
            functions,
            new HashSet<string> { Table },
            new HashSet<string> { Struct },
            new HashSet<string> { Struct }
        ).Analyze(function);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NormalProjectileDamageFollowsTheReachableSpawnAndItsAliases(bool namedWrite)
    {
        JsonObject Local(string name) =>
            new()
            {
                ["Token"] = "EX_LocalVariable",
                ["Variable"] = Field(Owner + ":Event:" + name),
            };
        var spawn = Node(
            "EX_LetObj",
            0,
            ("Variable", Local("spawn")),
            (
                "Assignment",
                new JsonObject
                {
                    ["Token"] = "EX_CallMath",
                    ["StackNode"] = Object(
                        "/Script/Engine.GameplayStatics:BeginDeferredActorSpawnFromClass"
                    ),
                    ["Parameters"] = new JsonArray(
                        new JsonObject { ["Token"] = "EX_Self" },
                        new JsonObject
                        {
                            ["Token"] = "EX_ObjectConst",
                            ["Value"] = Object("/Game/Projectile.Projectile_C"),
                        }
                    ),
                }
            )
        );
        var finish = Node(
            "EX_LetObj",
            10,
            ("Variable", Local("finished")),
            (
                "Assignment",
                new JsonObject
                {
                    ["Token"] = "EX_CallMath",
                    ["StackNode"] = Object("/Script/Engine.GameplayStatics:FinishSpawningActor"),
                    ["Parameters"] = new JsonArray(Local("spawn")),
                }
            )
        );
        var write = namedWrite
            ? Node(
                "EX_CallMath",
                20,
                ("StackNode", Object("/Script/Engine.KismetSystemLibrary:SetFloatPropertyByName")),
                (
                    "Parameters",
                    new JsonArray(
                        Local("spawn"),
                        new JsonObject { ["Token"] = "EX_NameConst", ["Value"] = "Damage" },
                        Constant(20)
                    )
                )
            )
            : Node(
                "EX_Let",
                20,
                (
                    "Variable",
                    new JsonObject
                    {
                        ["Token"] = "EX_Context",
                        ["ObjectExpression"] = Local("finished"),
                        ["ContextExpression"] = new JsonObject
                        {
                            ["Token"] = "EX_InstanceVariable",
                            ["Variable"] = Field("/Game/Projectile.Projectile_C:Damage"),
                        },
                    }
                ),
                ("Assignment", Constant(20))
            );
        var function = Function(
            "Staff Attack",
            spawn,
            finish,
            write,
            Node("EX_Return", 30),
            Node("EX_Let", 40, ("Assignment", Constant(250))),
            Node("EX_Return", 50)
        );
        Assert.Equal(
            new SpawnedDamage("/Game/Projectile.Projectile_C", true, 20),
            Assert.Single(Analyze(function).SpawnedDamage!)
        );
    }

    [Fact]
    public void ANameAssignedAfterAClassComparisonMapsThatClassToTheName()
    {
        JsonObject Compare(int offset, string classPath) =>
            Node(
                "EX_LetBool",
                offset,
                (
                    "Expression",
                    Node(
                        "EX_CallMath",
                        offset + 1,
                        (
                            "StackNode",
                            Object("/Script/Engine.KismetMathLibrary:EqualEqual_ClassClass")
                        ),
                        (
                            "Parameters",
                            new JsonArray(
                                Node("EX_LocalVariable", offset + 2),
                                new JsonObject
                                {
                                    ["Token"] = "EX_ObjectConst",
                                    ["Value"] = Object(classPath),
                                }
                            )
                        )
                    )
                )
            );
        JsonObject Assign(int offset, string name) =>
            Node(
                "EX_Let",
                offset,
                ("Expression", new JsonObject { ["Token"] = "EX_NameConst", ["Value"] = name })
            );
        var function = Function(
            "Get Correct Item",
            Compare(0, "/Game/Invented/IronIngot.IronIngot_C"),
            Node("EX_JumpIfNot", 10, ("CodeOffset", 30)),
            Assign(20, "IronIngot"),
            Compare(30, "/Game/Invented/Moonstone.Moonstone_C"),
            Compare(40, "/Game/Invented/Moonstone.Moonstone_C"),
            Assign(50, "MoonstoneIngot"),
            // A name without a comparison before it maps no class.
            Assign(60, "Stray"),
            Node("EX_Return", 70)
        );
        Assert.Equal(
            [
                new ClassName("/Game/Invented/IronIngot.IronIngot_C", "IronIngot"),
                new ClassName("/Game/Invented/Moonstone.Moonstone_C", "MoonstoneIngot"),
            ],
            FunctionAnalysis.ClassNamesOf(function)
        );
    }

    [Fact]
    public void EventAttributionFollowsFlowStackBranchesAndLatentResumesWithoutOtherEvents()
    {
        var graph = Function(
            "ExecuteUbergraph_Panel",
            Node("EX_ComputedJump", 0, ("CodeOffsetExpression", Node("EX_LocalVariable", 1))),
            Node("EX_PushExecutionFlow", 10, ("PushingAddress", JsonValue.Create(70))),
            Node(
                "EX_JumpIfNot",
                20,
                ("CodeOffset", JsonValue.Create(50)),
                ("BooleanExpression", Node("EX_True", 21))
            ),
            Node("EX_ObjectConst", 30, ("Value", Object(Table))),
            Node("EX_PopExecutionFlow", 40),
            Read(50, "Guard"),
            Node("EX_PopExecutionFlowIfNot", 60, ("BooleanExpression", Node("EX_False", 61))),
            Node(
                "EX_CallMath",
                70,
                ("StackNode", Object("/Script/Engine.KismetSystemLibrary:Delay")),
                (
                    "Parameters",
                    new JsonArray(
                        Node(
                            "EX_StructConst",
                            71,
                            ("Struct", Object("/Script/Engine.LatentActionInfo")),
                            (
                                "Properties",
                                new JsonArray(
                                    new JsonObject
                                    {
                                        ["Token"] = "EX_SkipOffsetConst",
                                        ["Value"] = 100,
                                    },
                                    Constant(1234),
                                    new JsonObject
                                    {
                                        ["Token"] = "EX_NameConst",
                                        ["Value"] = "ExecuteUbergraph_Panel",
                                    },
                                    Node("EX_Self", 72)
                                )
                            )
                        )
                    )
                )
            ),
            Node("EX_Return", 80),
            Read(90, "Unrelated"),
            Node("EX_Let", 100, ("Variable", Read(101, "AfterDelay")), ("Assignment", Constant(2))),
            Node("EX_Return", 110),
            Node("EX_EndOfScript", 120)
        );
        var entry = Function(
            "Hover",
            Node(
                "EX_LocalFinalFunction",
                0,
                ("StackNode", Object(Owner + ":ExecuteUbergraph_Panel")),
                ("Parameters", new JsonArray(Constant(10)))
            ),
            Node("EX_Return", 15)
        );
        var result = Analyze(entry, graph);
        Assert.Equal(CodeParseStatus.Complete, result.ParseStatus);
        Assert.Contains(
            result.Reads,
            target => target.Path == Table && target.Kind == CodeTargetKind.DataTable
        );
        Assert.Contains(result.Reads, target => target.Label == "Protection.Guard");
        Assert.Contains(result.Writes, target => target.Label == "Protection.AfterDelay");
        Assert.DoesNotContain(
            result.Reads.Concat(result.Writes),
            target => target.Label == "Protection.Unrelated"
        );
        Assert.Contains("/Script/Engine:Delay", result.Calls);
        var index = CodeIndex.Create([result]);
        Assert.Contains(
            index.References,
            reference =>
                reference.Target.Path == Table && reference.Readers.Contains(Package + ":Hover")
        );
    }

    private static CodeFunction FingerprintFixture(int shift, int value, bool unsupported = false)
    {
        var graph = Function(
            "ExecuteUbergraph_Panel",
            Node("EX_ComputedJump", 0),
            Node("EX_Jump", 10 + shift, ("CodeOffset", JsonValue.Create(30 + shift))),
            Read(20 + shift, "Unrelated"),
            Node(
                "EX_Let",
                30 + shift,
                ("Variable", Read(31 + shift, "Guard")),
                (
                    "Assignment",
                    unsupported ? Node("UNSUPPORTED_EX_VectorConst", 32 + shift) : Constant(value)
                )
            ),
            Node("EX_Return", 40 + shift),
            Node("EX_EndOfScript", 50 + shift)
        );
        var entry = Function(
            "Equip",
            Node(
                "EX_LocalFinalFunction",
                0,
                ("StackNode", Object(Owner + ":ExecuteUbergraph_Panel")),
                ("Parameters", new JsonArray(Constant(10 + shift)))
            ),
            Node("EX_Return", 15)
        );
        return Analyze(entry, graph);
    }

    [Fact]
    public void FingerprintsIgnoreUnrelatedEventOffsetChangesButDetectChangedCode()
    {
        Assert.Equal(FingerprintFixture(0, 5).Fingerprint, FingerprintFixture(0, 5).Fingerprint);
        Assert.Equal(FingerprintFixture(0, 5).Fingerprint, FingerprintFixture(200, 5).Fingerprint);
        Assert.NotEqual(FingerprintFixture(0, 5).Fingerprint, FingerprintFixture(0, 6).Fingerprint);
    }

    [Fact]
    public void ReachedUnsupportedExpressionMakesTheEventPartial()
    {
        var result = FingerprintFixture(0, 5, unsupported: true);
        Assert.Equal(CodeParseStatus.Partial, result.ParseStatus);
        Assert.Contains(
            result.Errors,
            error => error.Contains("UNSUPPORTED_EX_VectorConst", StringComparison.Ordinal)
        );
    }

    [Fact]
    public void UnreadJumpDestinationMakesTheEventPartial()
    {
        var result = Analyze(
            Function("Read", Node("EX_Jump", 0, ("CodeOffset", JsonValue.Create(100))))
        );
        Assert.Equal(CodeParseStatus.Partial, result.ParseStatus);
    }

    [Fact]
    public void ParsedVectorConstantsKeepTheirComponentsInTheFingerprint()
    {
        CodeFunction Vector(float x)
        {
            using var bytes = new MemoryStream();
            using var writer = new BinaryWriter(bytes);
            writer.Write((byte)CUE4Parse.UE4.Kismet.EExprToken.EX_VectorConst);
            writer.Write(x);
            writer.Write(2f);
            writer.Write(3f);
            using var archive = new CUE4Parse.UE4.Assets.Readers.FKismetArchive(
                "invented",
                bytes.ToArray(),
                null!,
                new CUE4Parse.UE4.Versions.VersionContainer(
                    CUE4Parse.UE4.Versions.EGame.GAME_UE4_27
                )
            );
            return Analyze(
                Function(
                    "Vector",
                    ScriptExpressions.Encode(archive.ReadExpression()),
                    Node("EX_Return", 13)
                )
            );
        }
        Assert.Equal(CodeParseStatus.Complete, Vector(1).ParseStatus);
        Assert.Equal(Vector(1).Fingerprint, Vector(1).Fingerprint);
        Assert.NotEqual(Vector(1).Fingerprint, Vector(4).Fingerprint);
    }

    [Fact]
    public void ArrayElementTypeMetadataIsNotAFieldRead()
    {
        var result = Analyze(
            Function(
                "Construct",
                Node(
                    "EX_ArrayConst",
                    0,
                    ("InnerProperty", Field(Struct + ":Element.Element")),
                    ("Elements", new JsonArray())
                ),
                Node("EX_Return", 10)
            )
        );
        Assert.Empty(result.Reads);
    }

    [Fact]
    public void FunctionLocalsStayInCodeButNotInTheStateCrossReference()
    {
        var variable = Node(
            "EX_LocalVariable",
            0,
            ("Variable", Field(Owner + ":Inspect:Parameter"))
        );
        var plain = Analyze(
            Function("Inspect", variable, Read(10, "Guard"), Node("EX_Return", 20))
        );
        variable["Variable"]!["$local"] = true;
        var indexed = Analyze(
            Function("Inspect", variable, Read(10, "Guard"), Node("EX_Return", 20))
        );
        Assert.Equal(plain.Fingerprint, indexed.Fingerprint);
        Assert.DoesNotContain(
            indexed.Reads.Concat(indexed.Writes),
            target => target.Path.Contains(":Inspect:", StringComparison.Ordinal)
        );
        Assert.Contains(indexed.Reads, target => target.Label == "Protection.Guard");
    }
}
