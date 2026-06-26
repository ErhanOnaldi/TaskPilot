using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.Tasks.Dtos;

public sealed record UpdateTaskRequest(
    string Title,
    string? Description,
    DateTime? DueDate,
    TaskItemPriority Priority,
    int? AssignedUserId);
