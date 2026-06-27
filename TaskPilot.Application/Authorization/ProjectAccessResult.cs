using ProjectEntity = TaskPilot.Domain.Entities.Project;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Authorization;

public sealed record ProjectAccessResult(
    ProjectEntity Project,
    WorkSpace Workspace,
    WorkspaceMember WorkspaceMember,
    int CurrentUserId,
    ServiceResult? Failure)
{
    public static ProjectAccessResult Fail(ServiceResult failure, int currentUserId)
    {
        return new ProjectAccessResult(null!, null!, null!, currentUserId, failure);
    }
}
