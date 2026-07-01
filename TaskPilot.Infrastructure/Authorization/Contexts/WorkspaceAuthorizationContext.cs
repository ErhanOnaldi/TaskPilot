using TaskPilot.Domain.Entities;

namespace TaskPilot.Infrastructure.Authorization.Contexts;

public sealed record WorkspaceAuthorizationContext(
    WorkSpace Workspace,
    WorkspaceMember WorkspaceMember,
    int CurrentUserId);
