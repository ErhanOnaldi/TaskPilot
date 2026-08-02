using TaskPilot.Domain.Knowledge;

namespace TaskPilot.Application.Features.Knowledge.Repositories;

public interface ITaskNoteLinkRepository
{
    Task<bool> ExistsAsync(int taskId, int noteId, CancellationToken cancellationToken);
    ValueTask AddAsync(TaskNoteLink link, CancellationToken cancellationToken);
    Task RemoveAsync(int taskId, int noteId, CancellationToken cancellationToken);
}
