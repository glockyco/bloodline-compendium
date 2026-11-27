using Bloodline.Code;

namespace Bloodline.Catalog;

/// <summary>
/// A function whose behaviour the catalog holds as read with <c>bloodline script</c>, by the fingerprint it had then.
/// </summary>
internal static class ReadFunction
{
    /// <summary>
    /// The function of <paramref name="citation"/>, or null in a build that has no object of its class. A game
    /// update that changes or removes the function fails the catalog until it is read again and
    /// <paramref name="list"/> is updated.
    /// </summary>
    public static CodeFunction? Find(
        CatalogContext context,
        string citation,
        string fingerprint,
        string list
    )
    {
        var function = context.Records.Code?.Functions.SingleOrDefault(function =>
            function.Citation == citation
        );
        if (function is null)
        {
            var package = citation.Split(':')[0];
            if (context.Classes.Keys.Any(path => path.Split('.')[0] == package))
                throw new InvalidDataException(
                    $"{citation} is gone, but its class is not. Read the class again and update {list}."
                );
            return null;
        }
        if (!function.Fingerprint.StartsWith(fingerprint, StringComparison.Ordinal))
            throw new InvalidDataException(
                $"{citation} changed since it was read. Read it again and update {list}."
            );
        return function;
    }
}
