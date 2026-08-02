using TaskPilot.Application.Features.Notifications.Services;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Tests;

public sealed class DeadlineReminderServiceTests
{
    [Fact]
    public async Task Creates_idempotency_keyed_notification_for_each_due_task()
    {
        var now = new DateTime(2026, 8, 2, 12, 0, 0, DateTimeKind.Utc);
        var port = new FakePort
        {
            Candidates =
            [
                new DeadlineReminderCandidate(10, 20, "Release", now.AddHours(4))
            ]
        };
        var unitOfWork = new FakeUnitOfWork();
        var service = new DeadlineReminderService(port, unitOfWork, new FakeClock(now));

        var count = await service.CreateDueSoonNotificationsAsync(TimeSpan.FromHours(24), CancellationToken.None);

        Assert.Equal(1, count);
        var notification = Assert.Single(port.Notifications);
        Assert.Equal("DeadlineApproaching", notification.Type);
        Assert.Equal(10, notification.RelatedEntityId);
        Assert.NotNull(notification.SourceEventId);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Repeated_scan_uses_same_source_event_id()
    {
        var now = new DateTime(2026, 8, 2, 12, 0, 0, DateTimeKind.Utc);
        var candidate = new DeadlineReminderCandidate(10, 20, "Release", now.AddHours(4));
        var port = new FakePort { Candidates = [candidate] };
        var service = new DeadlineReminderService(port, new FakeUnitOfWork(), new FakeClock(now));

        await service.CreateDueSoonNotificationsAsync(TimeSpan.FromHours(24), CancellationToken.None);
        await service.CreateDueSoonNotificationsAsync(TimeSpan.FromHours(24), CancellationToken.None);

        Assert.Single(port.Notifications);
    }

    private sealed class FakePort : IDeadlineReminderPort
    {
        public IReadOnlyList<DeadlineReminderCandidate> Candidates { get; init; } = [];
        public List<Notification> Notifications { get; } = [];

        public Task<IReadOnlyList<DeadlineReminderCandidate>> GetDueSoonAsync(
            DateTime fromUtc,
            DateTime untilUtc,
            CancellationToken cancellationToken) => Task.FromResult(Candidates);

        public ValueTask<bool> AddIfMissingAsync(Notification notification, CancellationToken cancellationToken)
        {
            if (Notifications.All(existing =>
                    existing.UserId != notification.UserId || existing.SourceEventId != notification.SourceEventId))
            {
                Notifications.Add(notification);
                return ValueTask.FromResult(true);
            }
            return ValueTask.FromResult(false);
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }

    private sealed record FakeClock(DateTime UtcNow) : IDateTimeProvider;
}
