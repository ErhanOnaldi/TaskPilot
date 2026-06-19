namespace TaskPilot.Domain.Entities;

public class ProjectMember
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int UserId { get; set; }
    public ProjectRole Role { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public Project? Project { get; set; }
    public User? User { get; set; }
}