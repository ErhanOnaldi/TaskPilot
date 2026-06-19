namespace TaskPilot.Domain.Entities;

public class WorkspaceMember
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public int UserId { get; set; }
    public Role Role { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public WorkSpace? WorkSpace { get; set; }
    public User? User { get; set; }
}