namespace TaskPilot.Application.Features.Labels.Dtos;

public sealed record LabelResponse(
    int Id,
    int ProjectId,
    string Name,
    string Color,
    DateTime CreatedAt);
