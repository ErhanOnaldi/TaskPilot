namespace TaskPilot.Domain.Entities;

public class Label : AuditEntity
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Name { get; set; } = null!;
    public string Color { get; set; } = "#3B82F6";

    public Project? Project { get; set; }
    public ICollection<TaskLabel> TaskLabels { get; set; } = new List<TaskLabel>();
}