using TaskPilot.Application.Interfaces.Infrastructure.Messaging;
using TaskPilot.Application.Interfaces.Persistence.Messaging;
using TaskPilot.Application.Messaging;

namespace TaskPilot.Persistence.Messaging;

public sealed class PersistenceEventOutbox(IOutboxMessageRepository outboxMessageRepository) : IEventOutbox
{
    private readonly List<Func<IIntegrationEvent>> _pendingEventFactories = [];

    public Task EnqueueAsync(Func<IIntegrationEvent> eventFactory, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(eventFactory);
        _pendingEventFactories.Add(eventFactory);
        return Task.CompletedTask;
    }

    public async Task FlushAsync(CancellationToken cancellationToken)
    {
        foreach (var eventFactory in _pendingEventFactories)
        {
            await outboxMessageRepository.AddAsync(IntegrationEventEnvelope.Create(eventFactory()), cancellationToken);
        }

        _pendingEventFactories.Clear();
    }
}
