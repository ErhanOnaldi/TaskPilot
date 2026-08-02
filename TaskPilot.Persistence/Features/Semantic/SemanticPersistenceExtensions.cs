using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskPilot.Application.Features.Semantic.Contracts;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Persistence.Features.Semantic;

public static class SemanticPersistenceExtensions
{
    public static IServiceCollection AddSemanticPersistence(this IServiceCollection services) => services
        .AddScoped<ISemanticDocumentRepository, SemanticDocumentRepository>()
        .AddScoped<ISemanticBackfillSourcePort, SemanticBackfillSourceRepository>()
        .AddScoped<ISemanticAuthorizationPort, SemanticAuthorizationRepository>();
}

internal sealed class SemanticAuthorizationRepository(AppDbContext db, ICurrentUserService currentUser)
    : ISemanticAuthorizationPort
{
    public async Task<SemanticSearchScope?> GetReadScopeAsync(int workspaceId, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetRequiredUserId();
        var membership = await db.Set<WorkspaceMember>()
            .Where(x => x.WorkspaceId == workspaceId && x.UserId == userId && !x.WorkSpace!.IsArchived)
            .Select(x => new { x.Role })
            .SingleOrDefaultAsync(cancellationToken);
        if (membership is null) return null;

        var projectIds = membership.Role == Role.Owner
            ? await db.Set<Project>()
                .Where(x => x.WorkspaceId == workspaceId && x.Status != ProjectStatus.Archived)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken)
            : await db.Set<ProjectMember>()
                .Where(x => x.UserId == userId && x.Project!.WorkspaceId == workspaceId && x.Project.Status != ProjectStatus.Archived)
                .Select(x => x.ProjectId)
                .ToListAsync(cancellationToken);

        return new SemanticSearchScope(workspaceId, projectIds.ToHashSet(), true);
    }

    public async Task<SemanticSearchScope?> GetProjectReadScopeAsync(int projectId, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetRequiredUserId();
        var scope = await (
            from project in db.Set<Project>()
            join membership in db.Set<WorkspaceMember>()
                on new { project.WorkspaceId, UserId = userId } equals new { membership.WorkspaceId, membership.UserId }
            where project.Id == projectId
                  && project.Status != ProjectStatus.Archived
                  && !project.WorkSpace!.IsArchived
            select new { project.WorkspaceId, membership.Role })
            .SingleOrDefaultAsync(cancellationToken);
        if (scope is null) return null;

        var isProjectMember = await db.Set<ProjectMember>()
            .AnyAsync(x => x.ProjectId == projectId && x.UserId == userId, cancellationToken);
        if (scope.Role != Role.Owner && !isProjectMember) return null;

        return new SemanticSearchScope(scope.WorkspaceId, new HashSet<int> { projectId }, false);
    }
}
