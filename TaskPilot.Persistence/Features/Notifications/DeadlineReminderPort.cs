using Microsoft.EntityFrameworkCore;
using TaskPilot.Application.Features.Notifications.Services;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Persistence.Features.Notifications;

public sealed class DeadlineReminderPort(AppDbContext dbContext) : IDeadlineReminderPort
{
    public async Task<IReadOnlyList<DeadlineReminderCandidate>> GetDueSoonAsync(
        DateTime fromUtc,
        DateTime untilUtc,
        CancellationToken cancellationToken) =>
        await dbContext.TaskItems
            .AsNoTracking()
            .Where(task =>
                task.AssignedUserId != null &&
                task.DueDate != null &&
                task.DueDate > fromUtc &&
                task.DueDate <= untilUtc &&
                task.Status != TaskItemStatus.Done &&
                task.Status != TaskItemStatus.Cancelled &&
                task.Project != null &&
                task.Project.Status == ProjectStatus.Active &&
                task.Project.WorkSpace != null &&
                !task.Project.WorkSpace.IsArchived)
            .Select(task => new DeadlineReminderCandidate(
                task.Id,
                task.AssignedUserId!.Value,
                task.Title,
                task.DueDate!.Value))
            .ToListAsync(cancellationToken);

    public async ValueTask<bool> AddIfMissingAsync(Notification notification, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Notifications.AnyAsync(
            item => item.UserId == notification.UserId && item.SourceEventId == notification.SourceEventId,
            cancellationToken);
        if (exists)
            return false;
        await dbContext.Notifications.AddAsync(notification, cancellationToken);
        return true;
    }
}
