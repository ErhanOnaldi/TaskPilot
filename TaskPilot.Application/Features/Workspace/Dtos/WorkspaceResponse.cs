namespace TaskPilot.Application.Features.Workspace.Dtos;

public sealed record WorkspaceResponse(
    int Id,
    string Name,
    bool IsArchived,
    int CreatedByUserId,
    DateTime CreatedAt,
    DateTime UpdatedAt);
