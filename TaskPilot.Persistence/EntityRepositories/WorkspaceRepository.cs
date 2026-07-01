using Microsoft.EntityFrameworkCore;
using TaskPilot.Application.Common.Pagination;
using TaskPilot.Application.Features.Workspace.Dtos;
using TaskPilot.Application.Interfaces.Persistence.Workspace;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Persistence.EntityRepositories;

public class WorkspaceRepository : GenericRepository<WorkSpace>, IWorkspaceRepository
{
    private readonly AppDbContext _dbContext;

    public WorkspaceRepository(AppDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<WorkSpace>> GetWorkspacesByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        return await _dbContext.WorkSpaces
            .AsNoTracking()
            .Where(workspace => !workspace.IsArchived)
            .Where(workspace => workspace.Members.Any(member => member.UserId == userId))
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResponse<WorkSpace>> GetWorkspacesByUserIdAsync(
        int userId,
        WorkspaceQueryParameters query,
        CancellationToken cancellationToken)
    {
        var workspaceQuery = _dbContext.WorkSpaces
            .AsNoTracking()
            .Where(workspace => workspace.Members.Any(member => member.UserId == userId));

        if (!query.IncludeArchived)
        {
            workspaceQuery = workspaceQuery.Where(workspace => !workspace.IsArchived);
        }

        var search = query.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchPattern = $"%{search}%";
            workspaceQuery = workspaceQuery.Where(workspace => EF.Functions.ILike(workspace.Name, searchPattern));
        }

        var totalCount = await workspaceQuery.CountAsync(cancellationToken);
        var items = await ApplySorting(workspaceQuery, query)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return PagedResponse<WorkSpace>.Create(items, query.PageNumber, query.PageSize, totalCount);
    }

    public Task<WorkSpace?> GetWorkspaceForMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken)
    {
        return _dbContext.WorkSpaces
            .AsNoTracking()
            .FirstOrDefaultAsync(
                workspace =>
                    workspace.Id == workspaceId &&
                    workspace.Members.Any(member => member.UserId == userId),
                cancellationToken);
    }

    public Task<WorkSpace?> GetWorkspaceForOwnerAsync(int workspaceId, int userId, CancellationToken cancellationToken)
    {
        return _dbContext.WorkSpaces
            .Include(workspace => workspace.Members)
            .FirstOrDefaultAsync(
                workspace =>
                    workspace.Id == workspaceId &&
                    workspace.Members.Any(member => member.UserId == userId && member.Role == Role.Owner),
                cancellationToken);
    }

    private static IOrderedQueryable<WorkSpace> ApplySorting(IQueryable<WorkSpace> query, WorkspaceQueryParameters parameters)
    {
        return (parameters.SortBy, parameters.SortDirection) switch
        {
            (WorkspaceSortBy.Name, SortDirection.Asc) => query.OrderBy(workspace => workspace.Name).ThenByDescending(workspace => workspace.Id),
            (WorkspaceSortBy.Name, SortDirection.Desc) => query.OrderByDescending(workspace => workspace.Name).ThenByDescending(workspace => workspace.Id),
            (_, SortDirection.Asc) => query.OrderBy(workspace => workspace.CreatedAt).ThenByDescending(workspace => workspace.Id),
            _ => query.OrderByDescending(workspace => workspace.CreatedAt).ThenByDescending(workspace => workspace.Id)
        };
    }
}
