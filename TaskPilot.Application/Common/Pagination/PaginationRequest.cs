namespace TaskPilot.Application.Common.Pagination;

public class PaginationRequest
{
    public const int MaxPageSize = 100;

    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
