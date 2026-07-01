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
            unassignedTasks);
    }
}
