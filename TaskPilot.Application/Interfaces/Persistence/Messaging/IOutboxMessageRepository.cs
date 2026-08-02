using TaskPilot.Application.Messaging;

namespace TaskPilot.Application.Interfaces.Persistence.Messaging;

public interface IOutboxMessageRepository
{
    ValueTask AddAsync(IntegrationEventEnvelope envelope, CancellationToken cancellationToken);

    Task<IReadOnlyList<OutboxDispatchItem>> ClaimUndispatchedAsync(
        int maxCount,
        DateTime utcNow,
        Guid lockId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);

    Task MarkDispatchedAsync(Guid messageId, DateTime utcNow, CancellationToken cancellationToken);

    Task RecordFailureAsync(Guid messageId, string error, DateTime utcNow, CancellationToken cancellationToken);
}

public sealed record OutboxDispatchItem(Guid MessageId, IntegrationEventEnvelope Envelope, int AttemptCount);
