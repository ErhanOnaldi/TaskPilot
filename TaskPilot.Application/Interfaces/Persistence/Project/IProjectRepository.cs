namespace TaskPilot.Application.Interfaces.Persistence.Project;
using Domain.Entities;

public interface IProjectRepository : IGenericRepository<Project>
{
    Task<List<Project>> GetProjectsByWorkspaceIdAsync(
        int workspaceId,
        CancellationToken cancellationToken);

    Task<Project?> GetProjectByIdAsync(
        int projectId,
        CancellationToken cancellationToken);

    Task<Project?> GetProjectForUpdateAsync(
        int projectId,
        CancellationToken cancellationToken);

    Task<bool> ExistsByNameInWorkspaceAsync(
        int workspaceId,
        string name,
        CancellationToken cancellationToken);

    Task<bool> ExistsByNameInWorkspaceExceptProjectAsync(
        int workspaceId,
        int projectId,
        string name,
        CancellationToken cancellationToken);
}