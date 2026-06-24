namespace TaskPilot.Application.Interfaces.Persistence.Workspace;
using Domain.Entities;

public interface IWorkspaceRepository : IGenericRepository<WorkSpace>
{
    Task<List<WorkSpace>> GetWorkspacesByUserIdAsync(int userId, CancellationToken cancellationToken);
    Task<WorkSpace?> GetWorkspaceForMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken);
    Task<WorkSpace?> GetWorkspaceForOwnerAsync(int workspaceId, int userId, CancellationToken cancellationToken);
}
