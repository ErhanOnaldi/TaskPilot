namespace TaskPilot.Domain.Entities;

public class WorkSpace : AuditEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public bool IsArchived { get; set; }
    public int CreatedByUserId { get; set; }

    public User? CreatedByUser { get; set; }
    public ICollection<Project> Projects { get; set; } = new List<Project>();
    public ICollection<WorkspaceMember> Members { get; set; } = new List<WorkspaceMember>();
}