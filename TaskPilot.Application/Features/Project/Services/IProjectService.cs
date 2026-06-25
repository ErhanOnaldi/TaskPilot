using TaskPilot.Application.Features.Project.Dtos;

namespace TaskPilot.Application.Features.Project.Services;

public interface IProjectService
{
    Task<ServiceResult<List<ProjectListItemResponse>>> GetProjectsAsync(
        int workspaceId,
        CancellationToken cancellationToken);

    Task<ServiceResult<ProjectResponse>> CreateProjectAsync(int workspaceId,
        CreateProjectRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<ProjectResponse>> GetProjectAsync(
        int projectId,
        CancellationToken cancellationToken);

    Task<ServiceResult> UpdateProjectAsync(
        int projectId,
        UpdateProjectRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult> ArchiveProjectAsync(
        int projectId,
        CancellationToken cancellationToken);
}