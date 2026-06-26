using System.Net;
using FluentValidation;
using TaskPilot.Application.Features.ProjectMembers.Dtos;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Application.Interfaces.Persistence.Project;
using TaskPilot.Application.Interfaces.Persistence.Workspace;
using TaskPilot.Domain.Entities;
using ProjectEntity = TaskPilot.Domain.Entities.Project;

namespace TaskPilot.Application.Features.ProjectMembers.Services;

public class ProjectMemberService(
    IProjectRepository projectRepository,
    IProjectMemberRepository projectMemberRepository,
    IWorkspaceRepository workspaceRepository,
    IWorkspaceMemberRepository workspaceMemberRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IValidator<AddProjectMemberRequest> addValidator,
    IValidator<UpdateProjectMemberRoleRequest> updateValidator) : IProjectMemberService
{
    public async Task<ServiceResult<List<ProjectMemberResponse>>> GetMembersAsync(int projectId, CancellationToken cancellationToken)
    {
        var access = await LoadProjectAccessAsync(projectId, cancellationToken);
        if (access.Result is not null) return ServiceResult<List<ProjectMemberResponse>>.Fail(access.Result.ErrorMessages!, access.Result.Status);

        var members = await projectMemberRepository.GetMembersByProjectIdAsync(projectId, cancellationToken);
        return ServiceResult<List<ProjectMemberResponse>>.Success(members.Select(Map).ToList());
    }

    public async Task<ServiceResult<ProjectMemberResponse>> AddMemberAsync(int projectId, AddProjectMemberRequest request, CancellationToken cancellationToken)
    {
        var validation = await addValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ServiceResult<ProjectMemberResponse>.Fail(validation.Errors.Select(x => x.ErrorMessage).ToList(), HttpStatusCode.BadRequest);
        }

        var access = await LoadProjectAccessAsync(projectId, cancellationToken);
        if (access.Result is not null) return ServiceResult<ProjectMemberResponse>.Fail(access.Result.ErrorMessages!, access.Result.Status);
        if (!await CanManageProjectAsync(projectId, access.WorkspaceMember!, cancellationToken))
        {
            return ServiceResult<ProjectMemberResponse>.Fail("Only workspace owner or project manager can add project members.", HttpStatusCode.Forbidden);
        }

        if (await workspaceMemberRepository.GetMemberAsync(access.Project!.WorkspaceId, request.UserId, cancellationToken) is null)
        {
            return ServiceResult<ProjectMemberResponse>.Fail("User must be a workspace member before joining project.", HttpStatusCode.BadRequest);
        }

        if (await projectMemberRepository.IsProjectMemberAsync(projectId, request.UserId, cancellationToken))
        {
            return ServiceResult<ProjectMemberResponse>.Fail("User is already a project member.", HttpStatusCode.Conflict);
        }

        var member = new ProjectMember
        {
            ProjectId = projectId,
            UserId = request.UserId,
            Role = request.Role,
            JoinedAt = DateTime.UtcNow
        };

        await projectMemberRepository.AddAsync(member);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var saved = await projectMemberRepository.GetMemberAsync(projectId, request.UserId, cancellationToken);
        return ServiceResult<ProjectMemberResponse>.Success(Map(saved ?? member), HttpStatusCode.Created);
    }

    public async Task<ServiceResult> UpdateMemberRoleAsync(int projectId, int userId, UpdateProjectMemberRoleRequest request, CancellationToken cancellationToken)
    {
        var validation = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ServiceResult.Fail(validation.Errors.Select(x => x.ErrorMessage).ToList(), HttpStatusCode.BadRequest);
        }

        var access = await LoadProjectAccessAsync(projectId, cancellationToken);
        if (access.Result is not null) return access.Result;
        if (!await CanManageProjectAsync(projectId, access.WorkspaceMember!, cancellationToken))
        {
            return ServiceResult.Fail("Only workspace owner or project manager can update project member roles.", HttpStatusCode.Forbidden);
        }

        var member = await projectMemberRepository.GetMemberAsync(projectId, userId, cancellationToken);
        if (member is null) return ServiceResult.Fail("Project member not found.", HttpStatusCode.NotFound);

        if (member.Role == ProjectRole.ProjectManager && request.Role != ProjectRole.ProjectManager)
        {
            var managerCount = await projectMemberRepository.CountProjectManagersAsync(projectId, cancellationToken);
            if (managerCount <= 1)
            {
                return ServiceResult.Fail("Project must have at least one project manager.", HttpStatusCode.BadRequest);
            }
        }

        member.Role = request.Role;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    public async Task<ServiceResult> RemoveMemberAsync(int projectId, int userId, CancellationToken cancellationToken)
    {
        var access = await LoadProjectAccessAsync(projectId, cancellationToken);
        if (access.Result is not null) return access.Result;
        if (!await CanManageProjectAsync(projectId, access.WorkspaceMember!, cancellationToken))
        {
            return ServiceResult.Fail("Only workspace owner or project manager can remove project members.", HttpStatusCode.Forbidden);
        }

        var member = await projectMemberRepository.GetMemberAsync(projectId, userId, cancellationToken);
        if (member is null) return ServiceResult.Fail("Project member not found.", HttpStatusCode.NotFound);

        if (member.Role == ProjectRole.ProjectManager)
        {
            var managerCount = await projectMemberRepository.CountProjectManagersAsync(projectId, cancellationToken);
            if (managerCount <= 1)
            {
                return ServiceResult.Fail("Project must have at least one project manager.", HttpStatusCode.BadRequest);
            }
        }

        projectMemberRepository.Delete(member);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    private async Task<ProjectAccess> LoadProjectAccessAsync(int projectId, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.GetRequiredUserId();
        var project = await projectRepository.GetProjectByIdAsync(projectId, cancellationToken);
        if (project is null) return ProjectAccess.Fail(ServiceResult.Fail("Project not found.", HttpStatusCode.NotFound));
        if (project.Status == ProjectStatus.Archived) return ProjectAccess.Fail(ServiceResult.Fail("Project is archived.", HttpStatusCode.BadRequest));

        var workspace = await workspaceRepository.GetByIdAsync(project.WorkspaceId);
        if (workspace is null) return ProjectAccess.Fail(ServiceResult.Fail("Workspace not found.", HttpStatusCode.NotFound));
        if (workspace.IsArchived) return ProjectAccess.Fail(ServiceResult.Fail("Workspace is archived.", HttpStatusCode.BadRequest));

        var workspaceMember = await workspaceMemberRepository.GetMemberAsync(project.WorkspaceId, currentUserId, cancellationToken);
        if (workspaceMember is null) return ProjectAccess.Fail(ServiceResult.Fail("Project not found.", HttpStatusCode.NotFound));

        return new ProjectAccess(project, workspaceMember, null);
    }

    private async Task<bool> CanManageProjectAsync(int projectId, WorkspaceMember workspaceMember, CancellationToken cancellationToken)
    {
        return workspaceMember.Role == Role.Owner ||
               await projectMemberRepository.IsProjectManagerAsync(projectId, currentUserService.GetRequiredUserId(), cancellationToken);
    }

    private static ProjectMemberResponse Map(ProjectMember member)
    {
        return new ProjectMemberResponse(
            member.UserId,
            member.User?.Email ?? string.Empty,
            member.Role,
            member.JoinedAt);
    }

    private sealed record ProjectAccess(ProjectEntity Project, WorkspaceMember WorkspaceMember, ServiceResult? Result)
    {
        public static ProjectAccess Fail(ServiceResult result)
        {
            return new ProjectAccess(null!, null!, result);
        }
    }
}
