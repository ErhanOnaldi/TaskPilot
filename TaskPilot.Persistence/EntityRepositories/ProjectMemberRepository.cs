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

    public Task<List<ProjectMember>> GetMembersByProjectIdAsync(int projectId, CancellationToken cancellationToken)
    {
        return _dbContext.ProjectMembers
            .AsNoTracking()
            .Include(member => member.User)
            .Where(member => member.ProjectId == projectId)
            .OrderBy(member => member.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<ProjectMember?> GetMemberAsync(int projectId, int userId, CancellationToken cancellationToken)
    {
        return _dbContext.ProjectMembers
            .Include(member => member.User)
            .FirstOrDefaultAsync(
                member => member.ProjectId == projectId && member.UserId == userId,
                cancellationToken);
    }

    public Task<bool> IsProjectMemberAsync(int projectId, int userId, CancellationToken cancellationToken)
    {
        return _dbContext.ProjectMembers
            .AsNoTracking()
            .AnyAsync(
                member => member.ProjectId == projectId && member.UserId == userId,
                cancellationToken);
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

    public Task<int> CountProjectManagersAsync(int projectId, CancellationToken cancellationToken)
    {
        return _dbContext.ProjectMembers
            .AsNoTracking()
            .CountAsync(
                member => member.ProjectId == projectId && member.Role == ProjectRole.ProjectManager,
                cancellationToken);
    }
}
