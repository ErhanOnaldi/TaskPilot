namespace TaskPilot.Application.Features.Project.Dtos;

public sealed record CreateProjectRequest(
    string Name,
    string? Description);