using TaskPilot.Application.Features.Knowledge.Dtos;

namespace TaskPilot.Application.Features.Knowledge.Services;

public interface IKnowledgeNoteService
{
    Task<ServiceResult<NoteResponse>> GetAsync(int noteId, CancellationToken cancellationToken);
    Task<ServiceResult<NoteResponse>> CreateAsync(int workspaceId, CreateNoteRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<NoteResponse>> UpdateAsync(int noteId, UpdateNoteRequest request, CancellationToken cancellationToken);
    Task<ServiceResult> DeleteAsync(int noteId, CancellationToken cancellationToken);
    Task<ServiceResult<NoteResponse>> RestoreRevisionAsync(int noteId, int revisionId, int expectedVersion, CancellationToken cancellationToken);
}
