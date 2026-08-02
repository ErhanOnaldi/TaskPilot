using TaskPilot.Domain.AI.Semantic;
using TaskPilot.Application.Interfaces.Infrastructure.Messaging;

namespace TaskPilot.Application.Features.Semantic.Contracts;

public sealed record SemanticContentChangedEvent(Guid EventId, SemanticSourceType SourceType, int SourceId, int WorkspaceId, int? ProjectId, string Content, DateTime OccurredAt, bool IsDeleted = false, int ChunkIndex = 0) : IIntegrationEvent
{
    public Guid CorrelationId { get; init; } = EventId;
    public Guid? CausationId { get; init; }
    public string EventType => "semantic.content-changed";
    public int SchemaVersion => 1;
}
public sealed record SemanticSearchScope(int WorkspaceId, IReadOnlySet<int> ProjectIds, bool IncludesWorkspaceWideDocuments);
public sealed record SemanticSearchCandidate(int DocumentId, SemanticSourceType SourceType, int SourceId, int? ProjectId, string Content, float TextScore, float VectorScore);
public sealed record SimilarTaskMatch(int TaskId, string Content, float Similarity);
public sealed record DuplicateTaskWarning(IReadOnlyList<SimilarTaskMatch> Matches);
public sealed record TaskDuplicateDetectionOptions(int TopK = 3, float SimilarityThreshold = .8f);

public interface ISemanticDocumentRepository
{
    Task<bool> IsContentCurrentAsync(SemanticSourceType sourceType, int sourceId, string contentHash, string model, CancellationToken cancellationToken) => Task.FromResult(false);
    Task<bool> UpsertIfContentChangedAsync(SemanticDocument document, CancellationToken cancellationToken);
    Task DeactivateAsync(SemanticSourceType sourceType, int sourceId, CancellationToken cancellationToken);
    Task DeactivateAsync(
        SemanticSourceType sourceType,
        int sourceId,
        int workspaceId,
        int? projectId,
        int chunkIndex,
        DateTime occurredAt,
        CancellationToken cancellationToken) =>
        DeactivateAsync(sourceType, sourceId, cancellationToken);
    Task<IReadOnlyList<SemanticSearchCandidate>> SearchScopedAsync(SemanticSearchScope scope, string query, float[]? queryEmbedding, int maxCount, CancellationToken cancellationToken);
    async Task<IReadOnlyList<SemanticSearchCandidate>> SearchTaskDuplicatesAsync(SemanticSearchScope scope, int excludedTaskId, string query, float[] queryEmbedding, int topK, CancellationToken cancellationToken) =>
        (await SearchScopedAsync(scope, query, queryEmbedding, topK, cancellationToken))
        .Where(x => x.SourceId != excludedTaskId)
        .Where(x => x.SourceType == SemanticSourceType.Task)
        .ToList();
}

public interface ISemanticAuthorizationPort
{
    Task<SemanticSearchScope?> GetReadScopeAsync(int workspaceId, CancellationToken cancellationToken);
    Task<SemanticSearchScope?> GetProjectReadScopeAsync(int projectId, CancellationToken cancellationToken);
}

public interface IEmbeddingGenerator
{
    Task<(float[] Vector, string Model)> GenerateAsync(string content, CancellationToken cancellationToken);
    Task<(float[] Vector, string Model)> GenerateAsync(string content, string model, CancellationToken cancellationToken) => GenerateAsync(content, cancellationToken);
}

public interface ITaskDuplicateDetector
{
    Task<DuplicateTaskWarning?> DetectAsync(int projectId, int taskId, string content, CancellationToken cancellationToken);
}

public interface IActiveEmbeddingModelProvider { Task<string> GetActiveModelAsync(CancellationToken cancellationToken); }

public sealed record SemanticBackfillSourceItem(SemanticSourceType SourceType, int SourceId, int WorkspaceId, int? ProjectId, string Content, DateTime UpdatedAt);
public interface ISemanticBackfillSourcePort
{
    /// <summary>Returns a tenant-scoped, SourceId-ordered page. Persistence must filter workspace/project before materializing rows.</summary>
    Task<IReadOnlyList<SemanticBackfillSourceItem>> GetPageAsync(SemanticSourceType sourceType, int afterSourceId, int pageSize, CancellationToken cancellationToken);
}

public interface ISemanticIndexingService
{
    Task HandleAsync(SemanticContentChangedEvent @event, CancellationToken cancellationToken);
}

public interface ISemanticBackfillService
{
    Task<SemanticBackfillCheckpoint> RunAsync(SemanticBackfillCheckpoint checkpoint, int batchSize, CancellationToken cancellationToken);
}

public sealed record SemanticBackfillCheckpoint(SemanticSourceType SourceType, int LastSourceId, bool IsComplete);
