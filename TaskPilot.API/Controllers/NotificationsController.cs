using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPilot.Application.Features.Notifications.Dtos;
using TaskPilot.Application.Features.Notifications.Services;

namespace TaskPilot.API.Controllers;

[Route("api/notifications")]
[ApiController]
[Authorize]
public class NotificationsController(INotificationService notificationService) : CustomBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] NotificationQueryParameters query,
        CancellationToken cancellationToken)
    {
        return CreateActionResult(await notificationService.GetNotificationsAsync(query, cancellationToken));
    }

    [HttpPatch("{notificationId:int}/read")]
    public async Task<IActionResult> MarkAsRead([FromRoute] int notificationId, CancellationToken cancellationToken)
    {
        return CreateActionResult(await notificationService.MarkAsReadAsync(notificationId, cancellationToken));
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        return CreateActionResult(await notificationService.MarkAllAsReadAsync(cancellationToken));
    }
}
