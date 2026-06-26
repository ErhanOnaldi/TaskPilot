namespace TaskPilot.Application.Features.Notifications.Dtos;

public sealed record NotificationResponse(
    int Id,
    string Type,
    string Title,
    string Message,
    bool IsRead,
    int? RelatedEntityId,
    DateTime CreatedAt);
