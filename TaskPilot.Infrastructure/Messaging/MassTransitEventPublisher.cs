using MassTransit;
using TaskPilot.Application.Interfaces.Infrastructure.Messaging;

namespace TaskPilot.Infrastructure.Messaging;

public sealed class MassTransitEventPublisher(IPublishEndpoint publishEndpoint) : IEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken)
    {
        return publishEndpoint.Publish(@event!, cancellationToken);
    }
}
