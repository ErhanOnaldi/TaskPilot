using TaskPilot.Application.Messaging;

namespace TaskPilot.Application.Interfaces.Infrastructure.Messaging;

public interface IIntegrationEventPublisher
{
    Task PublishAsync(IntegrationEventEnvelope envelope, CancellationToken cancellationToken);
}
