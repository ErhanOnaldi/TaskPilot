using TaskPilot.Application.Interfaces.Infrastructure.Messaging;

namespace TaskPilot.Application.Events;

public sealed record WorkspaceMemberInvitedEvent(
    Guid EventId,
    int WorkspaceId,
    int InvitedUserId,
    int InvitedByUserId,
    DateTime OccurredAt) : IIntegrationEvent
{
    public Guid CorrelationId { get; init; } = EventId;
    public Guid? CausationId { get; init; }
    public string EventType => "workspace.member-invited";
    public int SchemaVersion => 1;
}
