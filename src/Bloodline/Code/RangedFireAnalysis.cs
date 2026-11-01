using System.Text.Json.Nodes;
using Bloodline.Extraction;
using Bloodline.Records;
using static Bloodline.Code.ScriptExpressions;

namespace Bloodline.Code;

internal sealed record DamageFormula(double Constant, double Item, double Weapon, double Charge)
{
    public double? At(double? item, double? weapon, double charge = 0) =>
        ((Item != 0 || Charge * charge != 0) && item is null) || (Weapon != 0 && weapon is null)
            ? null
            : Constant
                + Item * (item ?? 0)
                + Weapon * (weapon ?? 0)
                + Charge * (item ?? 0) * charge;
}

internal sealed record FiredProjectile(string Class, DamageFormula? Damage, bool Assigned);

internal sealed record RangedFire(
    IReadOnlyList<FiredProjectile>? Projectiles,
    bool ReplacesGeneric,
    double? MaximumCharge
)
{
    public IReadOnlyList<IReadOnlyList<FiredProjectile>>? Alternatives { get; init; }
    public bool DeferredCleanup { get; init; }
}

/// <summary>Executes the scalar assignments and bounded spawn loops of a weapon fire entry.</summary>
internal static class RangedFireAnalysis
{
    private sealed record Value(
        DamageFormula? Number = null,
        int? Actor = null,
        string? Class = null
    );

    private sealed class State
    {
        public Dictionary<string, Value> Values { get; init; } = new(StringComparer.Ordinal);
        public List<FiredProjectile> Projectiles { get; init; } = [];
        public int Steps { get; set; }
        public bool Unread { get; set; }
        public bool Delayed { get; set; }
        public bool? DeferredCleanup { get; set; }

        public State Copy() =>
            new()
            {
                Values = new(Values, StringComparer.Ordinal),
                Projectiles = [.. Projectiles],
                Steps = Steps,
                Unread = Unread,
                Delayed = Delayed,
                DeferredCleanup = DeferredCleanup,
            };
    }

