using ProjectEntity = TaskPilot.Domain.Entities.Project;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Infrastructure.Authorization.Contexts;

public sealed record ProjectAuthorizationContext(
    ProjectEntity Project,
    WorkSpace Workspace,
    WorkspaceMember WorkspaceMember,
    ProjectMember? ProjectMember,
    int CurrentUserId);
