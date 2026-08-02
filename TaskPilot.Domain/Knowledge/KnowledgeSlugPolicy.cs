using System.Text;
using System.Text.RegularExpressions;

namespace TaskPilot.Domain.Knowledge;

public static partial class KnowledgeSlugPolicy
{
    public static string Normalize(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = NonSlugCharacters().Replace(value.Trim().ToLowerInvariant(), "-");
        normalized = RepeatedDashes().Replace(normalized, "-").Trim('-');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Slug must contain at least one letter or number.", nameof(value));
        }

        return normalized;
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonSlugCharacters();

    [GeneratedRegex("-{2,}")]
    private static partial Regex RepeatedDashes();
}
