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
    int UnassignedTasks);
