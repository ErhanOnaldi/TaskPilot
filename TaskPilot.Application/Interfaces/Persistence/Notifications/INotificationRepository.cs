using TaskPilot.Application.Common.Pagination;
using TaskPilot.Application.Features.Notifications.Dtos;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Interfaces.Persistence.Notifications;

public interface INotificationRepository : IGenericRepository<Notification>
{
    Task<PagedResponse<Notification>> GetNotificationsByUserIdAsync(
        int userId,
        NotificationQueryParameters query,
        CancellationToken cancellationToken);

    Task<List<Notification>> GetUnreadNotificationsByUserIdAsync(
        int userId,
        CancellationToken cancellationToken);
}
