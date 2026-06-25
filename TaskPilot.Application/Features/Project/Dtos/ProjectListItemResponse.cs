using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.Project.Dtos;

public sealed record ProjectListItemResponse(
    int Id,
    string Name,
    string? Description,
    ProjectStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);