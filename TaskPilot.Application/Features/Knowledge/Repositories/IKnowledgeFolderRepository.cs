using TaskPilot.Domain.Knowledge;

namespace TaskPilot.Application.Features.Knowledge.Repositories;

public interface IKnowledgeFolderRepository
{
    ValueTask<KnowledgeFolder?> GetByIdAsync(int folderId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeFolder>> GetByWorkspaceAsync(int workspaceId, int? projectId, CancellationToken cancellationToken);
    Task<bool> ExistsBySlugAsync(int workspaceId, int? projectId, string normalizedSlug, CancellationToken cancellationToken);
    Task<bool> ExistsBySlugExceptFolderAsync(int workspaceId, int? projectId, int folderIdToExclude, string normalizedSlug, CancellationToken cancellationToken);
    ValueTask AddAsync(KnowledgeFolder folder, CancellationToken cancellationToken);
    Task DeleteAsync(KnowledgeFolder folder, CancellationToken cancellationToken);
}
