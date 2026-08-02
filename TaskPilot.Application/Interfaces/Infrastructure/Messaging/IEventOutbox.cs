namespace TaskPilot.Application.Interfaces.Infrastructure.Messaging;

/// <summary>
/// Defers integration-event persistence until the unit of work has generated any
/// database identifiers required by the event payload.
/// </summary>
public interface IEventOutbox
{
    Task EnqueueAsync(Func<IIntegrationEvent> eventFactory, CancellationToken cancellationToken);

    Task FlushAsync(CancellationToken cancellationToken);
}
