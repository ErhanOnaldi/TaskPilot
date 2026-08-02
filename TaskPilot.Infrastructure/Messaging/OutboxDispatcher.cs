using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Infrastructure.Messaging;
using TaskPilot.Application.Interfaces.Persistence.Messaging;
using Microsoft.Extensions.Logging;

namespace TaskPilot.Infrastructure.Messaging;

public sealed class OutboxDispatcher(
    IOutboxMessageRepository outboxMessageRepository,
    IIntegrationEventPublisher eventPublisher,
    IDateTimeProvider dateTimeProvider,
    Microsoft.Extensions.Logging.ILogger<OutboxDispatcher> logger)
{
    public async Task<int> DispatchPendingAsync(int maxCount, CancellationToken cancellationToken)
    {
        var lockId = Guid.NewGuid();
        var messages = await outboxMessageRepository.ClaimUndispatchedAsync(
            maxCount,
            dateTimeProvider.UtcNow,
            lockId,
            TimeSpan.FromSeconds(30),
            cancellationToken);
        foreach (var message in messages)
        {
            try
            {
                await eventPublisher.PublishAsync(message.Envelope, cancellationToken);
                await outboxMessageRepository.MarkDispatchedAsync(message.MessageId, dateTimeProvider.UtcNow, cancellationToken);
            }
            catch (Exception exception)
            {
                await outboxMessageRepository.RecordFailureAsync(message.MessageId, exception.Message, dateTimeProvider.UtcNow, cancellationToken);
                if (message.AttemptCount >= 9)
                {
                    logger.LogError(exception, "Outbox message {MessageId} became a poison message after {AttemptCount} attempts.", message.MessageId, message.AttemptCount + 1);
                }
                throw;
            }
        }

        return messages.Count;
    }
}
