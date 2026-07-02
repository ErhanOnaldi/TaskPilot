namespace TaskPilot.Domain.Entities;

public class Notification : AuditEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Type { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public bool IsRead { get; set; }
    public int? RelatedEntityId { get; set; }
    public Guid? SourceEventId { get; set; }
    
    public User? User { get; set; }
}