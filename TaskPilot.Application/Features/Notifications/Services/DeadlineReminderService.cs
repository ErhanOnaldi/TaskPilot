using System.Security.Cryptography;
using System.Text;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.Notifications.Services;

public sealed record DeadlineReminderCandidate(int TaskId, int UserId, string TaskTitle, DateTime DueDateUtc);

public interface IDeadlineReminderPort
{
    Task<IReadOnlyList<DeadlineReminderCandidate>> GetDueSoonAsync(
        DateTime fromUtc,
        DateTime untilUtc,
        CancellationToken cancellationToken);

    ValueTask<bool> AddIfMissingAsync(Notification notification, CancellationToken cancellationToken);
}

public interface IDeadlineReminderService
{
    Task<int> CreateDueSoonNotificationsAsync(TimeSpan window, CancellationToken cancellationToken);
}

public sealed class DeadlineReminderService(
    IDeadlineReminderPort reminders,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock) : IDeadlineReminderService
{
    public async Task<int> CreateDueSoonNotificationsAsync(TimeSpan window, CancellationToken cancellationToken)
    {
        if (window <= TimeSpan.Zero || window > TimeSpan.FromDays(7))
            throw new ArgumentOutOfRangeException(nameof(window));

        var now = clock.UtcNow;
        var candidates = await reminders.GetDueSoonAsync(now, now.Add(window), cancellationToken);
        var createdCount = 0;
        foreach (var candidate in candidates)
        {
            if (await reminders.AddIfMissingAsync(new Notification
            {
                UserId = candidate.UserId,
                Type = "DeadlineApproaching",
                Title = "Task deadline is approaching",
                Message = $"The deadline for '{candidate.TaskTitle}' is approaching.",
                RelatedEntityId = candidate.TaskId,
                SourceEventId = CreateSourceEventId(candidate),
                CreatedAt = now,
                UpdatedAt = now
            }, cancellationToken))
                createdCount++;
        }

        if (createdCount > 0)
            await unitOfWork.SaveChangesAsync(cancellationToken);

        return createdCount;
    }

    internal static Guid CreateSourceEventId(DeadlineReminderCandidate candidate)
    {
        var value = $"deadline-reminder:v1:{candidate.TaskId}:{candidate.UserId}:{candidate.DueDateUtc.ToUniversalTime():O}";
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(Encoding.UTF8.GetBytes(value), hash);
        return new Guid(hash[..16]);
    }
}
