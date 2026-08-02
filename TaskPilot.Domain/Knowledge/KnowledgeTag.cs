using TaskPilot.Domain.Entities;

namespace TaskPilot.Domain.Knowledge;

public sealed class KnowledgeTag : AuditEntity
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public int? ProjectId { get; set; }
    public string Name { get; set; } = null!;
    public string NormalizedSlug { get; set; } = null!;
}
