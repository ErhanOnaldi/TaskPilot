using TaskPilot.Application.Features.WorkspaceMembers.Dtos;

namespace TaskPilot.Application.Features.WorkspaceMembers.Services;

public interface IWorkspaceMemberService
{
    Task<ServiceResult<List<WorkspaceMemberResponse>>> GetMembersAsync(
        int workspaceId,
        CancellationToken cancellationToken);

    Task<ServiceResult<WorkspaceMemberResponse>> AddMemberAsync(
        int workspaceId,
        AddWorkspaceMemberRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult> UpdateMemberRoleAsync(
        int workspaceId,
        int userId,
        UpdateWorkspaceMemberRoleRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult> RemoveMemberAsync(
        int workspaceId,
        int userId,
        CancellationToken cancellationToken);
}