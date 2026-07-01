using Microsoft.EntityFrameworkCore;
using TaskPilot.Application.Interfaces.Persistence.Comments;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Persistence.EntityRepositories;

public sealed class CommentRepository : GenericRepository<Comment>, ICommentRepository
{
    private readonly AppDbContext _dbContext;

    public CommentRepository(AppDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<Comment>> GetCommentsByTaskIdAsync(int taskId, CancellationToken cancellationToken)
    {
        return _dbContext.Comments
            .AsNoTracking()
            .Where(comment => comment.TaskId == taskId)
            .OrderBy(comment => comment.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
