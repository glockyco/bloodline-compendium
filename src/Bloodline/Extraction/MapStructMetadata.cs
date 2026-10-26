using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace Bloodline.Extraction;

internal static class MapStructMetadata
{
    public static void Register(UStruct declaration, VersionContainer versions)
    {
        foreach (var map in declaration.ChildProperties?.OfType<FMapProperty>() ?? [])
        {
            var key = StructName(map.KeyProp);
            var value = StructName(map.ValueProp);
            if (key.Length == 0 && value.Length == 0)
                continue;
            var types = KeyValuePair.Create(key, value);
            if (
                versions.MapStructTypes.TryGetValue(map.Name.Text, out var existing)
                && ((existing.Key ?? "") != key || (existing.Value ?? "") != value)
            )
            {
                throw new InvalidDataException(
                    $"Conflicting struct types for map property {map.Name.Text}."
                );
            }
            // UE4's tagged maps omit their inner struct names. The declarations supply those names to the parser.
            versions.MapStructTypes[map.Name.Text] = types;
        }
    }

    private static string StructName(FProperty? property) =>
        property is FStructProperty { Struct: { IsNull: false } reference } ? reference.Name : "";
}
