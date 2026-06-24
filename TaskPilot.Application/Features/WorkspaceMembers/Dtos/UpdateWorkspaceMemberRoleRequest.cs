using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.WorkspaceMembers.Dtos;

public sealed record UpdateWorkspaceMemberRoleRequest(
    Role Role);