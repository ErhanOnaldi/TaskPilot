using System.Net;
using AutoMapper;
using TaskPilot.Application.Authorization.Abstractions;
using TaskPilot.Application.Authorization.Enums;
using TaskPilot.Application.Features.ProjectMembers.Dtos;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Application.Interfaces.Persistence.Project;
using TaskPilot.Application.Interfaces.Persistence.Workspace;
using TaskPilot.Application.Interfaces.Persistence.Tasks;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.ProjectMembers.Services;

public class ProjectMemberService(
    IProjectMemberRepository projectMemberRepository,
    IWorkspaceMemberRepository workspaceMemberRepository,
    ITaskRepository taskRepository,
    IUnitOfWork unitOfWork,
    IAccessControlService accessControlService,
    IMapper mapper,
    IDateTimeProvider dateTimeProvider) : IProjectMemberService
{
    public async Task<ServiceResult<List<ProjectMemberResponse>>> GetMembersAsync(int projectId, CancellationToken cancellationToken)
    {
        var access = await accessControlService.AuthorizeProjectAsync(
            projectId,
            ProjectAccessLevel.Read,
            requireActiveProject: false,
            cancellationToken);
        if (access.Failure is not null)
        {
            return ServiceResult<List<ProjectMemberResponse>>.Fail(access.Failure.ErrorMessages!, access.Failure.Status);
        }

        var members = await projectMemberRepository.GetMembersByProjectIdAsync(projectId, cancellationToken);
        return ServiceResult<List<ProjectMemberResponse>>.Success(
            mapper.Map<List<ProjectMemberResponse>>(members));
    }

    public async Task<ServiceResult<ProjectMemberResponse>> AddMemberAsync(int projectId, AddProjectMemberRequest request, CancellationToken cancellationToken)
    {
        var access = await accessControlService.AuthorizeProjectAsync(
            projectId,
            ProjectAccessLevel.Manage,
            requireActiveProject: true,
            cancellationToken);
        if (access.Failure is not null)
        {
            return access.Failure.Status == HttpStatusCode.Forbidden
                ? ServiceResult<ProjectMemberResponse>.Fail("Only workspace owner or project manager can add project members.", HttpStatusCode.Forbidden)
                : ServiceResult<ProjectMemberResponse>.Fail(access.Failure.ErrorMessages!, access.Failure.Status);
        }

        var workspaceMember = await workspaceMemberRepository.GetMemberAsync(
            access.Project.WorkspaceId,
            request.UserId,
            cancellationToken);
        if (workspaceMember is null)
        {
            return ServiceResult<ProjectMemberResponse>.Fail("User must be a workspace member before joining project.", HttpStatusCode.BadRequest);
        }

        if (workspaceMember.Role == Role.Guest && request.Role == ProjectRole.ProjectManager)
        {
            return ServiceResult<ProjectMemberResponse>.Fail(
                "Workspace guests cannot be project managers.",
                HttpStatusCode.Forbidden);
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
        return ServiceResult<ProjectMemberResponse>.Success(
            mapper.Map<ProjectMemberResponse>(saved ?? member),
            HttpStatusCode.Created);
    }

    public async Task<ServiceResult> UpdateMemberRoleAsync(int projectId, int userId, UpdateProjectMemberRoleRequest request, CancellationToken cancellationToken)
    {
        var access = await accessControlService.AuthorizeProjectAsync(
            projectId,
            ProjectAccessLevel.Manage,
            requireActiveProject: true,
            cancellationToken);
        if (access.Failure is not null)
        {
            return access.Failure.Status == HttpStatusCode.Forbidden
                ? ServiceResult.Fail("Only workspace owner or project manager can update project member roles.", HttpStatusCode.Forbidden)
                : access.Failure;
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

        if (request.Role == ProjectRole.Guest && member.Role != ProjectRole.Guest)
        {
            await taskRepository.UnassignUserFromProjectAsync(
                projectId,
                userId,
                dateTimeProvider.UtcNow,
                cancellationToken);
        }

        member.Role = request.Role;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    public async Task<ServiceResult> RemoveMemberAsync(int projectId, int userId, CancellationToken cancellationToken)
    {
        var access = await accessControlService.AuthorizeProjectAsync(
            projectId,
            ProjectAccessLevel.Manage,
            requireActiveProject: true,
            cancellationToken);
        if (access.Failure is not null)
        {
            return access.Failure.Status == HttpStatusCode.Forbidden
                ? ServiceResult.Fail("Only workspace owner or project manager can remove project members.", HttpStatusCode.Forbidden)
                : access.Failure;
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

        await taskRepository.UnassignUserFromProjectAsync(
            projectId,
            userId,
            dateTimeProvider.UtcNow,
            cancellationToken);

        projectMemberRepository.Delete(member);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success(HttpStatusCode.NoContent);
    }
}
