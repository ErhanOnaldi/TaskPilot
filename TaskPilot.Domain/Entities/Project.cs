namespace TaskPilot.Domain.Entities;

public class Project : AuditEntity
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int CreatedByUserId { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Active;

    public WorkSpace? WorkSpace { get; set; }
    public User? CreatedByUser { get; set; }
    public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    public ICollection<Label> Labels { get; set; } = new List<Label>();
    public ICollection<AiSuggestion> AiSuggestions { get; set; } = new List<AiSuggestion>();
}