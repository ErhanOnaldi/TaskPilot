using System.Net;
using TaskPilot.Application.Features.Knowledge.Authorization;
using TaskPilot.Application.Features.Knowledge.Dtos;
using TaskPilot.Application.Features.Knowledge.Repositories;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Domain.Knowledge;
using TaskPilot.Application.Interfaces.Infrastructure.Messaging;
using TaskPilot.Application.Features.Semantic.Contracts;
using TaskPilot.Domain.AI.Semantic;

namespace TaskPilot.Application.Features.Knowledge.Services;

public sealed class KnowledgeNoteService(
    INoteRepository noteRepository,
    INoteRevisionRepository noteRevisionRepository,
    INoteLinkRepository noteLinkRepository,
    IKnowledgeScopeValidationPort scopeValidationPort,
    IKnowledgeAccessPort knowledgeAccessPort,
    IUnitOfWork unitOfWork,
    IEventOutbox eventOutbox,
    IDateTimeProvider dateTimeProvider) : IKnowledgeNoteService
{
    public async Task<ServiceResult<NoteResponse>> GetAsync(int noteId, CancellationToken cancellationToken)
    {
        var note = await noteRepository.GetByIdAsync(noteId, cancellationToken);
        if (note is null) return ServiceResult<NoteResponse>.Fail("Note not found.", HttpStatusCode.NotFound);
        var access = await knowledgeAccessPort.AuthorizeAsync(note.WorkspaceId, note.ProjectId, KnowledgeAccessLevel.Read, cancellationToken);
        return access.Failure is null ? ServiceResult<NoteResponse>.Success(ToResponse(note)) : ServiceResult<NoteResponse>.Fail(access.Failure.ErrorMessages!, access.Failure.Status);
    }

    public async Task<ServiceResult<NoteResponse>> CreateAsync(int workspaceId, CreateNoteRequest request, CancellationToken cancellationToken)
    {
        var access = await knowledgeAccessPort.AuthorizeAsync(workspaceId, request.ProjectId, KnowledgeAccessLevel.Edit, cancellationToken);
        if (access.Failure is not null) return ServiceResult<NoteResponse>.Fail(access.Failure.ErrorMessages!, access.Failure.Status);
        if (!await IsValidScopeAsync(workspaceId, request.ProjectId, request.FolderId, cancellationToken)) return ServiceResult<NoteResponse>.Fail("Project and folder must belong to the workspace.", HttpStatusCode.BadRequest);
        var slug = KnowledgeSlugPolicy.Normalize(request.Slug);
        if (await noteRepository.ExistsBySlugAsync(workspaceId, 0, slug, cancellationToken)) return ServiceResult<NoteResponse>.Fail("A note with this slug already exists.", HttpStatusCode.Conflict);

        var now = dateTimeProvider.UtcNow;
        var note = new Note { WorkspaceId = workspaceId, ProjectId = request.ProjectId, FolderId = request.FolderId, CreatedByUserId = access.CurrentUserId, Title = request.Title.Trim(), NormalizedSlug = slug, Content = request.Content?.Trim() ?? string.Empty, Version = 1, CreatedAt = now, UpdatedAt = now };
        await noteRepository.AddAsync(note, cancellationToken);
        await noteRevisionRepository.AddAsync(CreateRevision(note, access.CurrentUserId, now, null), cancellationToken);
        await ReplaceLinksAsync(note, cancellationToken);
        await noteLinkRepository.ResolveTargetAsync(note, cancellationToken);
        await EnqueueSemanticChangeAsync(note, false, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult<NoteResponse>.Success(ToResponse(note), HttpStatusCode.Created);
    }

    public async Task<ServiceResult<NoteResponse>> UpdateAsync(int noteId, UpdateNoteRequest request, CancellationToken cancellationToken)
    {
        var note = await noteRepository.GetByIdAsync(noteId, cancellationToken);
        if (note is null) return ServiceResult<NoteResponse>.Fail("Note not found.", HttpStatusCode.NotFound);
        var access = await knowledgeAccessPort.AuthorizeAsync(note.WorkspaceId, note.ProjectId, KnowledgeAccessLevel.Edit, cancellationToken);
        if (access.Failure is not null) return ServiceResult<NoteResponse>.Fail(access.Failure.ErrorMessages!, access.Failure.Status);
        var targetAccess = await knowledgeAccessPort.AuthorizeAsync(note.WorkspaceId, request.ProjectId, KnowledgeAccessLevel.Edit, cancellationToken);
        if (targetAccess.Failure is not null) return ServiceResult<NoteResponse>.Fail(targetAccess.Failure.ErrorMessages!, targetAccess.Failure.Status);
        if (note.Version != request.ExpectedVersion) return ServiceResult<NoteResponse>.Fail("Note was changed by another user.", HttpStatusCode.Conflict);
        if (!await IsValidScopeAsync(note.WorkspaceId, request.ProjectId, request.FolderId, cancellationToken)) return ServiceResult<NoteResponse>.Fail("Project and folder must belong to the workspace.", HttpStatusCode.BadRequest);
        var slug = KnowledgeSlugPolicy.Normalize(request.Slug);
        if (await noteRepository.ExistsBySlugAsync(note.WorkspaceId, note.Id, slug, cancellationToken)) return ServiceResult<NoteResponse>.Fail("A note with this slug already exists.", HttpStatusCode.Conflict);

        note.Title = request.Title.Trim(); note.NormalizedSlug = slug; note.Content = request.Content?.Trim() ?? string.Empty; note.ProjectId = request.ProjectId; note.FolderId = request.FolderId; note.Version++; note.UpdatedAt = dateTimeProvider.UtcNow;
        await noteRevisionRepository.AddAsync(CreateRevision(note, access.CurrentUserId, note.UpdatedAt, null), cancellationToken);
        await ReplaceLinksAsync(note, cancellationToken);
        await noteLinkRepository.ResolveTargetAsync(note, cancellationToken);
        await EnqueueSemanticChangeAsync(note, false, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult<NoteResponse>.Success(ToResponse(note));
    }

    public async Task<ServiceResult<NoteResponse>> RestoreRevisionAsync(int noteId, int revisionId, int expectedVersion, CancellationToken cancellationToken)
    {
        var note = await noteRepository.GetByIdAsync(noteId, cancellationToken);
        if (note is null) return ServiceResult<NoteResponse>.Fail("Note not found.", HttpStatusCode.NotFound);
        var access = await knowledgeAccessPort.AuthorizeAsync(note.WorkspaceId, note.ProjectId, KnowledgeAccessLevel.Edit, cancellationToken);
        if (access.Failure is not null) return ServiceResult<NoteResponse>.Fail(access.Failure.ErrorMessages!, access.Failure.Status);
        if (note.Version != expectedVersion) return ServiceResult<NoteResponse>.Fail("Note was changed by another user.", HttpStatusCode.Conflict);
        var revision = await noteRevisionRepository.GetByIdAsync(noteId, revisionId, cancellationToken);
        if (revision is null) return ServiceResult<NoteResponse>.Fail("Note revision not found.", HttpStatusCode.NotFound);

        note.Title = revision.Title; note.NormalizedSlug = revision.NormalizedSlug; note.Content = revision.Content; note.Version++; note.UpdatedAt = dateTimeProvider.UtcNow;
        await noteRevisionRepository.AddAsync(CreateRevision(note, access.CurrentUserId, note.UpdatedAt, revision.Id), cancellationToken);
        await ReplaceLinksAsync(note, cancellationToken);
        await EnqueueSemanticChangeAsync(note, false, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult<NoteResponse>.Success(ToResponse(note));
    }

    public async Task<ServiceResult> DeleteAsync(int noteId, CancellationToken cancellationToken)
    {
        var note = await noteRepository.GetByIdAsync(noteId, cancellationToken);
        if (note is null) return ServiceResult.Fail("Note not found.", HttpStatusCode.NotFound);
        var access = await knowledgeAccessPort.AuthorizeAsync(note.WorkspaceId, note.ProjectId, KnowledgeAccessLevel.Edit, cancellationToken);
        if (access.Failure is not null) return access.Failure;
        await noteRepository.DeleteAsync(note, cancellationToken);
        await EnqueueSemanticChangeAsync(note, true, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    private async Task ReplaceLinksAsync(Note note, CancellationToken cancellationToken)
    {
        var links = new List<NoteLink>();
        foreach (var reference in WikiLinkParser.Parse(note.Content))
        {
            var target = await noteRepository.GetBySlugAsync(note.WorkspaceId, reference.Slug, cancellationToken);
            links.Add(new NoteLink { WorkspaceId = note.WorkspaceId, SourceNoteId = note.Id, SourceNote = note, TargetNoteId = target?.Id, TargetNormalizedSlug = reference.Slug, Alias = reference.Alias });
        }
        await noteLinkRepository.ReplaceForSourceNoteAsync(note, links, cancellationToken);
    }

    private async Task<bool> IsValidScopeAsync(int workspaceId, int? projectId, int? folderId, CancellationToken cancellationToken)
    {
        return await scopeValidationPort.IsProjectInWorkspaceAsync(workspaceId, projectId, cancellationToken) &&
               await scopeValidationPort.IsFolderInWorkspaceAsync(workspaceId, projectId, folderId, cancellationToken);
    }

    private Task EnqueueSemanticChangeAsync(Note note, bool isDeleted, CancellationToken cancellationToken) =>
        eventOutbox.EnqueueAsync(
            () => new SemanticContentChangedEvent(
                Guid.NewGuid(),
                SemanticSourceType.Note,
                note.Id,
                note.WorkspaceId,
                note.ProjectId,
                $"{note.Title}\n{note.Content}",
                dateTimeProvider.UtcNow,
                isDeleted),
            cancellationToken);

    private static NoteRevision CreateRevision(Note note, int userId, DateTime now, int? restoredFromRevisionId) => new() { NoteId = note.Id, Note = note, RevisionNumber = note.Version, CreatedByUserId = userId, Title = note.Title, NormalizedSlug = note.NormalizedSlug, Content = note.Content, RestoredFromRevisionId = restoredFromRevisionId, CreatedAt = now, UpdatedAt = now };
    private static NoteResponse ToResponse(Note note) => new(note.Id, note.WorkspaceId, note.ProjectId, note.FolderId, note.Title, note.NormalizedSlug, note.Content, note.Version, note.UpdatedAt);
}
