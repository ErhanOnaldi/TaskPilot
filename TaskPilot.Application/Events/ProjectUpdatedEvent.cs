namespace TaskPilot.Application.Events;

public sealed record ProjectUpdatedEvent(
    Guid ProjectId,
    Guid UpdatedByUserId,
    DateTime OccurredAt
);