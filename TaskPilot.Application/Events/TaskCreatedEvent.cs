namespace TaskPilot.Application.Events;

public sealed record TaskCreatedEvent(
    Guid EventId,
    int TaskId,
    int ProjectId,
    int CreatedByUserId,
    int? AssignedUserId,
    DateTime OccurredAt
);
