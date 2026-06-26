using System.Net;
using TaskPilot.Application.Features.Dashboard.Dtos;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Application.Interfaces.Persistence.Project;
using TaskPilot.Application.Interfaces.Persistence.Workspace;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.Dashboard.Services;

public class DashboardService(
    IProjectRepository projectRepository,
    IWorkspaceMemberRepository workspaceMemberRepository,
    IGenericRepository<TaskItem> taskRepository,
    ICurrentUserService currentUserService) : IDashboardService
{
    public async Task<ServiceResult<ProjectDashboardResponse>> GetProjectDashboardAsync(int projectId, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetProjectByIdAsync(projectId, cancellationToken);
        if (project is null) return ServiceResult<ProjectDashboardResponse>.Fail("Project not found.", HttpStatusCode.NotFound);

        var currentUserId = currentUserService.GetRequiredUserId();
        if (!await workspaceMemberRepository.IsWorkspaceMemberAsync(project.WorkspaceId, currentUserId, cancellationToken))
        {
            return ServiceResult<ProjectDashboardResponse>.Fail("Project not found.", HttpStatusCode.NotFound);
        }

        var tasks = taskRepository.Where(x => x.ProjectId == projectId).ToList();
        var now = DateTime.UtcNow;
        var response = new ProjectDashboardResponse(
            projectId,
            tasks.Count,
            tasks.Count(x => x.Status == TaskItemStatus.Todo),
            tasks.Count(x => x.Status == TaskItemStatus.InProgress),
            tasks.Count(x => x.Status == TaskItemStatus.InReview),
            tasks.Count(x => x.Status == TaskItemStatus.Done),
            tasks.Count(x => x.Status == TaskItemStatus.Cancelled),
            tasks.Count(x => x.DueDate.HasValue && x.DueDate.Value < now && x.Status != TaskItemStatus.Done && x.Status != TaskItemStatus.Cancelled),
            tasks.Count(x => x.AssignedUserId.HasValue),
            tasks.Count(x => !x.AssignedUserId.HasValue));

        return ServiceResult<ProjectDashboardResponse>.Success(response);
    }
}
