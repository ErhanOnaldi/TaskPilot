using TaskPilot.Application.Interfaces.Infrastructure.Messaging;

namespace TaskPilot.Application.Events;

public sealed record TaskAssignedEvent(
    Guid EventId,
    int TaskId,
    int ProjectId,
    int AssignedUserId,
    int AssignedByUserId,
    DateTime OccurredAt) : IIntegrationEvent
{
    public Guid CorrelationId { get; init; } = EventId;
    public Guid? CausationId { get; init; }
    public string EventType => "task.assigned";
    public int SchemaVersion => 1;
}
