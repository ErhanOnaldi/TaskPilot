using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Security.Claims;
using TaskPilot.Application;
using TaskPilot.Application.Authorization.Abstractions;
using TaskPilot.Application.Authorization.Enums;
using TaskPilot.Application.Authorization.Results;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Persistence.Project;
using TaskPilot.Application.Interfaces.Persistence.Workspace;
using TaskPilot.Domain.Entities;
using TaskPilot.Infrastructure.Authorization.Contexts;
using TaskPilot.Infrastructure.Authorization.Requirements;

namespace TaskPilot.Infrastructure.Authorization.Services;

public sealed class AccessControlService(
    IAuthorizationService authorizationService,
    IHttpContextAccessor httpContextAccessor,
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

        var authorizationResult = await authorizationService.AuthorizeAsync(
            GetUser(),
            new WorkspaceAuthorizationContext(workspace, workspaceMember, currentUserId),
            new WorkspaceAccessRequirement(accessLevel));

        if (!authorizationResult.Succeeded)
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

        var projectMember = await projectMemberRepository.GetMemberAsync(projectId, currentUserId, cancellationToken);
        var authorizationResult = await authorizationService.AuthorizeAsync(
            GetUser(),
            new ProjectAuthorizationContext(project, workspace, workspaceMember, projectMember, currentUserId),
            new ProjectAccessRequirement(accessLevel));

        if (!authorizationResult.Succeeded)
        {
            return ProjectAccessResult.Fail(
                ServiceResult.Fail(GetProjectForbiddenMessage(accessLevel), HttpStatusCode.Forbidden),
                currentUserId);
        }

        return new ProjectAccessResult(project, workspace, workspaceMember, currentUserId, null);
    }

    private static string GetProjectForbiddenMessage(ProjectAccessLevel accessLevel)
    {
        return accessLevel == ProjectAccessLevel.Participant
            ? "Only project members can perform this action."
            : "Only workspace owner or project manager can perform this action.";
    }

    private ClaimsPrincipal GetUser()
    {
        return httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());
    }
}
