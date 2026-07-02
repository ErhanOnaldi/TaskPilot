namespace TaskPilot.Application.Events;

public sealed record CommentAddedEvent(
    Guid EventId,
    int CommentId,
    int TaskId,
    int ProjectId,
    int AuthorUserId,
    DateTime OccurredAt
);
