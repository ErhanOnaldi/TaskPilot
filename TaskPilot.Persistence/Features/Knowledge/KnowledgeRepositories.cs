using Microsoft.EntityFrameworkCore;
using TaskPilot.Application.Features.Knowledge.ReadModels;
using TaskPilot.Application.Features.Knowledge.Repositories;
using TaskPilot.Domain.Knowledge;
using TaskPilot.Domain.Entities;
using NpgsqlTypes;

namespace TaskPilot.Persistence.Features.Knowledge;

public sealed class KnowledgeFolderRepository(AppDbContext db) : IKnowledgeFolderRepository
{
    public ValueTask<KnowledgeFolder?> GetByIdAsync(int folderId, CancellationToken cancellationToken) => db.Set<KnowledgeFolder>().FindAsync([folderId], cancellationToken);
    public async Task<IReadOnlyList<KnowledgeFolder>> GetByWorkspaceAsync(int workspaceId, int? projectId, CancellationToken cancellationToken) =>
        await db.Set<KnowledgeFolder>()
            .Where(x => x.WorkspaceId == workspaceId && (!projectId.HasValue || x.ProjectId == projectId))
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    public Task<bool> ExistsBySlugAsync(int workspaceId, int? projectId, string slug, CancellationToken cancellationToken) => db.Set<KnowledgeFolder>().AnyAsync(x => x.WorkspaceId == workspaceId && x.ProjectId == projectId && x.NormalizedSlug == slug, cancellationToken);
    public Task<bool> ExistsBySlugExceptFolderAsync(int workspaceId, int? projectId, int folderIdToExclude, string slug, CancellationToken cancellationToken) =>
        db.Set<KnowledgeFolder>().AnyAsync(x => x.WorkspaceId == workspaceId && x.ProjectId == projectId && x.Id != folderIdToExclude && x.NormalizedSlug == slug, cancellationToken);
    public async ValueTask AddAsync(KnowledgeFolder folder, CancellationToken cancellationToken) => await db.Set<KnowledgeFolder>().AddAsync(folder, cancellationToken);
    public Task DeleteAsync(KnowledgeFolder folder, CancellationToken cancellationToken)
    {
        db.Set<KnowledgeFolder>().Remove(folder);
        return Task.CompletedTask;
    }
}

public sealed class KnowledgeNoteRepository(AppDbContext db) : INoteRepository
{
    public ValueTask<Note?> GetByIdAsync(int noteId, CancellationToken cancellationToken) => db.Set<Note>().FindAsync([noteId], cancellationToken);
    public Task<Note?> GetBySlugAsync(int workspaceId, string slug, CancellationToken cancellationToken) => db.Set<Note>().SingleOrDefaultAsync(x => x.WorkspaceId == workspaceId && x.NormalizedSlug == slug, cancellationToken);
    public Task<bool> ExistsBySlugAsync(int workspaceId, int excludeId, string slug, CancellationToken cancellationToken) => db.Set<Note>().AnyAsync(x => x.WorkspaceId == workspaceId && x.Id != excludeId && x.NormalizedSlug == slug, cancellationToken);
    public async ValueTask AddAsync(Note note, CancellationToken cancellationToken) => await db.Set<Note>().AddAsync(note, cancellationToken);
    public Task DeleteAsync(Note note, CancellationToken cancellationToken)
    {
        db.Set<Note>().Remove(note);
        return Task.CompletedTask;
    }
}

public sealed class KnowledgeScopeValidationRepository(AppDbContext db) : IKnowledgeScopeValidationPort
{
    public Task<bool> IsProjectInWorkspaceAsync(int workspaceId, int? projectId, CancellationToken cancellationToken) =>
        projectId is null
            ? Task.FromResult(true)
            : db.Set<Project>().AnyAsync(x => x.Id == projectId && x.WorkspaceId == workspaceId && x.Status != ProjectStatus.Archived, cancellationToken);

