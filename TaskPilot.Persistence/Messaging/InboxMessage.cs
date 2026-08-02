namespace TaskPilot.Persistence.Messaging;

public sealed class InboxMessage
{
    public Guid Id { get; set; }
    public string ConsumerName { get; set; } = null!;
    public Guid EventId { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public Guid? LockId { get; set; }
    public DateTime? LockedUntilUtc { get; set; }
}
