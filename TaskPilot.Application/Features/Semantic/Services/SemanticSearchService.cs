using System.Net;
using TaskPilot.Application.Features.Semantic.Contracts;
using TaskPilot.Application.Features.Semantic.Dtos;

namespace TaskPilot.Application.Features.Semantic.Services;

public sealed class SemanticSearchService(ISemanticAuthorizationPort authorizationPort, ISemanticDocumentRepository repository, IEmbeddingGenerator embeddingGenerator) : ISemanticSearchService
{
    public async Task<ServiceResult<IReadOnlyList<SemanticSearchResponse>>> SearchAsync(int workspaceId, SemanticSearchRequest request, CancellationToken cancellationToken)
        => await SearchAsync(await authorizationPort.GetReadScopeAsync(workspaceId, cancellationToken), request, cancellationToken);

    public async Task<ServiceResult<IReadOnlyList<SemanticSearchResponse>>> SearchProjectAsync(int projectId, SemanticSearchRequest request, CancellationToken cancellationToken)
        => await SearchAsync(await authorizationPort.GetProjectReadScopeAsync(projectId, cancellationToken), request, cancellationToken);

    private async Task<ServiceResult<IReadOnlyList<SemanticSearchResponse>>> SearchAsync(SemanticSearchScope? scope, SemanticSearchRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query)) return ServiceResult<IReadOnlyList<SemanticSearchResponse>>.Fail("Query is required.", HttpStatusCode.BadRequest);
        if (scope is null) return ServiceResult<IReadOnlyList<SemanticSearchResponse>>.Fail("Knowledge access denied.", HttpStatusCode.Forbidden);
        float[]? queryEmbedding = null;
        if (request.Mode is SemanticSearchMode.Semantic or SemanticSearchMode.Hybrid)
        {
            queryEmbedding = (await embeddingGenerator.GenerateAsync(request.Query.Trim(), cancellationToken)).Vector;
        }
        var candidates = await repository.SearchScopedAsync(scope, request.Query.Trim(), queryEmbedding, Math.Clamp(request.Limit, 1, 50), cancellationToken);
        IEnumerable<SemanticSearchCandidate> ordered = request.Mode == SemanticSearchMode.Hybrid ? SemanticHybridRanker.Rank(candidates) : candidates.OrderByDescending(c => request.Mode == SemanticSearchMode.Text ? c.TextScore : c.VectorScore);
        return ServiceResult<IReadOnlyList<SemanticSearchResponse>>.Success(ordered.Select(c => new SemanticSearchResponse(c.DocumentId, c.SourceType, c.SourceId, c.ProjectId, c.Content, request.Mode == SemanticSearchMode.Text ? c.TextScore : request.Mode == SemanticSearchMode.Semantic ? c.VectorScore : c.TextScore * .3f + c.VectorScore * .7f)).ToList());
    }
}
