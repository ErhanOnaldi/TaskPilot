namespace TaskPilot.Application.Features.Workspace.Dtos;

public sealed record WorkspaceListItemResponse(
    int Id,
    string Name,
    bool IsArchived,
    DateTime CreatedAt,
    DateTime UpdatedAt);
