using Microsoft.EntityFrameworkCore;
using TaskPilot.Application.Interfaces.Persistence.Project;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Persistence.EntityRepositories;

public class ProjectRepository : GenericRepository<Project>, IProjectRepository
{
    private readonly AppDbContext _dbContext;

    public ProjectRepository(AppDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<Project>> GetProjectsByWorkspaceIdAsync(int workspaceId, CancellationToken cancellationToken)
    {
        return _dbContext.Projects
            .AsNoTracking()
            .Where(project => project.WorkspaceId == workspaceId)
            .Where(project => project.Status != ProjectStatus.Archived)
            .OrderByDescending(project => project.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<Project?> GetProjectByIdAsync(int projectId, CancellationToken cancellationToken)
    {
        return _dbContext.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(project => project.Id == projectId, cancellationToken);
    }

    public Task<Project?> GetProjectForUpdateAsync(int projectId, CancellationToken cancellationToken)
    {
        return _dbContext.Projects
            .FirstOrDefaultAsync(project => project.Id == projectId, cancellationToken);
    }

    public Task<bool> ExistsByNameInWorkspaceAsync(int workspaceId, string name, CancellationToken cancellationToken)
    {
        return _dbContext.Projects
            .AsNoTracking()
            .AnyAsync(
                project =>
                    project.WorkspaceId == workspaceId &&
                    project.Name == name,
                cancellationToken);
    }

    public Task<bool> ExistsByNameInWorkspaceExceptProjectAsync(int workspaceId, int projectId, string name,
        CancellationToken cancellationToken)
    {
        return _dbContext.Projects
            .AsNoTracking()
            .AnyAsync(
                project =>
                    project.WorkspaceId == workspaceId &&
                    project.Id != projectId &&
                    project.Name == name,
                cancellationToken);
    }
}
