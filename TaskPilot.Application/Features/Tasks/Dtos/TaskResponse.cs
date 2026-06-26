using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.Tasks.Dtos;

public sealed record TaskResponse(
    int Id,
    int ProjectId,
    string Title,
    string? Description,
    TaskItemStatus Status,
    TaskItemPriority Priority,
    DateTime? DueDate,
    int? AssignedUserId,
    int CreatedByUserId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? CompletedAt);
