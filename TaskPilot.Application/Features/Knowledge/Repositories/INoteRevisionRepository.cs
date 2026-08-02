using TaskPilot.Domain.Knowledge;

namespace TaskPilot.Application.Features.Knowledge.Repositories;

public interface INoteRevisionRepository
{
    ValueTask AddAsync(NoteRevision revision, CancellationToken cancellationToken);
    ValueTask<NoteRevision?> GetByIdAsync(int noteId, int revisionId, CancellationToken cancellationToken);
}
