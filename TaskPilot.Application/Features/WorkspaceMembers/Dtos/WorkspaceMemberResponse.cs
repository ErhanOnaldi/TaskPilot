using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.WorkspaceMembers.Dtos;

public sealed record WorkspaceMemberResponse(
    int UserId,
    string Email,
    Role Role,
    DateTime JoinedAt);