using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Bloodline.Output;
using static Bloodline.Code.ScriptExpressions;

namespace Bloodline.Code;

internal sealed record ScriptFunction(
    string Package,
    string Owner,
    string Name,
    bool Widget,
    IReadOnlyList<JsonObject> Statements
)
{
    public string Path => Owner + ":" + Name;
}

internal sealed class FunctionAnalysis(
    IReadOnlyDictionary<string, ScriptFunction> functions,
    IReadOnlySet<string> tables,
    IReadOnlySet<string> rowStructs,
    IReadOnlySet<string> structs
)
{
    public CodeFunction Analyze(ScriptFunction function)
    {
        var errors = new HashSet<string>(StringComparer.Ordinal);
        var reads = new HashSet<CodeTarget>();
        var writes = new HashSet<CodeTarget>();
        var calls = new HashSet<string>(StringComparer.Ordinal);
        var active = new HashSet<string>(StringComparer.Ordinal);
        var body = Graph(
            function,
            function.Statements.FirstOrDefault() is { } first
                ? Integer(first["StatementIndex"]) ?? 0
                : 0
        );
        return new CodeFunction(
            function.Package,
            function.Owner,
            function.Name,
            function.Widget,
            Convert.ToHexStringLower(
                SHA256.HashData(Encoding.UTF8.GetBytes(CanonicalJson.Serialize(body)))
            ),
            errors.Count == 0 ? CodeParseStatus.Complete : CodeParseStatus.Partial,
            reads.OrderBy(target => target.Path, StringComparer.Ordinal).ToArray(),
            writes.OrderBy(target => target.Path, StringComparer.Ordinal).ToArray(),
            calls.Order(StringComparer.Ordinal).ToArray(),
            errors.Order(StringComparer.Ordinal).ToArray()
        )
        {
            ClassNames = ClassNamesOf(function),
            SpawnedDamage = function.Name is "Staff Attack" or "Swing Event"
                ? SpawnDamage(body)
                : null,
        };

        JsonArray Graph(ScriptFunction source, int entry)
        {
            var key = source.Path + "[" + entry + "]";
            if (!active.Add(key))
            {
                errors.Add("Recursive ubergraph entry: " + source.Path);
                return [];
            }
            try
            {
                if (source.Statements.Count == 0)
                    return [];
                var statements = source.Statements.ToDictionary(node =>
                    Integer(node["StatementIndex"]) ?? 0
                );
                var next = source
                    .Statements.Select(
                        (node, index) =>
                            (
                                Offset: Integer(node["StatementIndex"]) ?? 0,
                                Next: index + 1 < source.Statements.Count
                                    ? Integer(source.Statements[index + 1]["StatementIndex"])
                                    : null
                            )
                    )
                    .ToDictionary(pair => pair.Offset, pair => pair.Next);
                var reached = new List<int>();
                var positions = new Dictionary<int, int>();
                var edges = new Dictionary<int, List<int>>();
                var states = new HashSet<string>(StringComparer.Ordinal);
                var pending = new Stack<(int Offset, int[] Flow)>();
                pending.Push((entry, []));
                while (pending.TryPop(out var state))
                {
                    if (!states.Add(state.Offset + ":" + string.Join(",", state.Flow)))
                        continue;
                    if (
                        state.Flow.Length > source.Statements.Count
                        || states.Count > source.Statements.Count * 64
                    )
                    {
                        errors.Add("Execution-flow stack cannot be bounded: " + source.Path);
                        break;
                    }
                    if (!statements.TryGetValue(state.Offset, out var statement))
                    {
                        errors.Add(
                            "Unread execution target in " + source.Path + ": " + state.Offset
                        );
                        continue;
                    }
                    if (positions.TryAdd(state.Offset, reached.Count))
                        reached.Add(state.Offset);
                    edges.TryAdd(state.Offset, []);
                    var successors = new List<(int Offset, int[] Flow)>();
                    void Target(int? offset, int[] flow)
                    {
                        if (offset is { } address)
                        {
                            successors.Add((address, flow));
                            if (!edges[state.Offset].Contains(address))
                                edges[state.Offset].Add(address);
                        }
                        else
                            errors.Add("Code ends without a return in " + source.Path);
                    }
                    void Fallthrough() => Target(next[state.Offset], state.Flow);
                    void Pop()
                    {
                        if (state.Flow.Length > 0)
                            Target(state.Flow[^1], state.Flow[..^1]);
                    }
                    switch (Text(statement["Token"]))
                    {
                        case "EX_Return":
                        case "EX_EndOfScript":
                            break;
                        case "EX_Jump":
                            Target(Integer(statement["CodeOffset"]), state.Flow);
                            break;
                        case "EX_JumpIfNot":
                            Fallthrough();
                            Target(Integer(statement["CodeOffset"]), state.Flow);
                            break;
                        case "EX_ComputedJump":
                            if (
                                Integer(statement["CodeOffsetExpression"]?["Value"]) is { } computed
                            )
                                Target(computed, state.Flow);
                            else
                                errors.Add("Unresolved computed entry in " + source.Path);
                            break;
                        case "EX_PushExecutionFlow":
                            if (Integer(statement["PushingAddress"]) is { } pushed)
                                Target(next[state.Offset], [.. state.Flow, pushed]);
                            else
                                errors.Add("Unread execution-flow address in " + source.Path);
                            break;
                        case "EX_PopExecutionFlow":
                            Pop();
                            break;
                        case "EX_PopExecutionFlowIfNot":
                            Fallthrough();
                            Pop();
                            break;
                        default:
                            Fallthrough();
                            break;
                    }
                    foreach (var latent in Walk(statement).Where(IsLatent))
                    {
                        var properties = latent["Properties"]?.AsArray();
                        var resume = Integer(properties?.FirstOrDefault()?["Value"]);
                        var resumeName = Text(properties?.ElementAtOrDefault(2)?["Value"]);
                        if (resumeName == source.Name && resume is >= 0)
                            Target(resume, []);
                        else if (resume is not -1)
                            errors.Add("Unresolved latent resume in " + source.Path);
                    }
                    for (var index = successors.Count - 1; index >= 0; index--)
                        pending.Push(successors[index]);
                }
                // Nested expression offsets matter to switch and skip operands too. Their preorder
                // positions replace byte addresses so an unrelated event cannot change the hash.
                var expressionPositions = new Dictionary<int, int>();
                foreach (var offset in reached)
                {
                    foreach (
                        var expression in Walk(statements[offset])
                            .Where(node => node.ContainsKey("Token"))
                    )
                    {
                        if (Integer(expression["StatementIndex"]) is { } address)
                            expressionPositions.TryAdd(address, expressionPositions.Count);
                    }
                }
                var latentIds = new Dictionary<int, int>();
                JsonNode? Normalize(JsonNode? node)
                {
                    if (node is JsonArray array)
                        return new JsonArray(array.Select(Normalize).ToArray());
                    if (node is not JsonObject obj)
                        return node?.DeepClone();
                    var normalized = new JsonObject();
                    foreach (var (name, value) in obj)
                    {
                        if (name is "StatementIndex" or "$local")
                            continue;
                        if (
                            name == "Offset"
                            && Text(obj["Token"])
                                is "EX_Context"
                                    or "EX_Context_FailSilent"
                                    or "EX_ClassContext"
                        )
                        {
                            normalized[name] = "context-end";
                        }
                        else if (
                            name is "CodeOffset" or "PushingAddress"
                            || (name == "Value" && Text(obj["Token"]) == "EX_SkipOffsetConst")
                        )
                        {
                            normalized[name] =
                                Integer(value) is { } address
                                && expressionPositions.TryGetValue(address, out var position)
                                    ? JsonValue.Create(position)
                                    : JsonValue.Create("outside-entry");
                        }
                        else if (name is "NextOffset" or "EndGotoOffset")
                            normalized[name] = "expression-end";
                        else
                            normalized[name] = Normalize(value);
                    }
                    if (
                        IsLatent(obj)
                        && obj["Properties"] is JsonArray latent
                        && latent.Count >= 2
                        && Integer(latent[1]?["Value"]) is { } uuid
                    )
                    {
                        latentIds.TryAdd(uuid, latentIds.Count);
                        normalized["Properties"]![1]!["Value"] = latentIds[uuid];
                    }
                    if (Entry(obj) is { } target)
                    {
                        normalized["Parameters"] = new JsonArray("entry");
                        if (functions.TryGetValue(target.Path, out var called))
                            normalized["ReachedCode"] = Graph(called, target.Offset);
                        else
                            errors.Add("Missing ubergraph: " + target.Path);
                    }
                    return normalized;
                }
                var result = new JsonArray();
                foreach (var offset in reached)
                {
                    Scan(statements[offset], reads, writes, calls);
                    foreach (var expression in Walk(statements[offset]))
                    {
                        if (
                            Text(expression["Token"]) is { } token
                            && (
                                token.StartsWith("UNSUPPORTED", StringComparison.Ordinal)
                                || token == "EX_Unknown"
                            )
                        )
                            errors.Add("Unsupported token in " + source.Path + ": " + token);
                    }
                    result.Add(
                        new JsonObject
                        {
                            ["Expression"] = Normalize(statements[offset]),
                            ["Next"] = new JsonArray(
                                edges[offset]
                                    .Select(address =>
                                        (JsonNode?)
                                            JsonValue.Create(
                                                positions.GetValueOrDefault(address, -1)
                                            )
                                    )
                                    .ToArray()
                            ),
                        }
                    );
                }
                return result;
            }
            finally
            {
                active.Remove(key);
            }
        }
    }

    private static IReadOnlyList<SpawnedDamage> SpawnDamage(JsonNode body)
    {
        var actors = new List<SpawnedDamage>();
        var aliases = new Dictionary<string, int>(StringComparer.Ordinal);
        static string? Field(JsonNode? node) => Text(node?["Variable"]?["$field"]);
        static double? Literal(JsonNode? node) =>
            Text(node?["Token"]) is "EX_FloatConst" or "EX_DoubleConst" or "EX_IntConst"
                ? Bloodline.Records.RecordValues.Number(node?["Value"])
                : null;
        foreach (var node in Walk(body))
        {
            var call = node["Assignment"];
            var called = Text(call?["StackNode"]?["$object"]);
            var parameters = call?["Parameters"] as JsonArray;
            var variable = Field(node["Variable"]);
            if (
                called?.EndsWith(":BeginDeferredActorSpawnFromClass", StringComparison.Ordinal)
                    == true
                && variable is not null
                && Text(parameters?.ElementAtOrDefault(1)?["Value"]?["$object"]) is { } actor
            )
            {
                aliases[variable] = actors.Count;
                actors.Add(new SpawnedDamage(actor, false, null));
            }
            else if (
                called?.EndsWith(":FinishSpawningActor", StringComparison.Ordinal) == true
                && variable is not null
                && Field(parameters?.FirstOrDefault()) is { } source
                && aliases.TryGetValue(source, out var index)
            )
            {
                aliases[variable] = index;
            }
            if (
                Text(node["Token"]) == "EX_Let"
                && Text(node["Variable"]?["Token"]) == "EX_Context"
                && Text(node["Variable"]?["ContextExpression"]?["Variable"]?["$field"])
                    ?.EndsWith(":Damage", StringComparison.Ordinal) == true
                && Field(node["Variable"]?["ObjectExpression"]) is { } destination
                && aliases.TryGetValue(destination, out var target)
            )
            {
                Assign(target, Literal(node["Assignment"]));
            }
            if (
                Text(node["StackNode"]?["$object"])
                    ?.EndsWith(":SetFloatPropertyByName", StringComparison.Ordinal) == true
                && node["Parameters"] is JsonArray arguments
                && Text(arguments.ElementAtOrDefault(1)?["Value"]) == "Damage"
                && Field(arguments.FirstOrDefault()) is { } receiver
                && aliases.TryGetValue(receiver, out var namedTarget)
            )
            {
                Assign(namedTarget, Literal(arguments.ElementAtOrDefault(2)));
            }
        }
        return actors;

        void Assign(int index, double? value)
        {
            var actor = actors[index];
            actors[index] = actor with
            {
                Assigned = true,
                Damage = !actor.Assigned || actor.Damage == value ? value : null,
            };
        }
    }

    private static bool IsLatent(JsonObject obj) =>
        Text(obj["Token"]) == "EX_StructConst"
        && Text(obj["Struct"]?["$object"])?.EndsWith(".LatentActionInfo", StringComparison.Ordinal)
            == true;

    private static (string Path, int Offset)? Entry(JsonObject expression)
    {
        var path = Text(expression["StackNode"]?["$object"]);
        if (path?.Split(':')[^1].StartsWith("ExecuteUbergraph_", StringComparison.Ordinal) != true)
            return null;
        return
            expression["Parameters"] is JsonArray { Count: 1 } parameters
            && Integer(parameters[0]?["Value"]) is { } offset
            ? (path, offset)
            : null;
    }

    public static string Citation(string path) => path.Split('.')[0] + ":" + path.Split(':')[^1];

    /// <summary>
    /// The names a function assigns after it compares a value with a class constant through
    /// <c>EqualEqual_ClassClass</c>, as a chain of such comparisons maps classes to row names.
    /// </summary>
    internal static IReadOnlyList<ClassName> ClassNamesOf(ScriptFunction function)
    {
        var result = new List<ClassName>();
        string? compared = null;
        foreach (var statement in function.Statements)
        {
            // Field names differ between expression types, so the statement's whole tree is searched.
            var nodes = ScriptExpressions.Walk(statement).ToArray();
            if (
                nodes.Any(node =>
                    Text(node["StackNode"]?["$object"])
                        ?.EndsWith(":EqualEqual_ClassClass", StringComparison.Ordinal) == true
                )
            )
            {
                compared =
                    nodes
                        .Where(node => Text(node["Token"]) == "EX_ObjectConst")
                        .Select(node => Text(node["Value"]?["$object"]))
                        .FirstOrDefault(path => path is not null)
                    ?? compared;
            }
            else if (
                compared is not null
                && Text(statement["Token"]) is "EX_Let" or "EX_LetValueOnPersistentFrame"
                && nodes.FirstOrDefault(node => Text(node["Token"]) == "EX_NameConst")
                    is { } constant
                && Text(constant["Value"]) is { } name
            )
            {
                result.Add(new ClassName(compared, name));
                compared = null;
            }
        }
        return result;
    }

    private void Scan(
        JsonNode? node,
        HashSet<CodeTarget> reads,
        HashSet<CodeTarget> writes,
        HashSet<string> calls,
        bool write = false
    )
    {
        if (node is JsonArray array)
        {
            foreach (var child in array)
                Scan(child, reads, writes, calls, write);
            return;
        }
        if (node is not JsonObject obj)
            return;
        if (Text(obj["$field"]) is { } field)
        {
            if (obj["$local"]?.GetValue<bool>() == true)
                return;
            var owner = field.Split(':')[0];
            var target = new CodeTarget(
                structs.Contains(owner) || Text(obj["$fieldKind"]) == "struct"
                    ? CodeTargetKind.StructField
                    : CodeTargetKind.Property,
                field
            );
            (write ? writes : reads).Add(target);
            return;
        }
        if (Text(obj["$object"]) is { } path)
        {
            reads.Add(
                new CodeTarget(
                    tables.Contains(path) ? CodeTargetKind.DataTable
                        : rowStructs.Contains(path) ? CodeTargetKind.RowStruct
                        : CodeTargetKind.Object,
                    path
                )
            );
            return;
        }
        var token = Text(obj["Token"]);
        if (
            token
            is "EX_FinalFunction"
                or "EX_LocalFinalFunction"
                or "EX_CallMath"
                or "EX_CallMulticastDelegate"
        )
        {
            if (Text(obj["StackNode"]?["$object"]) is { } called)
                calls.Add(Citation(called));
        }
        else if (
            token
            is "EX_VirtualFunction"
                or "EX_LocalVirtualFunction"
                or "EX_InstanceDelegate"
                or "EX_BindDelegate"
        )
        {
            var name = Text(obj["VirtualFunctionName"]) ?? Text(obj["FunctionName"]);
            if (name is not null)
                calls.Add("virtual:" + name);
        }
        foreach (var (key, child) in obj)
        {
            if (
                key
                    is "StackNode"
                        or "RValuePointer"
                        or "InnerProperty"
                        or "KeyProperty"
                        or "ValueProperty"
                        or "ArrayInnerProp"
                || key == "Property" && token == "EX_Let"
            )
                continue;
            var destination =
                token?.StartsWith("EX_Let", StringComparison.Ordinal) == true
                    && key is "Variable" or "DestinationProperty"
                || token == "EX_SetArray" && key == "AssigningProperty"
                || token == "EX_SetMap" && key == "MapProperty"
                || token == "EX_SetSet" && key == "SetProperty"
                || token == "EX_StructMemberContext" && key == "Property" && write;
            Scan(
                child,
                reads,
                writes,
                calls,
                destination || write && token is not "EX_StructMemberContext"
            );
        }
    }
}
