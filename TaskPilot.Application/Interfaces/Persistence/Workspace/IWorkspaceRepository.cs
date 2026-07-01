namespace TaskPilot.Application.Interfaces.Persistence.Workspace;
using Common.Pagination;
using Domain.Entities;
using Features.Workspace.Dtos;

public interface IWorkspaceRepository : IGenericRepository<WorkSpace>
{
    Task<List<WorkSpace>> GetWorkspacesByUserIdAsync(int userId, CancellationToken cancellationToken);
    Task<PagedResponse<WorkSpace>> GetWorkspacesByUserIdAsync(
        int userId,
        WorkspaceQueryParameters query,
        CancellationToken cancellationToken);
    Task<WorkSpace?> GetWorkspaceForMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken);
    Task<WorkSpace?> GetWorkspaceForOwnerAsync(int workspaceId, int userId, CancellationToken cancellationToken);
}
