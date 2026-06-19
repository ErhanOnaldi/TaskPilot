namespace TaskPilot.Domain.Entities;

public class Comment : AuditEntity
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public int UserId { get; set; }
    public string Content { get; set; } = null!;

    public TaskItem? TaskItem { get; set; }
    public User? User { get; set; }
}