    public static RangedFire Analyze(
        ScriptFunction function,
        IReadOnlyDictionary<string, ScriptFunction> functions,
        JsonNode body
    )
    {
        var source = function;
        var entry = function.Statements.FirstOrDefault() is { } first
            ? Integer(first["StatementIndex"]) ?? 0
            : 0;
        foreach (var node in function.Statements.SelectMany(Walk))
        {
            var name = Text(node["StackNode"]?["$object"]);
            if (
                name?.Split(':')[^1].StartsWith("ExecuteUbergraph_", StringComparison.Ordinal)
                    == true
                && functions.TryGetValue(name, out var graph)
                && Integer(node["Parameters"]?[0]?["Value"]) is { } offset
            )
            {
                source = graph;
                entry = offset;
                break;
            }
        }
        var statements = source.Statements.ToDictionary(s => Integer(s["StatementIndex"]) ?? 0);
        var next = source
            .Statements.Select(
                (s, i) =>
                    (
                        Offset: Integer(s["StatementIndex"]) ?? 0,
                        Next: i + 1 < source.Statements.Count
                            ? Integer(source.Statements[i + 1]["StatementIndex"])
                            : null
                    )
            )
            .ToDictionary(x => x.Offset, x => x.Next);
        var pending = new Stack<(int Offset, int[] Flow, State State)>();
        var initial = new State();
        // AnimNotify_Spawn Arrow and FirstPersonCharacter2 pass their newly spawned projectile to these overrides.
        var incoming =
            function.Name == "Custom Shoot Bow Event"
                ? "/Game/BloodlinesAlpha/Weapons/Arrow.Arrow_C"
            : function.Name == "Custom Fire Event"
                ? "/Game/BloodlinesAlpha/Weapons/CrossBowBolt.CrossBowBolt_C"
            : null;
        if (incoming is not null)
            foreach (var statement in function.Statements)
                if (Text(statement["DestinationProperty"]?["$field"]) is { } field)
                    initial.Values[field] = new Value(Class: incoming);
        pending.Push((entry, [], initial));
        var outcomes = new List<State>();
        var visited = 0;
        var replaces = Walk(body).Any(node => Called(node)?.Split(':')[^1] == "K2_DestroyActor");
        while (pending.TryPop(out var work))
        {
            var (offset, flow, state) = work;
            if (++visited > 20000 || ++state.Steps > 4000 || state.Projectiles.Count > 100)
                return new RangedFire(null, replaces, ChargeMaximum(source));
            if (!statements.TryGetValue(offset, out var statement))
                return new RangedFire(null, replaces, ChargeMaximum(source));
            var token = Text(statement["Token"]);
            void Continue(int? target, int[]? stack = null, State? branch = null)
            {
                if (target is { } address)
                    pending.Push((address, stack ?? flow, branch ?? state));
                else
                    outcomes.Add(state);
            }
            void Pop(State? branch = null)
            {
                if (flow.Length == 0)
                    outcomes.Add(branch ?? state);
                else
                    Continue(flow[^1], flow[..^1], branch);
            }
            switch (token)
            {
                case "EX_Return":
                case "EX_EndOfScript":
                    outcomes.Add(state);
                    break;
                case "EX_Jump":
                    Continue(Integer(statement["CodeOffset"]));
                    break;
                case "EX_PushExecutionFlow":
                    if (Integer(statement["PushingAddress"]) is not { } pushed)
                        return new RangedFire(null, replaces, ChargeMaximum(source));
                    Continue(next[offset], [.. flow, pushed]);
                    break;
                case "EX_PopExecutionFlow":
                    Pop();
                    break;
                case "EX_JumpIfNot":
                case "EX_PopExecutionFlowIfNot":
                    var expression = statement["BooleanExpression"];
                    var condition = Literal(Evaluate(expression, state));
                    JsonNode? instance = expression;
                    while (Text(instance?["Token"]) is "EX_Context" or "EX_Context_FailSilent")
                        instance = instance?["ContextExpression"];
                    var conditionField =
                        Text(instance?["Token"]) == "EX_InstanceVariable"
                            ? Text(instance?["Variable"]?["$field"])
                            : null;
                    if (condition != 0)
                    {
                        var positive = condition is null ? state.Copy() : state;
                        if (condition is null && conditionField is not null)
                            positive.Values[conditionField] = Number(1);
                        Continue(next[offset], branch: positive);
                    }
                    if (condition is null or 0)
                    {
                        if (condition is null && conditionField is not null)
                            state.Values[conditionField] = Number(0);
                        if (token == "EX_PopExecutionFlowIfNot")
                            Pop();
                        else
                            Continue(Integer(statement["CodeOffset"]));
                    }
                    break;
                case "EX_ComputedJump":
                    Continue(Integer(statement["CodeOffsetExpression"]?["Value"]));
                    break;
                default:
                    Evaluate(statement, state);
                    var latent = Walk(statement)
                        .FirstOrDefault(node =>
                            Text(node["Token"]) == "EX_StructConst"
                            && Text(node["Struct"]?["$object"])
                                ?.EndsWith(".LatentActionInfo", StringComparison.Ordinal) == true
                        );
                    if (
                        latent?["Properties"] is JsonArray properties
                        && Integer(properties.FirstOrDefault()?["Value"]) is >= 0 and var resume
                        && Text(properties.ElementAtOrDefault(2)?["Value"]) == source.Name
                    )
                    {
                        state.Delayed = true;
                        Continue(resume, []);
                    }
                    else
                        Continue(next[offset]);
                    break;
            }
        }
        var patterns = outcomes.Select(s => s.Projectiles).ToArray();
        var projectiles =
            outcomes.Any(s => s.Unread)
            || patterns.Length == 0
            || patterns.Any(p => !p.SequenceEqual(patterns[0]))
                ? null
                : patterns[0];
        return new RangedFire(projectiles, replaces, ChargeMaximum(source))
        {
            Alternatives =
                projectiles is null && patterns.Length > 0 && !outcomes.Any(state => state.Unread)
                    ? patterns
                    : null,
            DeferredCleanup =
                outcomes.Any(state => state.DeferredCleanup == true)
                || replaces && outcomes.Any(state => state.DeferredCleanup is null),
        };

        Value Evaluate(JsonNode? node, State state)
        {
            if (node is not JsonObject obj)
                return new Value();
            var kind = Text(obj["Token"]);
            if (kind is "EX_IntZero" or "EX_False")
                return Number(0);
            if (kind is "EX_IntOne" or "EX_True")
                return Number(1);
            if (
                kind
                is "EX_IntConst"
                    or "EX_IntConstByte"
                    or "EX_FloatConst"
                    or "EX_DoubleConst"
                    or "EX_ByteConst"
            )
                return RecordValues.Number(obj["Value"]) is { } number
                    ? Number(number)
                    : new Value();
            if (kind is "EX_LocalVariable" or "EX_InstanceVariable" or "EX_LocalOutVariable")
            {
                var field = Text(obj["Variable"]?["$field"]);
                if (field is null)
                    return new Value();
                if (FieldNames.StripSuffix(field.Split(':')[^1]) == "Multiplier")
                    return new Value(new DamageFormula(0, 0, 0, 1));
                // Swing Event is the normal attack, not a skill. PeasantsLongRangeRifle suppresses it during a skill.
                if (field.EndsWith(":Performing Skill", StringComparison.Ordinal))
                    return Number(0);
                return state.Values.GetValueOrDefault(field) ?? new Value();
            }
            if (kind == "EX_StructMemberContext")
            {
                var field = Text(obj["Property"]?["$field"]);
                if (field is not null && FieldNames.StripSuffix(field.Split(':')[^1]) == "Damage")
                {
                    var fields = Walk(obj)
                        .Select(n => Text(n["$field"]))
                        .Where(f => f is not null)
                        .ToArray();
                    if (fields.Any(f => FieldNames.StripSuffix(f!.Split(':')[^1]) == "ItemInfo"))
                        return new Value(new DamageFormula(0, 1, 0, 0));
                    if (fields.Any(f => FieldNames.StripSuffix(f!.Split(':')[^1]) == "WeaponInfo"))
                        return new Value(new DamageFormula(0, 0, 1, 0));
                }
                return new Value();
            }
            if (kind == "EX_DynamicCast")
            {
                var target = Evaluate(obj["Target"], state);
                var cast = Text(obj["ClassPtr"]?["$object"]);
                return target.Class == cast ? target : new Value();
            }
            if (
                kind == "EX_Cast"
                && Text(obj["ConversionType"])
                    ?.StartsWith("CST_ObjectToBool", StringComparison.Ordinal) == true
            )
            {
                var target = Evaluate(obj["Target"], state);
                return target.Actor is not null || target.Class is not null
                    ? Number(1)
                    : new Value();
            }
            if (kind is "EX_Context" or "EX_Context_FailSilent")
            {
                var operation = obj["ContextExpression"];
                if (
                    operation is not null
                    && Called(operation)?.Split(':')[^1] == "K2_DestroyActor"
                    && incoming is not null
                    && Evaluate(obj["ObjectExpression"], state) is { Actor: null } receiver
                    && receiver.Class == incoming
                )
                    state.DeferredCleanup ??= state.Delayed;
                return Evaluate(operation, state);
            }
            if (kind?.StartsWith("EX_Let", StringComparison.Ordinal) == true)
            {
                var value = Evaluate(obj["Assignment"] ?? obj["Expression"], state);
                var destination = obj["Variable"];
                var field = Text(destination?["Variable"]?["$field"]);
                if (field is not null)
                    state.Values[field] = value;
                if (
                    Text(destination?["Token"]) == "EX_Context"
                    && Text(destination?["ContextExpression"]?["Variable"]?["$field"])
                        ?.EndsWith(":Damage", StringComparison.Ordinal) == true
                    && Evaluate(destination?["ObjectExpression"], state).Actor is { } actor
                )
                    state.Projectiles[actor] = state.Projectiles[actor] with
                    {
                        Damage = value.Number,
                        Assigned = true,
                    };
                return value;
            }
            var called = Called(obj);
            if (called is not null)
            {
                var name = called.Split(':')[^1];
                var args = obj["Parameters"] as JsonArray;
                if (name == "BeginDeferredActorSpawnFromClass")
                {
                    if (
                        Text(args?.ElementAtOrDefault(1)?["Value"]?["$object"])
                        is not { } actorClass
                    )
                    {
                        state.Unread = true;
                        return new Value();
                    }
                    var index = state.Projectiles.Count;
                    state.Projectiles.Add(new FiredProjectile(actorClass, null, false));
                    return new Value(Actor: index, Class: actorClass);
                }
                if (name == "FinishSpawningActor")
                    return Evaluate(args?.FirstOrDefault(), state);
                if (name == "SetFloatPropertyByName")
                {
                    if (
                        Text(args?.ElementAtOrDefault(1)?["Value"]) == "Damage"
                        && Evaluate(args?.FirstOrDefault(), state).Actor is { } actor
                    )
                        state.Projectiles[actor] = state.Projectiles[actor] with
                        {
                            Damage = Evaluate(args?.ElementAtOrDefault(2), state).Number,
                            Assigned = true,
                        };
                    return new Value();
                }
                if (name == "K2_DestroyActor")
                    replaces = true;
                if (
                    name == "GetAllActorsOfClass"
                    && replaces
                    && Text(args?.ElementAtOrDefault(1)?["Value"]?["$object"])
                        is "/Game/BloodlinesAlpha/Weapons/Bullet.Bullet_C"
                            or "/Game/BloodlinesAlpha/Weapons/Arrow.Arrow_C"
                            or "/Game/BloodlinesAlpha/Weapons/CrossBowBolt.CrossBowBolt_C"
                )
                    state.DeferredCleanup ??= state.Delayed;
                // RustyBlunderbuss:Swing Event and Magazine-fedRifle2:Swing Event clear existing bullets first.
                // The cleanup array changes no replacement spawn, so its loop can be evaluated as empty.
                if (
                    name == "Array_Length"
                    && replaces
                    && Text(args?.FirstOrDefault()?["Variable"]?["$field"])
                        ?.Contains("GetAllActorsOfClass_OutActors", StringComparison.Ordinal)
                        == true
                )
                    return Number(0);
                var values = args?.Select(a => Evaluate(a, state)).ToArray() ?? [];
                var a = values.ElementAtOrDefault(0)?.Number;
                var b = values.ElementAtOrDefault(1)?.Number;
                if (name.StartsWith("Conv_", StringComparison.Ordinal))
                    return values.FirstOrDefault() ?? new Value();
                if (
                    name.StartsWith("Add_", StringComparison.Ordinal)
                    && a is not null
                    && b is not null
                )
                    return new Value(
                        new(
                            a.Constant + b.Constant,
                            a.Item + b.Item,
                            a.Weapon + b.Weapon,
                            a.Charge + b.Charge
                        )
                    );
                if (
                    name.StartsWith("Multiply_", StringComparison.Ordinal)
                    && a is not null
                    && b is not null
                )
                {
                    if (Literal(new Value(a)) is { } factor)
                        return Scale(b, factor);
                    if (Literal(new Value(b)) is { } other)
                        return Scale(a, other);
                    if (a == new DamageFormula(0, 1, 0, 0) && b == new DamageFormula(0, 0, 0, 1))
                        return new Value(b);
                }
                var left = Literal(values.ElementAtOrDefault(0));
                var right = Literal(values.ElementAtOrDefault(1));
                if (
                    name == "Divide_FloatFloat"
                    && a is not null
                    && right is { } divisor
                    && divisor != 0
                )
                    return Scale(a, 1 / divisor);
                if (left is { } l && right is { } r)
                {
                    if (name.StartsWith("LessEqual_", StringComparison.Ordinal))
                        return Number(l <= r ? 1 : 0);
                    if (name.StartsWith("Less_", StringComparison.Ordinal))
                        return Number(l < r ? 1 : 0);
                    if (name.StartsWith("EqualEqual_", StringComparison.Ordinal))
                        return Number(l == r ? 1 : 0);
                    if (name.StartsWith("GreaterEqual_", StringComparison.Ordinal))
                        return Number(l >= r ? 1 : 0);
                }
                return new Value();
            }
            return new Value();
        }
    }

    private static string? Called(JsonNode node) =>
        Text(node["StackNode"]?["$object"])
        ?? Text(node["VirtualFunctionName"])
        ?? Text(node["FunctionName"]);

    private static Value Number(double value) => new(new DamageFormula(value, 0, 0, 0));

    private static Value Scale(DamageFormula value, double factor) =>
        new(
            new(
                value.Constant * factor,
                value.Item * factor,
                value.Weapon * factor,
                value.Charge * factor
            )
        );

    private static double? Literal(Value? value) =>
        value?.Number is { Item: 0, Weapon: 0, Charge: 0 } number ? number.Constant : null;

    private static double? ChargeMaximum(ScriptFunction source)
    {
        var assignments = source
            .Statements.SelectMany(Walk)
            .Where(node =>
                Text(node["Variable"]?["Variable"]?["$field"]) is { } field
                && FieldNames.StripSuffix(field.Split(':')[^1]) == "Multiplier"
            )
            .ToArray();
        if (assignments.Length == 0)
            return null;
        var values = assignments
            .Select(node =>
                RecordValues.Number((node["Assignment"] ?? node["Expression"])?["Value"])
            )
            .ToArray();
        return values.All(value => value is not null) ? values.Max() : null;
    }
}
