namespace TaskPilot.Application.Features.Project.Dtos;

public sealed record UpdateProjectRequest(
    string Name,
    string? Description);