using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TaskPilot.Application.Authorization.Enums;
using TaskPilot.Domain.Entities;
using TaskPilot.Infrastructure.Authorization.Contexts;
using TaskPilot.Infrastructure.Authorization.Handlers;
using TaskPilot.Infrastructure.Authorization.Requirements;
using ProjectEntity = TaskPilot.Domain.Entities.Project;

namespace TaskPilot.Application.Tests;

public sealed class AuthorizationHandlerTests
{
    [Fact]
    public async Task WorkspaceAccessHandler_succeeds_for_owner_access_when_workspace_member_is_owner()
    {
        var requirement = new WorkspaceAccessRequirement(WorkspaceAccessLevel.Owner);
        var resource = new WorkspaceAuthorizationContext(
            new WorkSpace { Id = 10, Name = "Engineering" },
            new WorkspaceMember { WorkspaceId = 10, UserId = 1, Role = Role.Owner },
            CurrentUserId: 1);
        var context = CreateContext(requirement, resource);

        await new WorkspaceAccessHandler().HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task WorkspaceAccessHandler_fails_for_owner_access_when_workspace_member_is_not_owner()
    {
        var requirement = new WorkspaceAccessRequirement(WorkspaceAccessLevel.Owner);
        var resource = new WorkspaceAuthorizationContext(
            new WorkSpace { Id = 10, Name = "Engineering" },
            new WorkspaceMember { WorkspaceId = 10, UserId = 1, Role = Role.Member },
            CurrentUserId: 1);
        var context = CreateContext(requirement, resource);

        await new WorkspaceAccessHandler().HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task WorkspaceAccessHandler_succeeds_for_member_access_when_workspace_member_exists()
    {
        var requirement = new WorkspaceAccessRequirement(WorkspaceAccessLevel.Member);
        var resource = new WorkspaceAuthorizationContext(
            new WorkSpace { Id = 10, Name = "Engineering" },
            new WorkspaceMember { WorkspaceId = 10, UserId = 1, Role = Role.Guest },
            CurrentUserId: 1);
        var context = CreateContext(requirement, resource);

        await new WorkspaceAccessHandler().HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task ProjectAccessHandler_succeeds_for_manage_access_when_user_is_project_manager()
    {
        var requirement = new ProjectAccessRequirement(ProjectAccessLevel.Manage);
        var resource = CreateProjectContext(
            new WorkspaceMember { WorkspaceId = 10, UserId = 2, Role = Role.Member },
            new ProjectMember { ProjectId = 20, UserId = 2, Role = ProjectRole.ProjectManager });
        var context = CreateContext(requirement, resource);

        await new ProjectAccessHandler().HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task ProjectAccessHandler_fails_for_manage_access_when_user_is_team_member()
    {
        var requirement = new ProjectAccessRequirement(ProjectAccessLevel.Manage);
        var resource = CreateProjectContext(
            new WorkspaceMember { WorkspaceId = 10, UserId = 2, Role = Role.Member },
            new ProjectMember { ProjectId = 20, UserId = 2, Role = ProjectRole.TeamMember });
        var context = CreateContext(requirement, resource);

        await new ProjectAccessHandler().HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task ProjectAccessHandler_succeeds_for_manage_access_when_user_is_workspace_owner_without_project_membership()
    {
        var requirement = new ProjectAccessRequirement(ProjectAccessLevel.Manage);
        var resource = CreateProjectContext(
            new WorkspaceMember { WorkspaceId = 10, UserId = 1, Role = Role.Owner },
            projectMember: null);
        var context = CreateContext(requirement, resource);

        await new ProjectAccessHandler().HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    private static ProjectAuthorizationContext CreateProjectContext(
        WorkspaceMember workspaceMember,
        ProjectMember? projectMember)
    {
        return new ProjectAuthorizationContext(
            new ProjectEntity { Id = 20, WorkspaceId = 10, Name = "API", Status = ProjectStatus.Active },
            new WorkSpace { Id = 10, Name = "Engineering" },
            workspaceMember,
            projectMember,
            CurrentUserId: workspaceMember.UserId);
    }

    private static AuthorizationHandlerContext CreateContext<TRequirement, TResource>(
        TRequirement requirement,
        TResource resource)
        where TRequirement : IAuthorizationRequirement
    {
        return new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "1")], "Test")),
            resource);
    }
}
