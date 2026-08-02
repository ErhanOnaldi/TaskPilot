namespace TaskPilot.Domain.Knowledge;

public sealed class NoteTagAssignment
{
    public int Id { get; set; }
    public int NoteId { get; set; }
    public int KnowledgeTagId { get; set; }
}
