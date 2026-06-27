namespace TaskPilot.Application.Authorization;

public interface IAccessControlService
{
    Task<WorkspaceAccessResult> AuthorizeWorkspaceAsync(
        int workspaceId,
        WorkspaceAccessLevel accessLevel,
        bool requireActiveWorkspace,
        CancellationToken cancellationToken);

    Task<ProjectAccessResult> AuthorizeProjectAsync(
        int projectId,
        ProjectAccessLevel accessLevel,
        bool requireActiveProject,
        CancellationToken cancellationToken);
}
