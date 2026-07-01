using Microsoft.AspNetCore.Authorization;
using TaskPilot.Application.Authorization.Enums;
using TaskPilot.Domain.Entities;
using TaskPilot.Infrastructure.Authorization.Contexts;
using TaskPilot.Infrastructure.Authorization.Requirements;

namespace TaskPilot.Infrastructure.Authorization.Handlers;

public sealed class ProjectAccessHandler
    : AuthorizationHandler<ProjectAccessRequirement, ProjectAuthorizationContext>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ProjectAccessRequirement requirement,
        ProjectAuthorizationContext resource)
    {
        if (requirement.AccessLevel == ProjectAccessLevel.Read)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (resource.WorkspaceMember.Role == Role.Owner)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (requirement.AccessLevel == ProjectAccessLevel.Participant && resource.ProjectMember is not null)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (requirement.AccessLevel == ProjectAccessLevel.Manage &&
            resource.ProjectMember?.Role == ProjectRole.ProjectManager)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
