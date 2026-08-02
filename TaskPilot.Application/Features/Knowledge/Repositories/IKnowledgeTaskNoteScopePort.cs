namespace TaskPilot.Application.Features.Knowledge.Repositories;

/// <summary>Persistence-owned scope check keeps task, project and note joins out of the command service.</summary>
public interface IKnowledgeTaskNoteScopePort
{
    Task<TaskNoteScope?> GetActiveScopeAsync(int workspaceId, int taskId, int noteId, CancellationToken cancellationToken);
}

public sealed record TaskNoteScope(int WorkspaceId, int TaskId, int NoteId, int? TaskProjectId, int? NoteProjectId);
