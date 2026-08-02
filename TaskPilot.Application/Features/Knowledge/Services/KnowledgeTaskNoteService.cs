using System.Net;
using TaskPilot.Application.Features.Knowledge.Authorization;
using TaskPilot.Application.Features.Knowledge.Dtos;
using TaskPilot.Application.Features.Knowledge.Repositories;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Domain.Knowledge;

namespace TaskPilot.Application.Features.Knowledge.Services;

public sealed class KnowledgeTaskNoteService(
    IKnowledgeTaskNoteScopePort taskNoteScopePort,
    ITaskNoteLinkRepository taskNoteLinkRepository,
    IKnowledgeAccessPort knowledgeAccessPort,
    IUnitOfWork unitOfWork) : IKnowledgeTaskNoteService
{
    public Task<ServiceResult> LinkAsync(int workspaceId, TaskNoteLinkRequest request, CancellationToken cancellationToken) => ExecuteAsync(workspaceId, request, link: true, cancellationToken);
    public Task<ServiceResult> UnlinkAsync(int workspaceId, TaskNoteLinkRequest request, CancellationToken cancellationToken) => ExecuteAsync(workspaceId, request, link: false, cancellationToken);

    private async Task<ServiceResult> ExecuteAsync(int workspaceId, TaskNoteLinkRequest request, bool link, CancellationToken cancellationToken)
    {
        var scope = await taskNoteScopePort.GetActiveScopeAsync(workspaceId, request.TaskId, request.NoteId, cancellationToken);
        if (scope is null || scope.WorkspaceId != workspaceId || scope.TaskProjectId != scope.NoteProjectId)
        {
            return ServiceResult.Fail("Task and note must be active and belong to the same workspace and project.", HttpStatusCode.BadRequest);
        }

        var access = await knowledgeAccessPort.AuthorizeAsync(
            workspaceId,
            scope.TaskProjectId,
            KnowledgeAccessLevel.Edit,
            cancellationToken);
        if (access.Failure is not null) return access.Failure;

        var exists = await taskNoteLinkRepository.ExistsAsync(request.TaskId, request.NoteId, cancellationToken);
        if (link)
        {
            if (exists) return ServiceResult.Success(HttpStatusCode.NoContent);
            await taskNoteLinkRepository.AddAsync(new TaskNoteLink { WorkspaceId = workspaceId, TaskId = request.TaskId, NoteId = request.NoteId }, cancellationToken);
        }
        else
        {
            if (!exists) return ServiceResult.Success(HttpStatusCode.NoContent);
            await taskNoteLinkRepository.RemoveAsync(request.TaskId, request.NoteId, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success(HttpStatusCode.NoContent);
    }
}
