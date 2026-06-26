using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.Tasks.Dtos;

public sealed record CreateTaskRequest(
    string Title,
    string? Description,
    DateTime? DueDate,
    TaskItemPriority? Priority,
    int? AssignedUserId);
