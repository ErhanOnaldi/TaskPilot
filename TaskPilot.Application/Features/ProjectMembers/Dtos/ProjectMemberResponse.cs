using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.ProjectMembers.Dtos;

public sealed record ProjectMemberResponse(
    int UserId,
    string Email,
    ProjectRole Role,
    DateTime JoinedAt);
