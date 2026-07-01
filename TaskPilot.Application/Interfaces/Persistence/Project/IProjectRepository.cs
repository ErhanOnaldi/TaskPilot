namespace TaskPilot.Application.Interfaces.Persistence.Project;
using Common.Pagination;
using Domain.Entities;
using Features.Project.Dtos;

public interface IProjectRepository : IGenericRepository<Project>
{
    Task<List<Project>> GetProjectsByWorkspaceIdAsync(
        int workspaceId,
        CancellationToken cancellationToken);

    Task<PagedResponse<Project>> GetProjectsByWorkspaceIdAsync(
        int workspaceId,
        ProjectQueryParameters query,
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
