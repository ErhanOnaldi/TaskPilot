using TaskPilot.Domain.Entities;

namespace TaskPilot.Domain.Knowledge;

public sealed class NoteRevision : AuditEntity
{
    public int Id { get; set; }
    public int NoteId { get; set; }
    public Note? Note { get; set; }
    public int RevisionNumber { get; set; }
    public int CreatedByUserId { get; set; }
    public string Title { get; set; } = null!;
    public string NormalizedSlug { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
    public int? RestoredFromRevisionId { get; set; }
}
