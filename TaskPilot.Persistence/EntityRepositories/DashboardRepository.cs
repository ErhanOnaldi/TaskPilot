using Microsoft.EntityFrameworkCore;
using TaskPilot.Application.Features.Dashboard.Dtos;
using TaskPilot.Application.Interfaces.Persistence.Dashboard;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Persistence.EntityRepositories;

public sealed class DashboardRepository(AppDbContext dbContext) : IDashboardRepository
{
    public async Task<ProjectDashboardResponse> GetProjectDashboardAsync(
        int projectId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var taskQuery = dbContext.TaskItems
            .AsNoTracking()
            .Where(task => task.ProjectId == projectId);

        var totalTasks = await taskQuery.CountAsync(cancellationToken);
        var todoTasks = await taskQuery.CountAsync(task => task.Status == TaskItemStatus.Todo, cancellationToken);
        var inProgressTasks = await taskQuery.CountAsync(task => task.Status == TaskItemStatus.InProgress, cancellationToken);
        var inReviewTasks = await taskQuery.CountAsync(task => task.Status == TaskItemStatus.InReview, cancellationToken);
        var doneTasks = await taskQuery.CountAsync(task => task.Status == TaskItemStatus.Done, cancellationToken);
        var cancelledTasks = await taskQuery.CountAsync(task => task.Status == TaskItemStatus.Cancelled, cancellationToken);
        var overdueTasks = await taskQuery.CountAsync(
            task =>
                task.DueDate.HasValue &&
                task.DueDate.Value < utcNow &&
                task.Status != TaskItemStatus.Done &&
                task.Status != TaskItemStatus.Cancelled,
            cancellationToken);
        var assignedTasks = await taskQuery.CountAsync(task => task.AssignedUserId.HasValue, cancellationToken);
        var unassignedTasks = totalTasks - assignedTasks;
        var completionRate = ProjectDashboardResponse.CalculateCompletionRate(doneTasks, totalTasks, cancelledTasks);
        var recentComments = await dbContext.Comments
            .AsNoTracking()
            .Where(comment => dbContext.TaskItems.Any(task => task.Id == comment.TaskId && task.ProjectId == projectId))
            .OrderByDescending(comment => comment.CreatedAt)
            .ThenByDescending(comment => comment.Id)
            .Take(5)
            .Select(comment => new DashboardCommentResponse(
                comment.Id,
                comment.TaskId,
                comment.UserId,
                comment.Content,
                comment.CreatedAt))
            .ToListAsync(cancellationToken);
        var highestPriorityOpenTasks = await taskQuery
            .Where(task => task.Status != TaskItemStatus.Done && task.Status != TaskItemStatus.Cancelled)
            .OrderByDescending(task => task.Priority)
            .ThenBy(task => task.DueDate == null)
            .ThenBy(task => task.DueDate)
            .ThenBy(task => task.Id)
            .Take(5)
            .Select(task => new DashboardTaskResponse(
                task.Id,
                task.Title,
                task.Priority,
                task.DueDate))
            .ToListAsync(cancellationToken);

        return new ProjectDashboardResponse(
            projectId,
            totalTasks,
            todoTasks,
            inProgressTasks,
            inReviewTasks,
            doneTasks,
            cancelledTasks,
            overdueTasks,
            assignedTasks,
            unassignedTasks,
            completionRate,
            recentComments,
            highestPriorityOpenTasks);
    }
}
