using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json.Nodes;
using Bloodline.Records;
using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;

namespace Bloodline.Extraction;

internal sealed class PropertyEncoder(
    Action<string, string>? unsupported = null,
    Func<string, UObject?>? resolveType = null
)
{
    private static readonly ConcurrentDictionary<Type, FieldInfo[]> NativeFields = new();
    private readonly HashSet<UStruct> activeDefaults = [];

    public static bool IsEventBinding(FProperty property) =>
        property
            is FDelegateProperty
                or FMulticastDelegateProperty
                or FMulticastInlineDelegateProperty;

    private static bool IsEventBinding(string type) =>
        type
            is "DelegateProperty"
                or "MulticastDelegateProperty"
                or "MulticastInlineDelegateProperty"
                or "MulticastSparseDelegateProperty";

    public JsonObject Properties(IEnumerable<FPropertyTag> properties) =>
        EncodeProperties(properties, null, null);

    public JsonObject Properties(
        IEnumerable<FPropertyTag> properties,
        IEnumerable<string> declaredNames
    ) => EncodeProperties(properties, declaredNames, null);

    public JsonObject Properties(IEnumerable<FPropertyTag> properties, UStruct declaration) =>
        EncodeProperties(properties, null, declaration);

    public JsonNode? Value(FPropertyTagType? value, FPropertyTagData? metadata = null) =>
        EncodeValue(value, metadata, null, false);

    public JsonObject StructDefaults(UStruct declaration)
    {
        // Native constructors are not serialized defaults. Only user-defined structs declare them here.
        if (declaration is not UUserDefinedStruct userDefined)
        {
            return new JsonObject();
        }
        if (!activeDefaults.Add(declaration))
        {
            throw new InvalidDataException($"Recursive struct defaults in {declaration.Name}.");
        }
        try
        {
            var fields = DeclaredFields(declaration);
            var defaults = userDefined.DefaultProperties ?? [];
            var names = FieldNames.Resolve(
                fields.Keys.Concat(defaults.Select(field => field.Name.Text))
            );
            var result = new JsonObject();
            foreach (var (name, field) in fields)
            {
                result[names[name]] = Zero(field);
            }
            return RecordMerge.OverlayProperties(
                result,
                EncodeProperties(defaults, names.Keys, declaration)
            );
        }
        finally
        {
            activeDefaults.Remove(declaration);
        }
    }

    public JsonNode? Zero(FProperty declaration)
    {
        if (declaration.ArrayDim > 1)
        {
            var array = new JsonArray();
            for (var index = 0; index < declaration.ArrayDim; index++)
            {
                array.Add(ZeroElement(declaration));
            }
            return array;
        }
        return ZeroElement(declaration);
    }

    public IReadOnlyList<EnumEntry> EnumEntries(UEnum declaration)
    {
        var entries = new List<EnumEntry>();
        foreach (var (name, value) in declaration.Names ?? [])
        {
            entries.Add(new EnumEntry(name.Text, value, EnumDisplay(declaration, name.Text)));
        }
        return entries;
    }

    private JsonNode? ZeroElement(FProperty declaration) =>
        declaration switch
        {
            FBoolProperty => JsonValue.Create(false),
            FByteProperty { Enum: { IsNull: false } enumIndex } => ZeroEnum(enumIndex),
            FEnumProperty enumProperty => ZeroEnum(enumProperty.Enum),
            FByteProperty => JsonValue.Create((byte)0),
            FInt8Property => JsonValue.Create((sbyte)0),
            FInt16Property => JsonValue.Create((short)0),
            FIntProperty => JsonValue.Create(0),
            FInt64Property => JsonValue.Create(0L),
            FUInt16Property => JsonValue.Create((ushort)0),
            FUInt32Property => JsonValue.Create(0U),
            FUInt64Property => JsonValue.Create(0UL),
            FFloatProperty => JsonValue.Create(0F),
            FDoubleProperty => JsonValue.Create(0D),
            FNameProperty => JsonValue.Create("None"),
            FStrProperty or FUtf8StrProperty => JsonValue.Create(""),
            FTextProperty => new JsonObject { [RecordFormat.Text] = "" },
            FObjectProperty or FInterfaceProperty or FOptionalProperty => null,
            FArrayProperty or FSetProperty => new JsonArray(),
            FMapProperty => new JsonObject { [RecordFormat.Map] = new JsonArray() },
            FStructProperty structProperty => ZeroStruct(structProperty),
            _ => Unsupported(
                declaration.GetType().Name,
                "No serialized initial value encoder is available."
            ),
        };

    private JsonNode ZeroEnum(FPackageIndex? index)
    {
        var declaration = Load<UEnum>(index);
        if (declaration is not null)
        {
            foreach (var (name, value) in declaration.Names ?? [])
            {
                if (value == 0)
                {
                    return Enum(name.Text, declaration);
                }
            }
        }
        if (
            index?.ResolvedObject is { } native
            && native.GetPathName().StartsWith("/Script/", StringComparison.Ordinal)
        )
        {
            return Enum($"{native.Name.Text}::0", null);
        }
        return Unsupported("EnumProperty", "The enum has no resolvable member with value zero.");
    }

    private JsonNode? ZeroStruct(FStructProperty declaration)
    {
        var structure = ResolveStruct(null, declaration);
        if (structure is UUserDefinedStruct)
        {
            return StructDefaults(structure);
        }
        var name = structure?.Name ?? declaration.Struct?.Name;
        if (name is "SoftObjectPath" or "SoftClassPath")
        {
            return null;
        }
        IUStruct? initial = name switch
        {
            "Vector" => new FVector(),
            "Vector2D" => new FVector2D(),
            "Vector4" => new FVector4(),
            "Rotator" => new FRotator(),
            "Quat" => new FQuat(),
            "Transform" => FTransform.Identity,
            "Color" => new FColor(),
            "LinearColor" => new FLinearColor(),
            "IntPoint" => new FIntPoint(),
            "IntVector" => new FIntVector(),
            "Guid" => new FGuid(),
            "DateTime" or "Timespan" => new FDateTime(),
            "Box" => new FBox(),
            "Box2D" => new FBox2D(),
            "Plane" => new FPlane(),
            _ => null,
        };
        return initial is not null
            ? Native(initial)
            : Unsupported(
                $"StructProperty:{name ?? "Unknown"}",
                $"No initial value encoder for {declaration.Name.Text}."
            );
    }

    private JsonObject EncodeProperties(
        IEnumerable<FPropertyTag> properties,
        IEnumerable<string>? declaredNames,
        UStruct? declaration
    )
    {
        var stored = properties
            .Where(property =>
                !IsEventBinding(property.PropertyType.Text)
                && !IsEventBinding(property.Tag?.GetType().Name ?? "")
            )
            .ToArray();
        var fields = declaration is null ? null : DeclaredFields(declaration);
        var names = FieldNames.Resolve(
            stored
                .Select(field => field.Name.Text)
                .Concat(declaredNames ?? fields?.Keys.AsEnumerable() ?? [])
        );
        var result = new JsonObject();
        foreach (var group in stored.GroupBy(field => field.Name.Text, StringComparer.Ordinal))
        {
            var field = fields?.GetValueOrDefault(group.Key);
            var elements = group.ToArray();
            if (elements.Length > 1 || elements[0].ArrayIndex > 0 || field?.ArrayDim > 1)
            {
                var length = Math.Max(
                    field?.ArrayDim ?? 0,
                    elements.Max(element => element.ArrayIndex) + 1
                );
                var array = new JsonArray();
                for (var index = 0; index < length; index++)
                {
                    var element = elements.FirstOrDefault(element => element.ArrayIndex == index);
                    array.Add(
                        element is null
                            ? Unsupported(
                                "StaticArrayElement",
                                $"No stored value for {group.Key}[{index}]."
                            )
                            : EncodeTag(element, field)
                    );
                }
                result[names[group.Key]] = array;
            }
            else
            {
                result[names[group.Key]] = EncodeTag(elements[0], field);
            }
        }
        return result;
    }

    private JsonNode? EncodeTag(FPropertyTag property, FProperty? declaration)
    {
        if (property.Tag is not null)
        {
            return EncodeValue(property.Tag, property.TagData, declaration, false);
        }
        if (HasParser(property.PropertyType.Text))
        {
            // CUE4Parse can catch a parser exception and leave a supported tag undecoded.
            throw new InvalidDataException(
                $"Incomplete {property.PropertyType.Text} value for {property.Name.Text}."
            );
        }
        return Unsupported(
            property.PropertyType.Text,
            $"No decoded value for {property.Name.Text}."
        );
    }

    private static bool HasParser(string type) =>
        type
            is "ArrayProperty"
                or "AssetObjectProperty"
                or "AssetClassProperty"
                or "BoolProperty"
                or "ByteProperty"
                or "ClassProperty"
                or "DelegateProperty"
                or "DoubleProperty"
                or "EnumProperty"
                or "FieldPathProperty"
                or "FloatProperty"
                or "Int16Property"
                or "Int64Property"
                or "Int8Property"
                or "IntProperty"
                or "InterfaceProperty"
                or "LazyObjectProperty"
                or "MapProperty"
                or "MulticastDelegateProperty"
                or "MulticastInlineDelegateProperty"
                or "MulticastSparseDelegateProperty"
                or "NameProperty"
                or "ObjectProperty"
                or "SetProperty"
                or "SoftClassProperty"
                or "SoftObjectProperty"
                or "StrProperty"
                or "AnsiStrProperty"
                or "Utf8StrProperty"
                or "StructProperty"
                or "TextProperty"
                or "UInt16Property"
                or "UInt32Property"
                or "UInt64Property"
                or "WeakObjectProperty"
                or "OptionalProperty"
                or "ReferenceProperty"
                or "VerseStringProperty"
                or "VerseFunctionProperty"
                or "VerseDynamicProperty"
                or "VerseClassProperty";

    private JsonNode? EncodeValue(
        FPropertyTagType? property,
        FPropertyTagData? metadata,
        FProperty? declaration,
        bool fillStructDefaults
    )
    {
        switch (property)
        {
            case null:
                return null;
            case EnumProperty enumeration:
                return Enum(
                    enumeration.Value.Text,
                    ResolveEnum(metadata, declaration, enumeration.Value.Text)
                );
            case StructProperty structure:
                var scriptStruct = ResolveStruct(metadata, declaration as FStructProperty);
                var encoded = Native(structure.Value?.StructType, scriptStruct);
                return fillStructDefaults && scriptStruct is not null && encoded is JsonObject obj
                    ? RecordMerge.OverlayProperties(StructDefaults(scriptStruct), obj)
                    : encoded;
            case ArrayProperty { Value: { } array }:
                return Sequence(
                    array.Properties,
                    array.InnerTagData ?? metadata?.InnerTypeData,
                    (declaration as FArrayProperty)?.Inner
                );
            case SetProperty { Value: { } set }:
                return Sequence(
                    set.Properties,
                    metadata?.InnerTypeData,
                    (declaration as FSetProperty)?.ElementProp
                );
            case MapProperty { Value: { } map }:
                var entries = new JsonArray();
                foreach (var (key, value) in map.Properties)
                {
                    entries.Add(
                        new JsonArray(
                            EncodeValue(
                                key,
                                metadata?.InnerTypeData,
                                (declaration as FMapProperty)?.KeyProp,
                                true
                            ),
                            EncodeValue(
                                value,
                                metadata?.ValueTypeData,
                                (declaration as FMapProperty)?.ValueProp,
                                true
                            )
                        )
                    );
                }
                return new JsonObject { [RecordFormat.Map] = entries };
            case OptionalProperty optional:
                return EncodeValue(
                    optional.Value,
                    metadata?.InnerTypeData,
                    (declaration as FOptionalProperty)?.ValueProperty,
                    fillStructDefaults
                );
            case AssetObjectProperty asset:
                return Soft(asset.Value);
            case FPropertyTagType<FPackageIndex> reference:
                return Reference(reference.Value);
            case InterfaceProperty reference:
                return Reference(reference.Value?.Object);
            case FPropertyTagType<FSoftObjectPath> soft:
                return Soft(soft.Value);
            case FPropertyTagType<FText> text:
                return text.Value is null ? null : Text(text.Value);
            case FPropertyTagType<FName> name:
                return JsonValue.Create(name.Value.Text);
            case FPropertyTagType<string> text:
                return JsonValue.Create(text.Value);
            case FPropertyTagType<bool> boolean:
                return JsonValue.Create(boolean.Value);
            case FPropertyTagType<byte> number:
                return JsonValue.Create(number.Value);
            case FPropertyTagType<sbyte> number:
                return JsonValue.Create(number.Value);
            case FPropertyTagType<short> number:
                return JsonValue.Create(number.Value);
            case FPropertyTagType<ushort> number:
                return JsonValue.Create(number.Value);
            case FPropertyTagType<int> number:
                return JsonValue.Create(number.Value);
            case FPropertyTagType<uint> number:
                return JsonValue.Create(number.Value);
            case FPropertyTagType<long> number:
                return JsonValue.Create(number.Value);
            case FPropertyTagType<ulong> number:
                return JsonValue.Create(number.Value);
            case FPropertyTagType<float> number:
                return float.IsFinite(number.Value)
                    ? JsonValue.Create(number.Value)
                    : Unsupported("FloatProperty", "JSON cannot represent a non-finite float.");
            case FPropertyTagType<double> number:
                return double.IsFinite(number.Value)
                    ? JsonValue.Create(number.Value)
                    : Unsupported("DoubleProperty", "JSON cannot represent a non-finite double.");
            default:
                return Unsupported(property.GetType().Name, "No property encoder is available.");
        }
    }

    private JsonArray Sequence(
        IEnumerable<FPropertyTagType> elements,
        FPropertyTagData? metadata,
        FProperty? declaration
    )
    {
        var result = new JsonArray();
        foreach (var element in elements)
        {
            result.Add(EncodeValue(element, metadata, declaration, true));
        }
        return result;
    }

    private JsonNode? Native(object? value, UStruct? declaration = null)
    {
        switch (value)
        {
            case null:
                return null;
            case FStructFallback fallback:
                return EncodeProperties(fallback.Properties, null, declaration);
            case FScriptStruct structure:
                return Native(structure.StructType, declaration);
            case FName name:
                return JsonValue.Create(name.Text);
            case FText text:
                return Text(text);
            case FPackageIndex reference:
                return Reference(reference);
            case FSoftObjectPath soft:
                return Soft(soft);
            case string text:
                return JsonValue.Create(text);
            case bool boolean:
                return JsonValue.Create(boolean);
            case byte number:
                return JsonValue.Create(number);
            case sbyte number:
                return JsonValue.Create(number);
            case short number:
                return JsonValue.Create(number);
            case ushort number:
                return JsonValue.Create(number);
            case int number:
                return JsonValue.Create(number);
            case uint number:
                return JsonValue.Create(number);
            case long number:
                return JsonValue.Create(number);
            case ulong number:
                return JsonValue.Create(number);
            case float number:
                return float.IsFinite(number)
                    ? JsonValue.Create(number)
                    : Unsupported("Single", "JSON cannot represent a non-finite float.");
            case double number:
                return double.IsFinite(number)
                    ? JsonValue.Create(number)
                    : Unsupported("Double", "JSON cannot represent a non-finite double.");
            case System.Enum enumeration:
                return Enum($"{enumeration.GetType().Name}::{enumeration}", null);
            case IDictionary dictionary:
                var entries = new JsonArray();
                foreach (DictionaryEntry entry in dictionary)
                {
                    entries.Add(new JsonArray(Native(entry.Key), Native(entry.Value)));
                }
                return new JsonObject { [RecordFormat.Map] = entries };
            case IEnumerable sequence:
                var elements = new JsonArray();
                foreach (var element in sequence)
                {
                    elements.Add(Native(element));
                }
                return elements;
            case IUStruct:
                // Read decoded data fields, not computed properties or CUE4Parse's presentation JSON.
                var fields = NativeFields.GetOrAdd(
                    value.GetType(),
                    static type =>
                        type.GetFields(
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                        )
                );
                if (fields.Length == 0 || fields.Any(field => !field.IsPublic))
                {
                    return Unsupported(
                        value.GetType().Name,
                        "The decoded native struct does not expose all data fields."
                    );
                }
                var result = new JsonObject();
                foreach (var field in fields)
                {
                    result[field.Name] = Native(field.GetValue(value));
                }
                return result;
            default:
                return Unsupported(
                    value.GetType().Name,
                    "No decoded native value encoder is available."
                );
        }
    }

    private JsonNode? Reference(FPackageIndex? value)
    {
        if (value is null || value.IsNull)
        {
            return null;
        }
        return value.ResolvedObject is { } resolved
            ? new JsonObject { [RecordFormat.Reference] = resolved.GetPathName() }
            : Unsupported("ObjectProperty", "A non-null package index has no resolved outer path.");
    }

    private static JsonNode? Soft(FSoftObjectPath value) =>
        value.AssetPathName.IsNone || string.IsNullOrEmpty(value.AssetPathName.Text)
            ? null
            : Soft(
                string.IsNullOrEmpty(value.SubPathString)
                    ? value.AssetPathName.Text
                    : $"{value.AssetPathName.Text}:{value.SubPathString}"
            );

    private static JsonNode? Soft(string? path) =>
        string.IsNullOrEmpty(path) || path == "None"
            ? null
            : new JsonObject { [RecordFormat.SoftReference] = path };

    private static JsonNode Text(FText value) => TextEncoder.Encode(value);

    private JsonObject Enum(string name, UEnum? declaration)
    {
        var result = new JsonObject { [RecordFormat.Enum] = name };
        if (declaration is not null && EnumDisplay(declaration, name) is { } display)
        {
            result[RecordFormat.EnumDisplay] = display;
        }
        return result;
    }

    private static string? EnumDisplay(UEnum declaration, string name)
    {
        if (declaration is not UUserDefinedEnum)
        {
            return null;
        }
        var display =
            declaration
                .Properties.FirstOrDefault(property => property.Name.Text == "DisplayNameMap")
                ?.Tag?.GenericValue as UScriptMap;
        if (display is null)
        {
            return null;
        }
        var member = EnumMember(name);
        foreach (var (key, value) in display.Properties)
        {
            var keyName = key.GenericValue switch
            {
                FName entry => entry.Text,
                string entry => entry,
                _ => null,
            };
            if (
                keyName is not null
                && (keyName == name || EnumMember(keyName) == member)
                && value?.GenericValue is FText text
            )
            {
                return RecordValues.Text(TextEncoder.Encode(text));
            }
        }
        return null;
    }

    private static string EnumMember(string name)
    {
        var separator = name.LastIndexOf("::", StringComparison.Ordinal);
        return separator < 0 ? name : name[(separator + 2)..];
    }

    private UEnum? ResolveEnum(FPropertyTagData? metadata, FProperty? declaration, string name) =>
        metadata?.Enum
        ?? (
            declaration switch
            {
                FEnumProperty enumeration => Load<UEnum>(enumeration.Enum),
                FByteProperty enumeration => Load<UEnum>(enumeration.Enum),
                _ => null,
            }
        )
        ?? Resolve<UEnum>(metadata?.EnumName)
        ?? Resolve<UEnum>(
            name.Contains("::", StringComparison.Ordinal)
                ? name[..name.IndexOf("::", StringComparison.Ordinal)]
                : null
        );

    private UStruct? ResolveStruct(FPropertyTagData? metadata, FStructProperty? declaration) =>
        metadata?.Struct
        ?? Load<UStruct>(declaration?.Struct)
        ?? Resolve<UStruct>(metadata?.StructType);

    private T? Load<T>(FPackageIndex? index)
        where T : UObject =>
        index is null || index.IsNull ? null : index.Load() as T ?? Resolve<T>(index.Name);

    private T? Resolve<T>(string? name)
        where T : UObject =>
        string.IsNullOrEmpty(name) || name == "None" ? null : resolveType?.Invoke(name) as T;

    private static Dictionary<string, FProperty> DeclaredFields(UStruct declaration)
    {
        var result = new Dictionary<string, FProperty>(StringComparer.Ordinal);
        var current = declaration;
        var visited = new HashSet<UStruct>();
        while (visited.Add(current))
        {
            foreach (var field in current.ChildProperties ?? [])
            {
                if (field is FProperty property && !IsEventBinding(property))
                {
                    result.TryAdd(property.Name.Text, property);
                }
            }
            if (
                current.SuperStruct is not { IsNull: false } parent
                || parent.Load() is not UStruct ancestor
            )
            {
                return result;
            }
            current = ancestor;
        }
        throw new InvalidDataException($"Recursive struct declaration in {declaration.Name}.");
    }

    private JsonObject Unsupported(string type, string detail)
    {
        unsupported?.Invoke(type, detail);
        return new JsonObject { [RecordFormat.Unsupported] = type };
    }
}
