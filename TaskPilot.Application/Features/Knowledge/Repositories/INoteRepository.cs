using TaskPilot.Domain.Knowledge;

namespace TaskPilot.Application.Features.Knowledge.Repositories;

public interface INoteRepository
{
    ValueTask<Note?> GetByIdAsync(int noteId, CancellationToken cancellationToken);
    Task<Note?> GetBySlugAsync(int workspaceId, string normalizedSlug, CancellationToken cancellationToken);
    Task<bool> ExistsBySlugAsync(int workspaceId, int noteIdToExclude, string normalizedSlug, CancellationToken cancellationToken);
    ValueTask AddAsync(Note note, CancellationToken cancellationToken);
    Task DeleteAsync(Note note, CancellationToken cancellationToken);
}
