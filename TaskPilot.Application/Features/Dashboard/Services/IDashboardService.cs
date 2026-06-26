using TaskPilot.Application.Features.Dashboard.Dtos;

namespace TaskPilot.Application.Features.Dashboard.Services;

public interface IDashboardService
{
    Task<ServiceResult<ProjectDashboardResponse>> GetProjectDashboardAsync(int projectId, CancellationToken cancellationToken);
}
