using TaskPilot.Application.Authorization.Abstractions;
using TaskPilot.Application.Authorization.Enums;
using TaskPilot.Application.Features.Dashboard.Dtos;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.Dashboard.Services;

public class DashboardService(
    IGenericRepository<TaskItem> taskRepository,
    IAccessControlService accessControlService) : IDashboardService
{
    public async Task<ServiceResult<ProjectDashboardResponse>> GetProjectDashboardAsync(int projectId, CancellationToken cancellationToken)
    {
        var access = await accessControlService.AuthorizeProjectAsync(
            projectId,
            ProjectAccessLevel.Read,
            requireActiveProject: false,
            cancellationToken);
        if (access.Failure is not null)
        {
            return ServiceResult<ProjectDashboardResponse>.Fail(access.Failure.ErrorMessages!, access.Failure.Status);
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
