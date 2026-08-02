namespace TaskPilot.Application.Features.Knowledge.Repositories;

/// <summary>Performs database-scoped workspace/project/folder consistency checks for Knowledge commands.</summary>
public interface IKnowledgeScopeValidationPort
{
    Task<bool> IsProjectInWorkspaceAsync(int workspaceId, int? projectId, CancellationToken cancellationToken);
    Task<bool> IsFolderInWorkspaceAsync(int workspaceId, int? projectId, int? folderId, CancellationToken cancellationToken);
    Task<bool> IsParentFolderInWorkspaceAsync(int workspaceId, int? projectId, int? parentFolderId, CancellationToken cancellationToken);
}
