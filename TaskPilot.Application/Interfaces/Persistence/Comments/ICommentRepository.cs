using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Interfaces.Persistence.Comments;

public interface ICommentRepository : IGenericRepository<Comment>
{
    Task<List<Comment>> GetCommentsByTaskIdAsync(
        int taskId,
        CancellationToken cancellationToken);
}
