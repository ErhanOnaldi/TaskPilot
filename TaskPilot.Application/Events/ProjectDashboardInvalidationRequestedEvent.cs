using TaskPilot.Application.Interfaces.Infrastructure.Messaging;

namespace TaskPilot.Application.Events;

public sealed record ProjectDashboardInvalidationRequestedEvent(
    Guid EventId,
    int ProjectId,
    DateTime OccurredAt) : IIntegrationEvent
{
    public Guid CorrelationId { get; init; } = EventId;
    public Guid? CausationId { get; init; }
    public string EventType => "project.dashboard-invalidation.requested";
    public int SchemaVersion => 1;
}
