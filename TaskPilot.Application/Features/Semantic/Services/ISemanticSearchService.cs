using TaskPilot.Application.Features.Semantic.Dtos;

namespace TaskPilot.Application.Features.Semantic.Services;

public interface ISemanticSearchService
{
    Task<ServiceResult<IReadOnlyList<SemanticSearchResponse>>> SearchAsync(int workspaceId, SemanticSearchRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<IReadOnlyList<SemanticSearchResponse>>> SearchProjectAsync(int projectId, SemanticSearchRequest request, CancellationToken cancellationToken);
}
