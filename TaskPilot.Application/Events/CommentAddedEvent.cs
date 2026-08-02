using TaskPilot.Application.Interfaces.Infrastructure.Messaging;

namespace TaskPilot.Application.Events;

public sealed record CommentAddedEvent(
    Guid EventId,
    int CommentId,
    int TaskId,
    int ProjectId,
    int AuthorUserId,
    DateTime OccurredAt) : IIntegrationEvent
{
    public Guid CorrelationId { get; init; } = EventId;
    public Guid? CausationId { get; init; }
    public string EventType => "comment.added";
    public int SchemaVersion => 1;
}
