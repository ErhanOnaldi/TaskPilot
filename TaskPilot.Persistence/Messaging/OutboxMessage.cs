namespace TaskPilot.Persistence.Messaging;

public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid CorrelationId { get; set; }
    public Guid? CausationId { get; set; }
    public string EventType { get; set; } = null!;
    public int SchemaVersion { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string Payload { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? DispatchedAtUtc { get; set; }
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
    public Guid? LockId { get; set; }
    public DateTime? LockedUntilUtc { get; set; }
    public DateTime? DeadLetteredAtUtc { get; set; }
}
