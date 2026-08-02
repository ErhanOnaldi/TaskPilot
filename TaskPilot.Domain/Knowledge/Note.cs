using TaskPilot.Domain.Entities;

namespace TaskPilot.Domain.Knowledge;

public sealed class Note : AuditEntity
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public int? ProjectId { get; set; }
    public int? FolderId { get; set; }
    public int CreatedByUserId { get; set; }
    public string Title { get; set; } = null!;
    public string NormalizedSlug { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
}
