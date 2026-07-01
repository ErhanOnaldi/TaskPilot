using System.Net;
using AutoMapper;
using TaskPilot.Application.Common.Pagination;
using TaskPilot.Application.Features.Notifications.Dtos;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Application.Interfaces.Persistence.Notifications;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.Notifications.Services;

public class NotificationService(
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IMapper mapper) : INotificationService
{
    public async Task<ServiceResult<PagedResponse<NotificationResponse>>> GetNotificationsAsync(
        NotificationQueryParameters query,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var notifications = await notificationRepository.GetNotificationsByUserIdAsync(userId, query, cancellationToken);
        var response = PagedResponse<NotificationResponse>.Create(
            mapper.Map<List<NotificationResponse>>(notifications.Items),
            notifications.PageNumber,
            notifications.PageSize,
            notifications.TotalCount);

        return ServiceResult<PagedResponse<NotificationResponse>>.Success(response);
    }

    public async Task<ServiceResult> MarkAsReadAsync(int notificationId, CancellationToken cancellationToken)
    {
        var notification = await notificationRepository.GetByIdAsync(notificationId);
        if (notification is null || notification.UserId != currentUserService.GetRequiredUserId())
        {
            return ServiceResult.Fail("Notification not found.", HttpStatusCode.NotFound);
        }

        notification.IsRead = true;
        notification.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    public async Task<ServiceResult> MarkAllAsReadAsync(CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var notifications = await notificationRepository.GetUnreadNotificationsByUserIdAsync(userId, cancellationToken);
        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.UpdatedAt = DateTime.UtcNow;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

}
