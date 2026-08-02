using TaskPilot.Application.Features.Knowledge.Dtos;

namespace TaskPilot.Application.Features.Knowledge.Services;

public interface IKnowledgeTaskNoteService
{
    Task<ServiceResult> LinkAsync(int workspaceId, TaskNoteLinkRequest request, CancellationToken cancellationToken);
    Task<ServiceResult> UnlinkAsync(int workspaceId, TaskNoteLinkRequest request, CancellationToken cancellationToken);
}
