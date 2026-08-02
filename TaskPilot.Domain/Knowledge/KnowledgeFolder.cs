using TaskPilot.Domain.Entities;

namespace TaskPilot.Domain.Knowledge;

public sealed class KnowledgeFolder : AuditEntity
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public int? ProjectId { get; set; }
    public int? ParentFolderId { get; set; }
    public string Name { get; set; } = null!;
    public string NormalizedSlug { get; set; } = null!;
}
