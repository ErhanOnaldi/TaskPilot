using Microsoft.EntityFrameworkCore;
using TaskPilot.Application.Interfaces.Persistence.Workspace;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Persistence.EntityRepositories;

public class WorkspaceMemberRepository : GenericRepository<WorkspaceMember>, IWorkspaceMemberRepository
{
    private readonly AppDbContext _dbContext;

    public WorkspaceMemberRepository(AppDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<WorkspaceMember>> GetMembersByWorkspaceIdAsync(
        int workspaceId,
        CancellationToken cancellationToken)
    {
        return _dbContext.WorkspaceMembers
            .AsNoTracking()
            .Include(member => member.User)
            .Where(member => member.WorkspaceId == workspaceId)
            .OrderBy(member => member.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<WorkspaceMember?> GetMemberAsync(
        int workspaceId,
        int userId,
        CancellationToken cancellationToken)
    {
        return _dbContext.WorkspaceMembers
            .Include(member => member.User)
            .FirstOrDefaultAsync(
                member => member.WorkspaceId == workspaceId && member.UserId == userId,
                cancellationToken);
    }

    public Task<bool> IsWorkspaceMemberAsync(
        int workspaceId,
        int userId,
        CancellationToken cancellationToken)
    {
        return _dbContext.WorkspaceMembers
            .AsNoTracking()
            .AnyAsync(
                member => member.WorkspaceId == workspaceId && member.UserId == userId,
                cancellationToken);
    }

    public Task<bool> IsWorkspaceOwnerAsync(
        int workspaceId,
        int userId,
        CancellationToken cancellationToken)
    {
        return _dbContext.WorkspaceMembers
            .AsNoTracking()
            .AnyAsync(
                member =>
                    member.WorkspaceId == workspaceId &&
                    member.UserId == userId &&
                    member.Role == Role.Owner,
                cancellationToken);
    }

    public Task<int> CountOwnersAsync(
        int workspaceId,
        CancellationToken cancellationToken)
    {
        return _dbContext.WorkspaceMembers
            .AsNoTracking()
            .CountAsync(
                member => member.WorkspaceId == workspaceId && member.Role == Role.Owner,
                cancellationToken);
    }
}
