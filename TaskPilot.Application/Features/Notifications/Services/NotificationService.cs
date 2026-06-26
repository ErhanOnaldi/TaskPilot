using System.Net;
using AutoMapper;
using TaskPilot.Application.Features.Notifications.Dtos;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.Notifications.Services;

public class NotificationService(
    IGenericRepository<Notification> notificationRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IMapper mapper) : INotificationService
{
    public Task<ServiceResult<List<NotificationResponse>>> GetNotificationsAsync(CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var notifications = notificationRepository
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        return Task.FromResult(
            ServiceResult<List<NotificationResponse>>.Success(mapper.Map<List<NotificationResponse>>(notifications)));
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
        var notifications = (await notificationRepository.GetAllAsync())
            .Where(x => x.UserId == userId && !x.IsRead)
            .ToList();
        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.UpdatedAt = DateTime.UtcNow;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

}
