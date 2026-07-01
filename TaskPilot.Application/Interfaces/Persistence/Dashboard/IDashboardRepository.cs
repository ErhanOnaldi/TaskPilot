using TaskPilot.Application.Features.Dashboard.Dtos;

namespace TaskPilot.Application.Interfaces.Persistence.Dashboard;

public interface IDashboardRepository
{
    Task<ProjectDashboardResponse> GetProjectDashboardAsync(
        int projectId,
        DateTime utcNow,
        CancellationToken cancellationToken);
}
