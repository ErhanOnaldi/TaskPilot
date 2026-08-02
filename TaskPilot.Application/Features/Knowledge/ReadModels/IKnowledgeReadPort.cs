namespace TaskPilot.Application.Features.Knowledge.ReadModels;

public interface IKnowledgeReadPort
{
    Task<IReadOnlyList<NoteLinkReadModel>> GetLinksAsync(int workspaceId, int noteId, CancellationToken cancellationToken);
    Task<IReadOnlyList<NoteLinkReadModel>> GetBacklinksAsync(int workspaceId, int noteId, CancellationToken cancellationToken);
    Task<IReadOnlyList<NoteRevisionReadModel>> GetRevisionsAsync(int workspaceId, int noteId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeSearchItem>> SearchAsync(int workspaceId, string query, int maxCount, CancellationToken cancellationToken);
    Task<KnowledgeGraphReadModel> GetGraphAsync(int workspaceId, int? projectId, CancellationToken cancellationToken);
}