    public Task<bool> IsFolderInWorkspaceAsync(int workspaceId, int? projectId, int? folderId, CancellationToken cancellationToken) =>
        folderId is null
            ? Task.FromResult(true)
            : db.Set<KnowledgeFolder>().AnyAsync(x => x.Id == folderId && x.WorkspaceId == workspaceId && x.ProjectId == projectId, cancellationToken);

    public Task<bool> IsParentFolderInWorkspaceAsync(int workspaceId, int? projectId, int? parentFolderId, CancellationToken cancellationToken) =>
        IsFolderInWorkspaceAsync(workspaceId, projectId, parentFolderId, cancellationToken);
}

public sealed class KnowledgeNoteRevisionRepository(AppDbContext db) : INoteRevisionRepository
{
    public async ValueTask AddAsync(NoteRevision revision, CancellationToken cancellationToken) => await db.Set<NoteRevision>().AddAsync(revision, cancellationToken);
    public ValueTask<NoteRevision?> GetByIdAsync(int noteId, int revisionId, CancellationToken cancellationToken) => new(db.Set<NoteRevision>().SingleOrDefaultAsync(x => x.NoteId == noteId && x.Id == revisionId, cancellationToken));
}

public sealed class KnowledgeNoteLinkRepository(AppDbContext db) : INoteLinkRepository
{
    public async Task ReplaceForSourceNoteAsync(Note sourceNote, IReadOnlyList<NoteLink> links, CancellationToken cancellationToken)
    {
        if (sourceNote.Id != 0) await db.Set<NoteLink>().Where(x => x.SourceNoteId == sourceNote.Id).ExecuteDeleteAsync(cancellationToken);
        await db.Set<NoteLink>().AddRangeAsync(links, cancellationToken);
    }

    public async Task ResolveTargetAsync(Note targetNote, CancellationToken cancellationToken)
    {
        var unresolved = await db.Set<NoteLink>()
            .Where(x => x.WorkspaceId == targetNote.WorkspaceId && x.TargetNoteId == null && x.TargetNormalizedSlug == targetNote.NormalizedSlug)
            .ToListAsync(cancellationToken);
        foreach (var link in unresolved) link.TargetNote = targetNote;
    }
}

public sealed class KnowledgeTagRepository(AppDbContext db) : IKnowledgeTagRepository
{
    public Task<bool> ExistsBySlugAsync(int workspaceId, int? projectId, string slug, CancellationToken cancellationToken) => db.Set<KnowledgeTag>().AnyAsync(x => x.WorkspaceId == workspaceId && x.ProjectId == projectId && x.NormalizedSlug == slug, cancellationToken);
    public async ValueTask AddAsync(KnowledgeTag tag, CancellationToken cancellationToken) => await db.Set<KnowledgeTag>().AddAsync(tag, cancellationToken);
}

public sealed class KnowledgeTaskNoteLinkRepository(AppDbContext db) : ITaskNoteLinkRepository
{
    public Task<bool> ExistsAsync(int taskId, int noteId, CancellationToken cancellationToken) => db.Set<TaskNoteLink>().AnyAsync(x => x.TaskId == taskId && x.NoteId == noteId, cancellationToken);
    public async ValueTask AddAsync(TaskNoteLink link, CancellationToken cancellationToken) => await db.Set<TaskNoteLink>().AddAsync(link, cancellationToken);
    public Task RemoveAsync(int taskId, int noteId, CancellationToken cancellationToken) =>
        db.Set<TaskNoteLink>()
            .Where(x => x.TaskId == taskId && x.NoteId == noteId)
            .ExecuteDeleteAsync(cancellationToken);
}

