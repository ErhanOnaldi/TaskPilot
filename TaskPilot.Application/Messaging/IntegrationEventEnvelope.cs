using System.Text.Json;
using TaskPilot.Application.Interfaces.Infrastructure.Messaging;

namespace TaskPilot.Application.Messaging;

public sealed record IntegrationEventEnvelope(
    Guid EventId,
    Guid CorrelationId,
    Guid? CausationId,
    string EventType,
    int SchemaVersion,
    DateTime OccurredAt,
    string Payload)
{
    public static IntegrationEventEnvelope Create(IIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return new IntegrationEventEnvelope(
            integrationEvent.EventId,
            integrationEvent.CorrelationId,
            integrationEvent.CausationId,
            integrationEvent.EventType,
            integrationEvent.SchemaVersion,
            integrationEvent.OccurredAt,
            JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType()));
    }
}
