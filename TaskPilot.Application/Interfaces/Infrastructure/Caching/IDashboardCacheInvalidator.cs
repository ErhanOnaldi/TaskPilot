namespace TaskPilot.Application.Interfaces.Infrastructure.Caching;

public interface IDashboardCacheInvalidator
{
    Task InvalidateProjectDashboardAsync(
        int projectId,
        CancellationToken cancellationToken);
}