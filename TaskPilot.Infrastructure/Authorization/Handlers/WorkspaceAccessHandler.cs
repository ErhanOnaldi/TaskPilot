using Microsoft.AspNetCore.Authorization;
using TaskPilot.Application.Authorization.Enums;
using TaskPilot.Domain.Entities;
using TaskPilot.Infrastructure.Authorization.Contexts;
using TaskPilot.Infrastructure.Authorization.Requirements;

namespace TaskPilot.Infrastructure.Authorization.Handlers;

public sealed class WorkspaceAccessHandler
    : AuthorizationHandler<WorkspaceAccessRequirement, WorkspaceAuthorizationContext>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        WorkspaceAccessRequirement requirement,
        WorkspaceAuthorizationContext resource)
    {
        if (requirement.AccessLevel == WorkspaceAccessLevel.Member)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (resource.WorkspaceMember.Role == Role.Owner)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
