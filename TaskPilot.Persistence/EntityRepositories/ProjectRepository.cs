using Microsoft.EntityFrameworkCore;
using TaskPilot.Application.Common.Pagination;
using TaskPilot.Application.Features.Project.Dtos;
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

    public async Task<PagedResponse<Project>> GetProjectsByWorkspaceIdAsync(
        int workspaceId,
        ProjectQueryParameters query,
        CancellationToken cancellationToken)
    {
        var projectQuery = _dbContext.Projects
            .AsNoTracking()
            .Where(project => project.WorkspaceId == workspaceId);

        if (query.Status.HasValue)
        {
            projectQuery = projectQuery.Where(project => project.Status == query.Status.Value);
        }
        else
        {
            projectQuery = projectQuery.Where(project => project.Status != ProjectStatus.Archived);
        }

        var search = query.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchPattern = $"%{search}%";
            projectQuery = projectQuery.Where(project =>
                EF.Functions.ILike(project.Name, searchPattern) ||
                (project.Description != null && EF.Functions.ILike(project.Description, searchPattern)));
        }

        var totalCount = await projectQuery.CountAsync(cancellationToken);
        var items = await ApplySorting(projectQuery, query)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return PagedResponse<Project>.Create(items, query.PageNumber, query.PageSize, totalCount);
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

    private static IOrderedQueryable<Project> ApplySorting(IQueryable<Project> query, ProjectQueryParameters parameters)
    {
        return (parameters.SortBy, parameters.SortDirection) switch
        {
            (ProjectSortBy.Name, SortDirection.Asc) => query.OrderBy(project => project.Name).ThenByDescending(project => project.Id),
            (ProjectSortBy.Name, SortDirection.Desc) => query.OrderByDescending(project => project.Name).ThenByDescending(project => project.Id),
            (ProjectSortBy.Status, SortDirection.Asc) => query.OrderBy(project => project.Status).ThenByDescending(project => project.Id),
            (ProjectSortBy.Status, SortDirection.Desc) => query.OrderByDescending(project => project.Status).ThenByDescending(project => project.Id),
            (_, SortDirection.Asc) => query.OrderBy(project => project.CreatedAt).ThenByDescending(project => project.Id),
            _ => query.OrderByDescending(project => project.CreatedAt).ThenByDescending(project => project.Id)
        };
    }
}
