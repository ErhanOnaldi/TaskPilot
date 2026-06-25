using Microsoft.EntityFrameworkCore;
using TaskPilot.Application.Interfaces.Persistence.Project;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Persistence.EntityRepositories;

public class ProjectMemberRepository : GenericRepository<ProjectMember>, IProjectMemberRepository
{
    private readonly AppDbContext _dbContext;

    public ProjectMemberRepository(AppDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> IsProjectManagerAsync(int projectId, int userId, CancellationToken cancellationToken)
    {
        return _dbContext.ProjectMembers
            .AsNoTracking()
            .AnyAsync(
                member =>
                    member.ProjectId == projectId &&
                    member.UserId == userId &&
                    member.Role == ProjectRole.ProjectManager,
                cancellationToken);
    }
}
