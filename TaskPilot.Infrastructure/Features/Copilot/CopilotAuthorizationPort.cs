using Microsoft.Extensions.DependencyInjection;
using TaskPilot.Application.Authorization.Abstractions;
using TaskPilot.Application.Authorization.Enums;
using TaskPilot.Application.Features.Copilot;
using TaskPilot.Application.Features.Semantic.Contracts;
using TaskPilot.Domain.AI;

namespace TaskPilot.Infrastructure.Features.Copilot;

public sealed class CopilotAuthorizationPort(IAccessControlService access, ISemanticAuthorizationPort semanticAuthorization) : ICopilotAuthorizationPort
{
    public async Task<CopilotAuthorizedScope?> AuthorizeProjectAsync(int projectId, string? correlationId, Guid? causationId, CancellationToken ct)
    {
        var project = await access.AuthorizeProjectAsync(projectId, ProjectAccessLevel.Read, true, ct);
        if (project.Failure is not null) return null;
        var semanticScope = await semanticAuthorization.GetReadScopeAsync(project.Project.WorkspaceId, ct);
        if (semanticScope is null || !semanticScope.ProjectIds.Contains(projectId)) return null;
        return new CopilotAuthorizedScope(new AgentExecutionScope(project.CurrentUserId, project.Project.WorkspaceId, projectId, ToCorrelationId(correlationId), causationId ?? Guid.NewGuid()), semanticScope.ProjectIds, semanticScope.IncludesWorkspaceWideDocuments);
    }
    public async Task<CopilotAuthorizedScope?> AuthorizeWorkspaceAsync(int workspaceId, string? correlationId, Guid? causationId, CancellationToken ct)
    {
        var workspace = await access.AuthorizeWorkspaceAsync(workspaceId, WorkspaceAccessLevel.Member, true, ct);
        if (workspace.Failure is not null) return null;
        var semanticScope = await semanticAuthorization.GetReadScopeAsync(workspaceId, ct);
        if (semanticScope is null) return null;
        return new CopilotAuthorizedScope(new AgentExecutionScope(workspace.CurrentUserId, workspaceId, null, ToCorrelationId(correlationId), causationId ?? Guid.NewGuid()), semanticScope.ProjectIds, semanticScope.IncludesWorkspaceWideDocuments);
    }
    private static Guid ToCorrelationId(string? correlationId) => Guid.TryParse(correlationId, out var value) ? value : Guid.NewGuid();
}
public static class CopilotInfrastructureServiceCollectionExtensions { public static IServiceCollection AddCopilotInfrastructure(this IServiceCollection services) { services.AddScoped<ICopilotAuthorizationPort, CopilotAuthorizationPort>(); return services; } }
