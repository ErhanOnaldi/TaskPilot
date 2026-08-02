namespace TaskPilot.Domain.Knowledge;

public sealed class TaskNoteLink
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public int TaskId { get; set; }
    public int NoteId { get; set; }
}
