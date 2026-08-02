using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskPilot.Application.Events;
using TaskPilot.Application.Interfaces.Infrastructure.Messaging;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Application.Messaging;
using TaskPilot.Infrastructure.Messaging;
using TaskPilot.Persistence;
using TaskPilot.Persistence.Messaging;

namespace TaskPilot.IntegrationTests;

[Collection(TaskPilotIntegrationTestCollection.Name)]
public sealed class MessagingIntegrationTests(TaskPilotIntegrationEnvironment environment)
{
    [Fact]
    public async Task Pending_outbox_is_dispatched_and_duplicate_delivery_creates_one_notification()
    {
        using var assignee = await IntegrationTestApi.RegisterAsync(environment.Factory);
        using var assigner = await IntegrationTestApi.RegisterAsync(environment.Factory);
        var eventId = Guid.NewGuid();
        var assigned = new TaskAssignedEvent(
            eventId,
            TaskId: 900_001,
            ProjectId: 900_001,
            AssignedUserId: assignee.Auth.User.Id,
            AssignedByUserId: assigner.Auth.User.Id,
            OccurredAt: DateTime.UtcNow);

        await using (var scope = environment.Factory.Services.CreateAsyncScope())
        {
            var outbox = scope.ServiceProvider.GetRequiredService<IEventOutbox>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            await outbox.EnqueueAsync(() => assigned, CancellationToken.None);
            await unitOfWork.SaveChangesAsync();

            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var pending = await dbContext.Set<OutboxMessage>().SingleAsync(message => message.EventId == eventId);
            Assert.Null(pending.DispatchedAtUtc);
            Assert.Equal(0, pending.AttemptCount);
        }

        await using (var scope = environment.Factory.Services.CreateAsyncScope())
        {
            var dispatcher = scope.ServiceProvider.GetRequiredService<OutboxDispatcher>();
            Assert.True(await dispatcher.DispatchPendingAsync(500, CancellationToken.None) >= 1);
        }

        await WaitForProcessedInboxAsync(eventId);
        Assert.Equal(1, await CountNotificationsAsync(eventId));

        await using (var scope = environment.Factory.Services.CreateAsyncScope())
        {
            var publisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();
            await publisher.PublishAsync(IntegrationEventEnvelope.Create(assigned), CancellationToken.None);
        }

        await Eventually.UntilAsync(
            environment.IsIntegrationEventQueueIdleAsync,
            idle => idle,
            "the duplicate integration event delivery to drain from RabbitMQ");

        Assert.Equal(1, await CountNotificationsAsync(eventId));
        await using var verificationScope = environment.Factory.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await verificationDb.Set<InboxMessage>().CountAsync(message => message.EventId == eventId));
        var dispatched = await verificationDb.Set<OutboxMessage>().SingleAsync(message => message.EventId == eventId);
        Assert.NotNull(dispatched.DispatchedAtUtc);
        Assert.Equal(1, dispatched.AttemptCount);
    }

    private async Task WaitForProcessedInboxAsync(Guid eventId)
    {
        await Eventually.UntilAsync(
            async () =>
            {
                await using var scope = environment.Factory.Services.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                return await dbContext.Set<InboxMessage>()
                    .AnyAsync(message => message.EventId == eventId && message.ProcessedAtUtc != null);
            },
            processed => processed,
            $"inbox event {eventId} to be processed");
    }

    private async Task<int> CountNotificationsAsync(Guid eventId)
    {
        await using var scope = environment.Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await dbContext.Notifications.CountAsync(notification => notification.SourceEventId == eventId);
    }
}
