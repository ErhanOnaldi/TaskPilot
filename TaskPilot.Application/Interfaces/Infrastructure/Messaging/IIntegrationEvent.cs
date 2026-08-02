namespace TaskPilot.Application.Interfaces.Infrastructure.Messaging;

public interface IIntegrationEvent
{
    Guid EventId { get; }
    Guid CorrelationId { get; }
    Guid? CausationId { get; }
    string EventType { get; }
    int SchemaVersion { get; }
    DateTime OccurredAt { get; }
}
