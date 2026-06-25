using WorkspaceMemberEntity = TaskPilot.Domain.Entities.WorkspaceMember;

namespace TaskPilot.Application.Interfaces.Persistence.Workspace;

public interface IWorkspaceMemberRepository : IGenericRepository<WorkspaceMemberEntity>
{
    Task<List<WorkspaceMemberEntity>> GetMembersByWorkspaceIdAsync(
        int workspaceId,
        CancellationToken cancellationToken);

    Task<WorkspaceMemberEntity?> GetMemberAsync(
        int workspaceId,
        int userId,
        CancellationToken cancellationToken);

    Task<bool> IsWorkspaceMemberAsync(
        int workspaceId,
        int userId,
        CancellationToken cancellationToken);

    Task<bool> IsWorkspaceOwnerAsync(
        int workspaceId,
        int userId,
        CancellationToken cancellationToken);

    Task<int> CountOwnersAsync(
        int workspaceId,
        CancellationToken cancellationToken);
}
