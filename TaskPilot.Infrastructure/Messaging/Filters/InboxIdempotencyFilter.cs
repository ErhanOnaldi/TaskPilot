using MassTransit;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Persistence.Messaging;
using TaskPilot.Application.Messaging;

namespace TaskPilot.Infrastructure.Messaging.Filters;

public sealed class InboxIdempotencyFilter<TMessage>(
    IInboxMessageRepository inboxMessageRepository,
    IDateTimeProvider dateTimeProvider) : IFilter<ConsumeContext<TMessage>>
    where TMessage : class
{
    public async Task Send(ConsumeContext<TMessage> context, IPipe<ConsumeContext<TMessage>> next)
    {
        if (context.Message is not IntegrationEventEnvelope envelope)
        {
            await next.Send(context);
            return;
        }

        const string consumerName = "IntegrationEventConsumer";
        var eventId = envelope.EventId;
        var claim = await inboxMessageRepository.TryStartAsync(
            consumerName,
            eventId,
            dateTimeProvider.UtcNow,
            context.CancellationToken);
        if (claim == InboxClaimResult.AlreadyProcessed)
        {
            return;
        }
        if (claim == InboxClaimResult.Busy)
            throw new InvalidOperationException("Inbox message is currently leased by another consumer instance.");

        try
        {
            await next.Send(context);
            await inboxMessageRepository.MarkProcessedAsync(consumerName, eventId, dateTimeProvider.UtcNow, context.CancellationToken);
        }
        catch
        {
            await inboxMessageRepository.RemoveAsync(consumerName, eventId, context.CancellationToken);
            throw;
        }
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("inboxIdempotency");
    }
}
