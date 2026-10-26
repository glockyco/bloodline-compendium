namespace Bloodline.Extraction;

internal static class FieldNames
{
    public static string StripSuffix(string name)
    {
        var guidSeparator = name.LastIndexOf('_');
        if (guidSeparator <= 0 || name.Length - guidSeparator - 1 != 32)
        {
            return name;
        }
        foreach (var character in name.AsSpan(guidSeparator + 1))
        {
            if (!char.IsAsciiHexDigit(character))
            {
                return name;
            }
        }
        var numberSeparator = name.LastIndexOf('_', guidSeparator - 1);
        if (numberSeparator <= 0 || numberSeparator + 1 == guidSeparator)
        {
            return name;
        }
        foreach (
            var character in name.AsSpan(numberSeparator + 1, guidSeparator - numberSeparator - 1)
        )
        {
            if (!char.IsAsciiDigit(character))
            {
                return name;
            }
        }
        return name[..numberSeparator];
    }

    public static IReadOnlyDictionary<string, string> Resolve(IEnumerable<string> names)
    {
        var candidates = new Dictionary<string, string>(StringComparer.Ordinal);
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var name in names)
        {
            candidates.TryAdd(name, StripSuffix(name));
        }
        bool changed;
        do
        {
            counts.Clear();
            foreach (var candidate in candidates.Values)
            {
                counts[candidate] = counts.GetValueOrDefault(candidate) + 1;
            }
            changed = false;
            foreach (var (name, candidate) in candidates)
            {
                if (name != candidate && counts[candidate] > 1)
                {
                    candidates[name] = name;
                    changed = true;
                }
            }
        } while (changed);
        return candidates;
    }
}
