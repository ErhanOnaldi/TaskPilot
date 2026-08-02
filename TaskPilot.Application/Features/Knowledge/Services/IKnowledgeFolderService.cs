using TaskPilot.Application.Features.Knowledge.Dtos;

namespace TaskPilot.Application.Features.Knowledge.Services;

public interface IKnowledgeFolderService
{
    Task<ServiceResult<IReadOnlyList<KnowledgeFolderResponse>>> ListAsync(int workspaceId, int? projectId, CancellationToken cancellationToken);
    Task<ServiceResult<KnowledgeFolderResponse>> GetAsync(int folderId, CancellationToken cancellationToken);
    Task<ServiceResult<KnowledgeFolderResponse>> UpdateAsync(int folderId, UpdateKnowledgeFolderRequest request, CancellationToken cancellationToken);
    Task<ServiceResult> DeleteAsync(int folderId, CancellationToken cancellationToken);
}
