using MassTransit;
using TaskPilot.Application.Interfaces.Infrastructure.Messaging;
using TaskPilot.Application.Messaging;

namespace TaskPilot.Infrastructure.Messaging;

public sealed class MassTransitIntegrationEventPublisher(IPublishEndpoint publishEndpoint) : IIntegrationEventPublisher
{
    public Task PublishAsync(IntegrationEventEnvelope envelope, CancellationToken cancellationToken)
    {
        return publishEndpoint.Publish(envelope, cancellationToken);
    }
}
