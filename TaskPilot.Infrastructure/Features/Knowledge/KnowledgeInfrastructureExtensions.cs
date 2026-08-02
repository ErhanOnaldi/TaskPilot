using System.Net;
using Microsoft.Extensions.DependencyInjection;
using TaskPilot.Application;
using TaskPilot.Application.Features.Knowledge.Authorization;
using TaskPilot.Application.Authorization.Abstractions;
using TaskPilot.Application.Authorization.Enums;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Persistence.Workspace;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Infrastructure.Features.Knowledge;

public static class KnowledgeInfrastructureExtensions
{
    public static IServiceCollection AddKnowledgeInfrastructure(this IServiceCollection services) =>
        services.AddScoped<IKnowledgeAccessPort, KnowledgeAccessAdapter>();
}

internal sealed class KnowledgeAccessAdapter(
    ICurrentUserService currentUser,
    IWorkspaceRepository workspaces,
    IWorkspaceMemberRepository members,
    IAccessControlService accessControl) : IKnowledgeAccessPort
{
    public Task<KnowledgeAccessResult> AuthorizeAsync(
        int workspaceId,
        KnowledgeAccessLevel accessLevel,
        CancellationToken cancellationToken) =>
        AuthorizeAsync(workspaceId, null, accessLevel, cancellationToken);

    public async Task<KnowledgeAccessResult> AuthorizeAsync(
        int workspaceId,
        int? projectId,
        KnowledgeAccessLevel accessLevel,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.GetRequiredUserId();
        var workspace = await workspaces.GetByIdAsync(workspaceId);
        if (workspace is null)
        {
            return KnowledgeAccessResult.Denied(
                ServiceResult.Fail("Workspace not found.", HttpStatusCode.NotFound), userId);
        }

        if (workspace.IsArchived)
        {
            return KnowledgeAccessResult.Denied(
                ServiceResult.Fail("Workspace is archived.", HttpStatusCode.BadRequest), userId);
        }

        var membership = await members.GetMemberAsync(workspaceId, userId, cancellationToken);
        if (membership is null)
        {
            return KnowledgeAccessResult.Denied(
                ServiceResult.Fail("Workspace not found.", HttpStatusCode.NotFound), userId);
        }

        if (projectId.HasValue)
        {
            var projectAccess = await accessControl.AuthorizeProjectAsync(
                projectId.Value,
                accessLevel == KnowledgeAccessLevel.Edit
                    ? ProjectAccessLevel.Participant
                    : ProjectAccessLevel.Read,
                true,
                cancellationToken);
            if (projectAccess.Failure is not null || projectAccess.Workspace.Id != workspaceId)
            {
                return KnowledgeAccessResult.Denied(
                    ServiceResult.Fail("Knowledge resource not found.", HttpStatusCode.NotFound),
                    userId);
            }
        }

        var allowed = accessLevel switch
        {
            KnowledgeAccessLevel.Read => true,
            KnowledgeAccessLevel.Edit => membership.Role is Role.Owner or Role.Manager or Role.Member,
            KnowledgeAccessLevel.Manage => membership.Role is Role.Owner or Role.Manager,
            _ => false
        };

        return allowed
            ? new KnowledgeAccessResult(userId, null)
            : KnowledgeAccessResult.Denied(
                ServiceResult.Fail("You do not have permission to modify workspace knowledge.", HttpStatusCode.Forbidden),
                userId);
    }
}
