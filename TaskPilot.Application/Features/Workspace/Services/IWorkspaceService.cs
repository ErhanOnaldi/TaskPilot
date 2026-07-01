using TaskPilot.Application.Common.Pagination;
using TaskPilot.Application.Features.Workspace.Dtos;

namespace TaskPilot.Application.Features.Workspace.Services;

public interface IWorkspaceService
{
    Task<ServiceResult<WorkspaceResponse>> CreateWorkspaceAsync(CreateWorkspaceRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<PagedResponse<WorkspaceListItemResponse>>> GetWorkSpacesAsync(
        WorkspaceQueryParameters query,
        CancellationToken cancellationToken);
    Task<ServiceResult<WorkspaceResponse>> GetWorkspaceAsync(int id, CancellationToken cancellationToken);
    Task<ServiceResult> UpdateWorkspaceAsync(int id, UpdateWorkspaceRequest request, CancellationToken cancellationToken);
    Task<ServiceResult> ArchiveWorkspaceAsync(int id, CancellationToken cancellationToken);
}
