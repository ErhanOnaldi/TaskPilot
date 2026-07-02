using TaskPilot.Application.Authorization.Abstractions;
using TaskPilot.Application.Authorization.Enums;
using TaskPilot.Application.Caching;
using TaskPilot.Application.Features.Dashboard.Dtos;
using TaskPilot.Application.Interfaces.Infrastructure.Caching;
using TaskPilot.Application.Interfaces.Persistence.Dashboard;

namespace TaskPilot.Application.Features.Dashboard.Services;

public class DashboardService(
    IDashboardRepository dashboardRepository,
    IAccessControlService accessControlService,
    ICacheService cacheService) : IDashboardService
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
        var cacheKey = CacheKeys.ProjectDashboard(projectId);

        var cachedDashboard = await cacheService.GetAsync<ProjectDashboardResponse>(
            cacheKey,
            cancellationToken);
        if (cachedDashboard is not null)
        {
            return ServiceResult<ProjectDashboardResponse>.Success(cachedDashboard);
        }
        
        var response = await dashboardRepository.GetProjectDashboardAsync(projectId, DateTime.UtcNow, cancellationToken);
        
        await cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5), cancellationToken);

        return ServiceResult<ProjectDashboardResponse>.Success(response);
    }
}
