using TaskPilot.Application.Caching;
using TaskPilot.Application.Interfaces.Infrastructure.Caching;

namespace TaskPilot.Infrastructure.Caching;

public class DashboardCacheInvalidator(ICacheService cacheService) : IDashboardCacheInvalidator
{
    public Task InvalidateProjectDashboardAsync(int projectId, CancellationToken cancellationToken)
    {
        return cacheService.RemoveAsync(
            CacheKeys.ProjectDashboard(projectId),
            cancellationToken);

    }
}