using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Authorization;

public sealed record WorkspaceAccessResult(
    WorkSpace Workspace,
    WorkspaceMember WorkspaceMember,
    int CurrentUserId,
    ServiceResult? Failure)
{
    public static WorkspaceAccessResult Fail(ServiceResult failure, int currentUserId)
    {
        return new WorkspaceAccessResult(null!, null!, currentUserId, failure);
    }
}
