using TaskPilot.Application.Common.Pagination;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.Project.Dtos;

public sealed class ProjectQueryParameters : PaginationRequest
{
    public string? Search { get; init; }
    public ProjectStatus? Status { get; init; }
    public ProjectSortBy SortBy { get; init; } = ProjectSortBy.CreatedAt;
    public SortDirection SortDirection { get; init; } = SortDirection.Desc;
}

public enum ProjectSortBy
{
    CreatedAt = 1,
    Name = 2,
    Status = 3
}
