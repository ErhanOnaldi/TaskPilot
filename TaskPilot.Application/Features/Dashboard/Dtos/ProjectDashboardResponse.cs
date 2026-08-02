namespace TaskPilot.Application.Features.Dashboard.Dtos;

public sealed record ProjectDashboardResponse(
    int ProjectId,
    int TotalTasks,
    int TodoTasks,
    int InProgressTasks,
    int InReviewTasks,
    int DoneTasks,
    int CancelledTasks,
    int OverdueTasks,
    int AssignedTasks,
    int UnassignedTasks,
    decimal CompletionRate,
    IReadOnlyList<DashboardCommentResponse> RecentComments,
    IReadOnlyList<DashboardTaskResponse> HighestPriorityOpenTasks)
{
    public static decimal CalculateCompletionRate(int doneTasks, int totalTasks, int cancelledTasks)
    {
        var eligibleTasks = totalTasks - cancelledTasks;
        return eligibleTasks <= 0 ? 0m : (decimal)doneTasks / eligibleTasks;
    }
}

public sealed record DashboardCommentResponse(
    int Id,
    int TaskId,
    int UserId,
    string Content,
    DateTime CreatedAt);

public sealed record DashboardTaskResponse(
    int Id,
    string Title,
    TaskPilot.Domain.Entities.TaskItemPriority Priority,
    DateTime? DueDate);
