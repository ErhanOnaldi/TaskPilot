using Microsoft.EntityFrameworkCore;
using TaskPilot.Application.Interfaces.Persistence.Messaging;
using TaskPilot.Persistence.Messaging;

namespace TaskPilot.Persistence.EntityRepositories;

public sealed class InboxMessageRepository(AppDbContext dbContext) : IInboxMessageRepository
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(2);

    public async Task<InboxClaimResult> TryStartAsync(
        string consumerName,
        Guid eventId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var lockId = Guid.NewGuid();
        var reclaimed = await dbContext.Set<InboxMessage>()
            .Where(message => message.ConsumerName == consumerName &&
                              message.EventId == eventId &&
                              message.ProcessedAtUtc == null &&
                              (message.LockedUntilUtc == null || message.LockedUntilUtc < utcNow))
            .ExecuteUpdateAsync(update => update
                .SetProperty(message => message.LockId, lockId)
                .SetProperty(message => message.LockedUntilUtc, utcNow.Add(LeaseDuration))
                .SetProperty(message => message.ReceivedAtUtc, utcNow), cancellationToken);
        if (reclaimed == 1) return InboxClaimResult.Acquired;

        var state = await dbContext.Set<InboxMessage>()
            .Where(message => message.ConsumerName == consumerName && message.EventId == eventId)
            .Select(message => new { message.ProcessedAtUtc, message.LockedUntilUtc })
            .SingleOrDefaultAsync(cancellationToken);
        if (state?.ProcessedAtUtc is not null) return InboxClaimResult.AlreadyProcessed;
        if (state is not null) return InboxClaimResult.Busy;

        var inboxMessage = new InboxMessage
        {
            Id = Guid.NewGuid(),
            ConsumerName = consumerName,
            EventId = eventId,
            ReceivedAtUtc = utcNow,
            LockId = lockId,
            LockedUntilUtc = utcNow.Add(LeaseDuration)
        };
        await dbContext.Set<InboxMessage>().AddAsync(inboxMessage, cancellationToken);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return InboxClaimResult.Acquired;
        }
        catch (DbUpdateException)
        {
            dbContext.Entry(inboxMessage).State = EntityState.Detached;
            state = await dbContext.Set<InboxMessage>()
                .Where(message => message.ConsumerName == consumerName && message.EventId == eventId)
                .Select(message => new { message.ProcessedAtUtc, message.LockedUntilUtc })
                .SingleAsync(cancellationToken);
            return state.ProcessedAtUtc is not null ? InboxClaimResult.AlreadyProcessed : InboxClaimResult.Busy;
        }
    }

    public Task MarkProcessedAsync(
        string consumerName,
        Guid eventId,
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        dbContext.Set<InboxMessage>()
            .Where(message => message.ConsumerName == consumerName && message.EventId == eventId)
            .ExecuteUpdateAsync(update => update
                .SetProperty(message => message.ProcessedAtUtc, utcNow)
                .SetProperty(message => message.LockId, (Guid?)null)
                .SetProperty(message => message.LockedUntilUtc, (DateTime?)null), cancellationToken);

    public Task RemoveAsync(string consumerName, Guid eventId, CancellationToken cancellationToken) =>
        dbContext.Set<InboxMessage>()
            .Where(message => message.ConsumerName == consumerName &&
                              message.EventId == eventId &&
                              message.ProcessedAtUtc == null)
            .ExecuteDeleteAsync(cancellationToken);
}
