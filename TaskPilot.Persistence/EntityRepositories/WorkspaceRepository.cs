using Microsoft.EntityFrameworkCore;
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
}
