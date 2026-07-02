namespace TaskPilot.Application.Events;

public sealed record TaskAssignedEvent(
    Guid EventId,
    int TaskId,
    int ProjectId,
    int AssignedUserId,
    int AssignedByUserId,
    DateTime OccurredAt
);
