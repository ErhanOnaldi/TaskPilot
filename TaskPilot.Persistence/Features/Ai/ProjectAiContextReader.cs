using Microsoft.EntityFrameworkCore;
using TaskPilot.Application.Features.Ai;
using TaskPilot.Domain.AI;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Persistence.Features.Ai;
public sealed class ProjectAiContextReader(AppDbContext db) : IProjectAiContextReader
{
    public async Task<ProjectAiContext> ReadAsync(AgentExecutionScope scope, CancellationToken cancellationToken)
    {
        if (scope.ProjectId is null || !await IsAuthorizedAsync(scope, cancellationToken))
            throw new UnauthorizedAccessException("Project membership is required.");

        var labels = (await db.Set<Label>()
                .AsNoTracking()
                .Where(label => label.ProjectId == scope.ProjectId.Value)
                .OrderBy(label => label.Name)
                .Take(50)
                .Select(label => label.Name)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var members = await db.Set<ProjectMember>()
            .AsNoTracking()
            .Where(member => member.ProjectId == scope.ProjectId.Value)
            .OrderBy(member => member.UserId)
            .Take(50)
            .Select(member => member.User!.Email)
            .ToListAsync(cancellationToken);
        var openTasks = await db.Set<TaskItem>()
            .AsNoTracking()
            .Where(task =>
                task.ProjectId == scope.ProjectId.Value &&
                task.Status != TaskItemStatus.Done &&
                task.Status != TaskItemStatus.Cancelled)
            .OrderByDescending(task => task.UpdatedAt)
            .Take(50)
            .Select(task => task.Title)
            .ToListAsync(cancellationToken);
        return new ProjectAiContext(labels, members, openTasks);
    }

    private Task<bool> IsAuthorizedAsync(AgentExecutionScope scope, CancellationToken cancellationToken) =>
        db.Set<Project>().AnyAsync(project =>
            project.Id == scope.ProjectId!.Value &&
            project.WorkspaceId == scope.WorkspaceId &&
            project.Status != ProjectStatus.Archived &&
            !project.WorkSpace!.IsArchived &&
            db.Set<WorkspaceMember>().Any(member =>
                member.WorkspaceId == project.WorkspaceId &&
                member.UserId == scope.UserId &&
                (member.Role == Role.Owner || db.Set<ProjectMember>().Any(projectMember =>
                    projectMember.ProjectId == project.Id && projectMember.UserId == scope.UserId))),
            cancellationToken);
}
