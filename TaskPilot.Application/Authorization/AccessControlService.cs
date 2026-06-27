using System.Net;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Persistence.Project;
using TaskPilot.Application.Interfaces.Persistence.Workspace;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Authorization;

public sealed class AccessControlService(
    ICurrentUserService currentUserService,
    IWorkspaceRepository workspaceRepository,
    IWorkspaceMemberRepository workspaceMemberRepository,
    IProjectRepository projectRepository,
    IProjectMemberRepository projectMemberRepository) : IAccessControlService
{
    public async Task<WorkspaceAccessResult> AuthorizeWorkspaceAsync(
        int workspaceId,
        WorkspaceAccessLevel accessLevel,
        bool requireActiveWorkspace,
        CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.GetRequiredUserId();
        var workspace = await workspaceRepository.GetByIdAsync(workspaceId);
        if (workspace is null)
        {
            return WorkspaceAccessResult.Fail(
                ServiceResult.Fail("Workspace not found.", HttpStatusCode.NotFound),
                currentUserId);
        }

        if (requireActiveWorkspace && workspace.IsArchived)
        {
            return WorkspaceAccessResult.Fail(
                ServiceResult.Fail("Workspace is archived.", HttpStatusCode.BadRequest),
                currentUserId);
        }

        var workspaceMember = await workspaceMemberRepository.GetMemberAsync(workspaceId, currentUserId, cancellationToken);
        if (workspaceMember is null)
        {
            return WorkspaceAccessResult.Fail(
                ServiceResult.Fail("Workspace not found.", HttpStatusCode.NotFound),
                currentUserId);
        }

        if (accessLevel == WorkspaceAccessLevel.Owner && workspaceMember.Role != Role.Owner)
        {
            return WorkspaceAccessResult.Fail(
                ServiceResult.Fail("Only workspace owner can perform this action.", HttpStatusCode.Forbidden),
                currentUserId);
        }

        return new WorkspaceAccessResult(workspace, workspaceMember, currentUserId, null);
    }

    public async Task<ProjectAccessResult> AuthorizeProjectAsync(
        int projectId,
        ProjectAccessLevel accessLevel,
        bool requireActiveProject,
        CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.GetRequiredUserId();
        var project = await projectRepository.GetProjectByIdAsync(projectId, cancellationToken);
        if (project is null)
        {
            return ProjectAccessResult.Fail(
                ServiceResult.Fail("Project not found.", HttpStatusCode.NotFound),
                currentUserId);
        }

        if (requireActiveProject && project.Status == ProjectStatus.Archived)
        {
            return ProjectAccessResult.Fail(
                ServiceResult.Fail("Project is archived.", HttpStatusCode.BadRequest),
                currentUserId);
        }

        var workspace = await workspaceRepository.GetByIdAsync(project.WorkspaceId);
        if (workspace is null)
        {
            return ProjectAccessResult.Fail(
                ServiceResult.Fail("Workspace not found.", HttpStatusCode.NotFound),
                currentUserId);
        }

        if (workspace.IsArchived)
        {
            return ProjectAccessResult.Fail(
                ServiceResult.Fail("Workspace is archived.", HttpStatusCode.BadRequest),
                currentUserId);
        }

        var workspaceMember = await workspaceMemberRepository.GetMemberAsync(project.WorkspaceId, currentUserId, cancellationToken);
        if (workspaceMember is null)
        {
            return ProjectAccessResult.Fail(
                ServiceResult.Fail("Project not found.", HttpStatusCode.NotFound),
                currentUserId);
        }

        if (accessLevel == ProjectAccessLevel.Read)
        {
            return new ProjectAccessResult(project, workspace, workspaceMember, currentUserId, null);
        }

        if (workspaceMember.Role == Role.Owner)
        {
            return new ProjectAccessResult(project, workspace, workspaceMember, currentUserId, null);
        }

        if (accessLevel == ProjectAccessLevel.Participant)
        {
            var isProjectMember = await projectMemberRepository.IsProjectMemberAsync(projectId, currentUserId, cancellationToken);
            if (!isProjectMember)
            {
                return ProjectAccessResult.Fail(
                    ServiceResult.Fail("Only project members can perform this action.", HttpStatusCode.Forbidden),
                    currentUserId);
            }

            return new ProjectAccessResult(project, workspace, workspaceMember, currentUserId, null);
        }

        var isProjectManager = await projectMemberRepository.IsProjectManagerAsync(projectId, currentUserId, cancellationToken);
        if (!isProjectManager)
        {
            return ProjectAccessResult.Fail(
                ServiceResult.Fail("Only workspace owner or project manager can perform this action.", HttpStatusCode.Forbidden),
                currentUserId);
        }

        return new ProjectAccessResult(project, workspace, workspaceMember, currentUserId, null);
    }
}
