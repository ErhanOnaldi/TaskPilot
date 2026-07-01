using TaskPilot.Application.Common.Pagination;
using TaskPilot.Application.Features.Notifications.Dtos;

namespace TaskPilot.Application.Features.Notifications.Services;

public interface INotificationService
{
    Task<ServiceResult<PagedResponse<NotificationResponse>>> GetNotificationsAsync(
        NotificationQueryParameters query,
        CancellationToken cancellationToken);
    Task<ServiceResult> MarkAsReadAsync(int notificationId, CancellationToken cancellationToken);
    Task<ServiceResult> MarkAllAsReadAsync(CancellationToken cancellationToken);
}
