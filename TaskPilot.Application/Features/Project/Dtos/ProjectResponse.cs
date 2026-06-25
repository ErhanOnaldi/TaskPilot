using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.Project.Dtos;

public sealed record ProjectResponse(
    int Id,
    int WorkspaceId,
    string Name,
    string? Description,
    ProjectStatus Status,
    int CreatedByUserId,
    DateTime CreatedAt,
    DateTime UpdatedAt);