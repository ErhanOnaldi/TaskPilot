namespace TaskPilot.Application.Interfaces.Infrastructure.Messaging;

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken);
}