public sealed class KnowledgeTaskNoteScopeRepository(AppDbContext db) : IKnowledgeTaskNoteScopePort
{
    public Task<TaskNoteScope?> GetActiveScopeAsync(
        int workspaceId,
        int taskId,
        int noteId,
        CancellationToken cancellationToken) =>
        (from task in db.Set<TaskItem>()
         join project in db.Set<Project>() on task.ProjectId equals project.Id
         join note in db.Set<Note>() on noteId equals note.Id
         where task.Id == taskId
               && note.Id == noteId
               && project.WorkspaceId == workspaceId
               && note.WorkspaceId == workspaceId
               && project.Status == ProjectStatus.Active
               && task.Status != TaskItemStatus.Cancelled
         select new TaskNoteScope(workspaceId, task.Id, note.Id, task.ProjectId, note.ProjectId))
        .SingleOrDefaultAsync(cancellationToken);
}

public sealed class KnowledgeReadRepository(AppDbContext db) : IKnowledgeReadPort
{
    public Task<IReadOnlyList<NoteLinkReadModel>> GetLinksAsync(int workspaceId, int noteId, CancellationToken ct) => db.Set<NoteLink>().Where(x => x.WorkspaceId == workspaceId && x.SourceNoteId == noteId).Select(x => new NoteLinkReadModel(x.SourceNoteId, x.TargetNoteId, x.TargetNormalizedSlug, x.Alias, x.TargetNoteId != null)).ToListAsync(ct).ContinueWith<IReadOnlyList<NoteLinkReadModel>>(t => t.Result, ct);
    public Task<IReadOnlyList<NoteLinkReadModel>> GetBacklinksAsync(int workspaceId, int noteId, CancellationToken ct) => db.Set<NoteLink>().Where(x => x.WorkspaceId == workspaceId && x.TargetNoteId == noteId).Select(x => new NoteLinkReadModel(x.SourceNoteId, x.TargetNoteId, x.TargetNormalizedSlug, x.Alias, true)).ToListAsync(ct).ContinueWith<IReadOnlyList<NoteLinkReadModel>>(t => t.Result, ct);
    public Task<IReadOnlyList<NoteRevisionReadModel>> GetRevisionsAsync(int workspaceId, int noteId, CancellationToken ct) => (from r in db.Set<NoteRevision>() join n in db.Set<Note>() on r.NoteId equals n.Id where n.WorkspaceId == workspaceId && r.NoteId == noteId orderby r.RevisionNumber descending select new NoteRevisionReadModel(r.Id, r.RevisionNumber, r.Title, r.NormalizedSlug, r.Content, r.CreatedAt, r.RestoredFromRevisionId)).ToListAsync(ct).ContinueWith<IReadOnlyList<NoteRevisionReadModel>>(t => t.Result, ct);
    public async Task<IReadOnlyList<KnowledgeSearchItem>> SearchAsync(int workspaceId, string query, int max, CancellationToken ct)
    {
        var tsQuery = EF.Functions.PlainToTsQuery("simple", query);
        return await db.Set<Note>()
            .Where(n => n.WorkspaceId == workspaceId && EF.Property<NpgsqlTsVector>(n, "SearchDocument").Matches(tsQuery))
            .OrderByDescending(n => EF.Property<NpgsqlTsVector>(n, "SearchDocument").Rank(tsQuery))
            .ThenByDescending(n => n.UpdatedAt)
            .Take(max)
            .Select(n => new KnowledgeSearchItem(n.Id, n.Title, n.NormalizedSlug, n.Content, n.UpdatedAt))
            .ToListAsync(ct);
    }
    public async Task<KnowledgeGraphReadModel> GetGraphAsync(int workspaceId, int? projectId, CancellationToken ct) { var nodes = await db.Set<Note>().Where(n => n.WorkspaceId == workspaceId && (!projectId.HasValue || n.ProjectId == projectId)).Select(n => new KnowledgeGraphNode(n.Id, n.Title, n.NormalizedSlug)).ToListAsync(ct); var ids = nodes.Select(n => n.NoteId).ToList(); var edges = await db.Set<NoteLink>().Where(l => l.WorkspaceId == workspaceId && ids.Contains(l.SourceNoteId)).Select(l => new KnowledgeGraphEdge(l.SourceNoteId, l.TargetNoteId, l.TargetNormalizedSlug)).ToListAsync(ct); return new KnowledgeGraphReadModel(nodes, edges); }
}
