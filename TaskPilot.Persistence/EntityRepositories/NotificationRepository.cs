using Microsoft.EntityFrameworkCore;
using TaskPilot.Application.Common.Pagination;
using TaskPilot.Application.Features.Notifications.Dtos;
using TaskPilot.Application.Interfaces.Persistence.Notifications;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Persistence.EntityRepositories;

public sealed class NotificationRepository : GenericRepository<Notification>, INotificationRepository
{
    private readonly AppDbContext _dbContext;

    public NotificationRepository(AppDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResponse<Notification>> GetNotificationsByUserIdAsync(
        int userId,
        NotificationQueryParameters query,
        CancellationToken cancellationToken)
    {
        var notificationQuery = _dbContext.Notifications
            .AsNoTracking()
            .Where(notification => notification.UserId == userId);

        if (query.IsRead.HasValue)
        {
            notificationQuery = notificationQuery.Where(notification => notification.IsRead == query.IsRead.Value);
        }

        var type = query.Type?.Trim();
        if (!string.IsNullOrWhiteSpace(type))
        {
            notificationQuery = notificationQuery.Where(notification => notification.Type == type);
        }

        var totalCount = await notificationQuery.CountAsync(cancellationToken);
        var items = await ApplySorting(notificationQuery, query)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return PagedResponse<Notification>.Create(items, query.PageNumber, query.PageSize, totalCount);
    }

    public Task<List<Notification>> GetUnreadNotificationsByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        return _dbContext.Notifications
            .Where(notification => notification.UserId == userId && !notification.IsRead)
            .ToListAsync(cancellationToken);
    }

    public Task<int> MarkAllAsReadAsync(int userId, DateTime utcNow, CancellationToken cancellationToken)
    {
        return _dbContext.Notifications
            .Where(notification => notification.UserId == userId && !notification.IsRead)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(notification => notification.IsRead, true)
                    .SetProperty(notification => notification.UpdatedAt, utcNow),
                cancellationToken);
    }
    public Task<bool> ExistsBySourceEventIdAsync(
        int userId,
        Guid sourceEventId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Notifications.AnyAsync(
            notification =>
                notification.UserId == userId &&
                notification.SourceEventId == sourceEventId,
            cancellationToken);
    }

    private static IOrderedQueryable<Notification> ApplySorting(
        IQueryable<Notification> query,
        NotificationQueryParameters parameters)
    {
        return (parameters.SortBy, parameters.SortDirection) switch
        {
            (NotificationSortBy.IsRead, SortDirection.Asc) => query.OrderBy(notification => notification.IsRead).ThenByDescending(notification => notification.Id),
            (NotificationSortBy.IsRead, SortDirection.Desc) => query.OrderByDescending(notification => notification.IsRead).ThenByDescending(notification => notification.Id),
            (NotificationSortBy.Type, SortDirection.Asc) => query.OrderBy(notification => notification.Type).ThenByDescending(notification => notification.Id),
            (NotificationSortBy.Type, SortDirection.Desc) => query.OrderByDescending(notification => notification.Type).ThenByDescending(notification => notification.Id),
            (_, SortDirection.Asc) => query.OrderBy(notification => notification.CreatedAt).ThenByDescending(notification => notification.Id),
            _ => query.OrderByDescending(notification => notification.CreatedAt).ThenByDescending(notification => notification.Id)
        };
    }
    
}
