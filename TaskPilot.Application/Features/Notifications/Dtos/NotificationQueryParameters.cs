using TaskPilot.Application.Common.Pagination;

namespace TaskPilot.Application.Features.Notifications.Dtos;

public sealed class NotificationQueryParameters : PaginationRequest
{
    public bool? IsRead { get; init; }
    public string? Type { get; init; }
    public NotificationSortBy SortBy { get; init; } = NotificationSortBy.CreatedAt;
    public SortDirection SortDirection { get; init; } = SortDirection.Desc;
}

public enum NotificationSortBy
{
    CreatedAt = 1,
    IsRead = 2,
    Type = 3
}
