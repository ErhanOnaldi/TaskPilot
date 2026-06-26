using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Interfaces.Persistence.Project;

public interface IProjectMemberRepository : IGenericRepository<ProjectMember>
{
    Task<List<ProjectMember>> GetMembersByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken);

    Task<ProjectMember?> GetMemberAsync(
        int projectId,
        int userId,
        CancellationToken cancellationToken);

    Task<bool> IsProjectMemberAsync(
        int projectId,
        int userId,
        CancellationToken cancellationToken);

    Task<bool> IsProjectManagerAsync(
        int projectId,
        int userId,
        CancellationToken cancellationToken);

    Task<int> CountProjectManagersAsync(
        int projectId,
        CancellationToken cancellationToken);
}
