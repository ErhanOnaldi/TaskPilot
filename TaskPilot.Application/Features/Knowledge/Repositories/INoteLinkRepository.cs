using TaskPilot.Domain.Knowledge;

namespace TaskPilot.Application.Features.Knowledge.Repositories;

public interface INoteLinkRepository
{
    Task ReplaceForSourceNoteAsync(Note sourceNote, IReadOnlyList<NoteLink> links, CancellationToken cancellationToken);
    Task ResolveTargetAsync(Note targetNote, CancellationToken cancellationToken);
}
