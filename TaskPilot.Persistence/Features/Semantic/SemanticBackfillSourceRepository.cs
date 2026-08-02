using Microsoft.EntityFrameworkCore;
using TaskPilot.Application.Features.Semantic.Contracts;
using TaskPilot.Domain.AI.Semantic;
using TaskPilot.Domain.Entities;
using TaskPilot.Domain.Knowledge;

namespace TaskPilot.Persistence.Features.Semantic;

public sealed class SemanticBackfillSourceRepository(AppDbContext db) : ISemanticBackfillSourcePort
{
    public Task<IReadOnlyList<SemanticBackfillSourceItem>> GetPageAsync(SemanticSourceType sourceType, int afterSourceId, int pageSize, CancellationToken cancellationToken)
    {
        var size = Math.Clamp(pageSize, 1, 500);
        return sourceType switch
        {
            SemanticSourceType.Task => GetTasksAsync(afterSourceId, size, cancellationToken),
            SemanticSourceType.Comment => GetCommentsAsync(afterSourceId, size, cancellationToken),
            SemanticSourceType.Note => GetNotesAsync(afterSourceId, size, cancellationToken),
            _ => Task.FromResult<IReadOnlyList<SemanticBackfillSourceItem>>([])
        };
    }

    private async Task<IReadOnlyList<SemanticBackfillSourceItem>> GetTasksAsync(int afterId, int size, CancellationToken ct)
    {
        return await db.Set<TaskItem>().Where(task => task.Id > afterId && task.Project!.Status != ProjectStatus.Archived && !task.Project.WorkSpace!.IsArchived).OrderBy(task => task.Id).Take(size).Select(task => new SemanticBackfillSourceItem(SemanticSourceType.Task, task.Id, task.Project!.WorkspaceId, task.ProjectId, task.Title + "\n" + (task.Description ?? string.Empty), task.UpdatedAt)).ToListAsync(ct);
    }
    private async Task<IReadOnlyList<SemanticBackfillSourceItem>> GetCommentsAsync(int afterId, int size, CancellationToken ct)
    {
        return await db.Set<Comment>().Where(comment => comment.Id > afterId && comment.TaskItem!.Project!.Status != ProjectStatus.Archived && !comment.TaskItem.Project.WorkSpace!.IsArchived).OrderBy(comment => comment.Id).Take(size).Select(comment => new SemanticBackfillSourceItem(SemanticSourceType.Comment, comment.Id, comment.TaskItem!.Project!.WorkspaceId, comment.TaskItem.ProjectId, comment.Content, comment.UpdatedAt)).ToListAsync(ct);
    }
    private async Task<IReadOnlyList<SemanticBackfillSourceItem>> GetNotesAsync(int afterId, int size, CancellationToken ct)
    {
        return await db.Set<Note>().Where(note => note.Id > afterId && db.Set<WorkSpace>().Any(workspace => workspace.Id == note.WorkspaceId && !workspace.IsArchived) && (note.ProjectId == null || db.Set<Project>().Any(project => project.Id == note.ProjectId && project.WorkspaceId == note.WorkspaceId && project.Status != ProjectStatus.Archived))).OrderBy(note => note.Id).Take(size).Select(note => new SemanticBackfillSourceItem(SemanticSourceType.Note, note.Id, note.WorkspaceId, note.ProjectId, note.Title + "\n" + note.Content, note.UpdatedAt)).ToListAsync(ct);
    }
}
