using Bloodline.SiteData;

namespace Bloodline.Tests.Catalog;

/// <summary>Lets tests compare a fact by the invariant text readers and item qualifiers see.</summary>
internal static class FactDisplay
{
    extension(Fact fact)
    {
        public string? Value => StatVocabulary.Display(fact);
    }
}
