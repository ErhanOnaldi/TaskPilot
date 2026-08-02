namespace TaskPilot.Domain.Knowledge;

public sealed class NoteLink
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public int SourceNoteId { get; set; }
    public Note? SourceNote { get; set; }
    public int? TargetNoteId { get; set; }
    public Note? TargetNote { get; set; }
    public string TargetNormalizedSlug { get; set; } = null!;
    public string? Alias { get; set; }
}
