using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.WorkspaceMembers.Dtos;

public sealed record AddWorkspaceMemberRequest(
    int UserId,
    Role Role);