using Microsoft.EntityFrameworkCore;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Persistence.Messaging;
using TaskPilot.Application.Messaging;
using TaskPilot.Persistence.Messaging;

namespace TaskPilot.Persistence.EntityRepositories;

public sealed class OutboxMessageRepository(AppDbContext dbContext, IDateTimeProvider dateTimeProvider) : IOutboxMessageRepository
{
    public async ValueTask AddAsync(IntegrationEventEnvelope envelope, CancellationToken cancellationToken)
    {
        await dbContext.Set<OutboxMessage>().AddAsync(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventId = envelope.EventId,
            CorrelationId = envelope.CorrelationId,
            CausationId = envelope.CausationId,
            EventType = envelope.EventType,
            SchemaVersion = envelope.SchemaVersion,
            OccurredAtUtc = envelope.OccurredAt,
            Payload = envelope.Payload,
            CreatedAtUtc = dateTimeProvider.UtcNow
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<OutboxDispatchItem>> ClaimUndispatchedAsync(
        int maxCount,
        DateTime utcNow,
        Guid lockId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var messages = await dbContext.Set<OutboxMessage>()
            .FromSqlInterpolated($$"""
                SELECT * FROM "OutboxMessages"
                WHERE "DispatchedAtUtc" IS NULL
                  AND "DeadLetteredAtUtc" IS NULL
                  AND ("LockedUntilUtc" IS NULL OR "LockedUntilUtc" < {{utcNow}})
                ORDER BY "CreatedAtUtc"
                FOR UPDATE SKIP LOCKED
                LIMIT {{Math.Clamp(maxCount, 1, 500)}}
                """)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            message.LockId = lockId;
            message.LockedUntilUtc = utcNow.Add(leaseDuration);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return messages.Select(message => new OutboxDispatchItem(message.Id,
            new IntegrationEventEnvelope(message.EventId, message.CorrelationId, message.CausationId, message.EventType,
                message.SchemaVersion, message.OccurredAtUtc, message.Payload), message.AttemptCount)).ToList();
    }

    public Task MarkDispatchedAsync(Guid messageId, DateTime utcNow, CancellationToken cancellationToken)
    {
        return dbContext.Set<OutboxMessage>().Where(message => message.Id == messageId && message.DispatchedAtUtc == null)
            .ExecuteUpdateAsync(update => update
                .SetProperty(message => message.DispatchedAtUtc, utcNow)
                .SetProperty(message => message.AttemptCount, message => message.AttemptCount + 1)
                .SetProperty(message => message.LockId, (Guid?)null)
                .SetProperty(message => message.LockedUntilUtc, (DateTime?)null)
                .SetProperty(message => message.LastError, (string?)null), cancellationToken);
    }

    public Task RecordFailureAsync(Guid messageId, string error, DateTime utcNow, CancellationToken cancellationToken)
    {
        return dbContext.Set<OutboxMessage>().Where(message => message.Id == messageId && message.DispatchedAtUtc == null)
            .ExecuteUpdateAsync(update => update
                .SetProperty(message => message.AttemptCount, message => message.AttemptCount + 1)
                .SetProperty(message => message.DeadLetteredAtUtc, message => message.AttemptCount >= 9 ? utcNow : null)
                .SetProperty(message => message.LastError, error[..Math.Min(error.Length, 4000)]), cancellationToken);
    }
}
