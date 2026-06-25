using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Interfaces.Persistence.Project;

public interface IProjectMemberRepository : IGenericRepository<ProjectMember>
{
    Task<bool> IsProjectManagerAsync(
        int projectId,
        int userId,
        CancellationToken cancellationToken);
}