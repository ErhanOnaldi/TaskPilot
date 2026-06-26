using TaskPilot.Application.Features.ProjectMembers.Dtos;

namespace TaskPilot.Application.Features.ProjectMembers.Services;

public interface IProjectMemberService
{
    Task<ServiceResult<List<ProjectMemberResponse>>> GetMembersAsync(int projectId, CancellationToken cancellationToken);
    Task<ServiceResult<ProjectMemberResponse>> AddMemberAsync(int projectId, AddProjectMemberRequest request, CancellationToken cancellationToken);
    Task<ServiceResult> UpdateMemberRoleAsync(int projectId, int userId, UpdateProjectMemberRoleRequest request, CancellationToken cancellationToken);
    Task<ServiceResult> RemoveMemberAsync(int projectId, int userId, CancellationToken cancellationToken);
}
