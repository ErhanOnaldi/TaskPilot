using TaskPilot.Application.Features.Semantic.Contracts;

namespace TaskPilot.Application.Features.Semantic;

public static class SemanticHybridRanker
{
    public static IReadOnlyList<SemanticSearchCandidate> Rank(IEnumerable<SemanticSearchCandidate> candidates, float semanticWeight = .7f)
    {
        var textWeight = 1f - semanticWeight;
        return candidates.OrderByDescending(candidate => candidate.VectorScore * semanticWeight + candidate.TextScore * textWeight).ThenBy(candidate => candidate.DocumentId).ToList();
    }
}
