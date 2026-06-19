namespace TaskPilot.Domain.Entities;

public class TaskItem : AuditEntity
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Todo;
    public TaskItemPriority Priority { get; set; } = TaskItemPriority.Medium;
    public DateTime? DueDate { get; set; }
    public int? AssignedUserId { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime? CompletedAt { get; set; }

    public Project? Project { get; set; }
    public User? AssignedUser { get; set; }
    public User? CreatedByUser { get; set; }
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<TaskLabel> TaskLabels { get; set; } = new List<TaskLabel>();
}