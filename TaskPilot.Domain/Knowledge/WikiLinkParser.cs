using System.Text.RegularExpressions;

namespace TaskPilot.Domain.Knowledge;

public sealed record WikiLinkReference(string Slug, string? Alias);

public static partial class WikiLinkParser
{
    public static IReadOnlyList<WikiLinkReference> Parse(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return [];

        return WikiLinks().Matches(content)
            .Select(match => new WikiLinkReference(
                KnowledgeSlugPolicy.Normalize(match.Groups["slug"].Value),
                match.Groups["alias"].Success ? match.Groups["alias"].Value.Trim() : null))
            .DistinctBy(link => link.Slug)
            .ToList();
    }

    [GeneratedRegex(@"\[\[(?<slug>[a-zA-Z0-9][a-zA-Z0-9 _-]*)(?:\|(?<alias>[^\]\r\n]+))?\]\]")]
    private static partial Regex WikiLinks();
}
