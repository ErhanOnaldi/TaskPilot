namespace TaskPilot.Application.Interfaces.Persistence.Messaging;

public enum InboxClaimResult
{
    Acquired,
    AlreadyProcessed,
    Busy
}

public interface IInboxMessageRepository
{
    Task<InboxClaimResult> TryStartAsync(string consumerName, Guid eventId, DateTime utcNow, CancellationToken cancellationToken);

    Task MarkProcessedAsync(string consumerName, Guid eventId, DateTime utcNow, CancellationToken cancellationToken);

    Task RemoveAsync(string consumerName, Guid eventId, CancellationToken cancellationToken);
}
