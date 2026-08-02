using TaskPilot.Application.Interfaces.Infrastructure.Messaging;

namespace TaskPilot.Application.Events;

public sealed record TaskCreatedEvent(
    Guid EventId,
    int TaskId,
    int ProjectId,
    int CreatedByUserId,
    int? AssignedUserId,
    DateTime OccurredAt) : IIntegrationEvent
{
    public Guid CorrelationId { get; init; } = EventId;
    public Guid? CausationId { get; init; }
    public string EventType => "task.created";
    public int SchemaVersion => 1;
}
