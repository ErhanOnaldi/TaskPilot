using TaskPilot.Application.Features.Knowledge.Dtos;

namespace TaskPilot.Application.Features.Knowledge.Services;

public interface IKnowledgeAdministrationService
{
    Task<ServiceResult> CreateFolderAsync(int workspaceId, CreateKnowledgeFolderRequest request, CancellationToken cancellationToken);
    Task<ServiceResult> CreateTagAsync(int workspaceId, CreateKnowledgeTagRequest request, CancellationToken cancellationToken);
}
