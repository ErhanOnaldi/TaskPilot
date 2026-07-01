using TaskPilot.Application.Common.Pagination;

namespace TaskPilot.Application.Features.Workspace.Dtos;

public sealed class WorkspaceQueryParameters : PaginationRequest
{
    public string? Search { get; init; }
    public bool IncludeArchived { get; init; }
    public WorkspaceSortBy SortBy { get; init; } = WorkspaceSortBy.CreatedAt;
    public SortDirection SortDirection { get; init; } = SortDirection.Desc;
}

public enum WorkspaceSortBy
{
    CreatedAt = 1,
    Name = 2
}
