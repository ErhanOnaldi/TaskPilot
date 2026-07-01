using TaskPilot.Application.Common.Pagination;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.Tasks.Dtos;

public sealed class TaskQueryParameters : PaginationRequest
{
    public TaskItemStatus? Status { get; init; }
    public TaskItemPriority? Priority { get; init; }
    public int? AssignedUserId { get; init; }
    public string? Search { get; init; }
    public TaskSortBy SortBy { get; init; } = TaskSortBy.CreatedAt;
    public SortDirection SortDirection { get; init; } = SortDirection.Desc;
}

public enum TaskSortBy
{
    CreatedAt = 1,
    DueDate = 2,
    Priority = 3,
    Status = 4,
    Title = 5
}
