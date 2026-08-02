using Microsoft.EntityFrameworkCore;
using TaskPilot.Application.Features.Semantic.Contracts;
using TaskPilot.Domain.AI.Semantic;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace TaskPilot.Persistence.Features.Semantic;

public sealed class SemanticDocumentRepository(AppDbContext db) : ISemanticDocumentRepository
{
    private const string TombstoneModel = "__tombstone__";

    public Task<bool> IsContentCurrentAsync(SemanticSourceType sourceType, int sourceId, string contentHash, string model, CancellationToken cancellationToken)
        => db.Set<SemanticDocument>().AnyAsync(x => x.SourceType == sourceType && x.SourceId == sourceId && x.ContentHash == contentHash && x.EmbeddingModel == model && x.IsActive, cancellationToken);
    public Task DeactivateAsync(SemanticSourceType sourceType, int sourceId, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Semantic deactivation requires event scope and occurrence time.");
    public async Task<bool> UpsertIfContentChangedAsync(SemanticDocument document, CancellationToken cancellationToken)
    {
        if (await db.Set<SemanticDocument>().AnyAsync(x =>
                x.SourceType == document.SourceType &&
                x.SourceId == document.SourceId &&
                x.ChunkIndex == document.ChunkIndex &&
                x.EmbeddingModel == TombstoneModel &&
                x.UpdatedAt >= document.UpdatedAt,
                cancellationToken))
            return false;

        var existing = await db.Set<SemanticDocument>().SingleOrDefaultAsync(x => x.SourceType == document.SourceType && x.SourceId == document.SourceId && x.ChunkIndex == document.ChunkIndex && x.EmbeddingModel == document.EmbeddingModel, cancellationToken);
        if (existing is not null && (existing.UpdatedAt >= document.UpdatedAt || existing.ContentHash == document.ContentHash && existing.IsActive)) return false;
        if (document.Embedding is null || document.Embedding.Length != 768) throw new InvalidOperationException("The active embedding model must produce 768 dimensions.");
        if (existing is null)
        {
            await db.Set<SemanticDocument>().AddAsync(document, cancellationToken);
            db.Entry(document).Property<Vector?>("EmbeddingVector").CurrentValue = new Vector(document.Embedding);
        }
        else
        {
            existing.WorkspaceId = document.WorkspaceId; existing.ProjectId = document.ProjectId; existing.SourceText = document.SourceText; existing.ContentHash = document.ContentHash; existing.Dimensions = document.Dimensions; existing.IsActive = true; existing.UpdatedAt = document.UpdatedAt;
            db.Entry(existing).Property<Vector?>("EmbeddingVector").CurrentValue = new Vector(document.Embedding);
        }
        await db.SaveChangesAsync(cancellationToken); return true;
    }
    public async Task DeactivateAsync(
        SemanticSourceType sourceType,
        int sourceId,
        int workspaceId,
        int? projectId,
        int chunkIndex,
        DateTime occurredAt,
        CancellationToken cancellationToken)
    {
        await db.Set<SemanticDocument>()
            .Where(x => x.SourceType == sourceType && x.SourceId == sourceId && x.UpdatedAt < occurredAt)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.IsActive, false)
                .SetProperty(x => x.UpdatedAt, occurredAt), cancellationToken);

        var tombstone = await db.Set<SemanticDocument>().SingleOrDefaultAsync(x =>
            x.SourceType == sourceType && x.SourceId == sourceId &&
            x.ChunkIndex == chunkIndex && x.EmbeddingModel == TombstoneModel,
            cancellationToken);
        if (tombstone is null)
        {
            await db.Set<SemanticDocument>().AddAsync(new SemanticDocument
            {
                WorkspaceId = workspaceId,
                ProjectId = projectId,
                SourceType = sourceType,
                SourceId = sourceId,
                ChunkIndex = chunkIndex,
                SourceText = string.Empty,
                ContentHash = string.Empty,
                EmbeddingModel = TombstoneModel,
                Dimensions = 0,
                IsActive = false,
                UpdatedAt = occurredAt
            }, cancellationToken);
        }
        else if (tombstone.UpdatedAt < occurredAt)
        {
            tombstone.WorkspaceId = workspaceId;
            tombstone.ProjectId = projectId;
            tombstone.UpdatedAt = occurredAt;
        }
        await db.SaveChangesAsync(cancellationToken);
    }
    public async Task<IReadOnlyList<SemanticSearchCandidate>> SearchScopedAsync(SemanticSearchScope scope, string query, float[]? queryEmbedding, int maxCount, CancellationToken ct)
    {
        var scoped = db.Set<SemanticDocument>()
            .Where(x => x.WorkspaceId == scope.WorkspaceId && x.IsActive &&
                        (scope.IncludesWorkspaceWideDocuments && x.ProjectId == null ||
                         x.ProjectId != null && scope.ProjectIds.Contains(x.ProjectId.Value)));

        if (queryEmbedding is null)
        {
            var textDocuments = await scoped
                .Where(x => EF.Functions.ILike(x.SourceText, $"%{query}%"))
                .OrderByDescending(x => x.UpdatedAt)
                .Take(maxCount)
                .Select(x => new { x.Id, x.SourceType, x.SourceId, x.ProjectId, x.SourceText })
                .ToListAsync(ct);
            return textDocuments.Select(x => new SemanticSearchCandidate(x.Id, x.SourceType, x.SourceId, x.ProjectId, x.SourceText, 1f, 0f)).ToList();
        }

        if (queryEmbedding.Length != 768) throw new InvalidOperationException("The active query embedding must have 768 dimensions.");
        var queryVector = new Vector(queryEmbedding);
        var vectorDocuments = await scoped
            .Select(x => new
            {
                x.Id, x.SourceType, x.SourceId, x.ProjectId, x.SourceText,
                TextMatch = EF.Functions.ILike(x.SourceText, $"%{query}%"),
                Distance = EF.Property<Vector>(x, "EmbeddingVector").CosineDistance(queryVector)
            })
            .OrderBy(x => x.Distance)
            .Take(maxCount)
            .ToListAsync(ct);
        return vectorDocuments.Select(x => new SemanticSearchCandidate(
            x.Id, x.SourceType, x.SourceId, x.ProjectId, x.SourceText,
            x.TextMatch ? 1f : 0f,
            (float)Math.Clamp(1d - x.Distance, 0d, 1d))).ToList();
    }

    public async Task<IReadOnlyList<SemanticSearchCandidate>> SearchTaskDuplicatesAsync(
        SemanticSearchScope scope,
        int excludedTaskId,
        string query,
        float[] queryEmbedding,
        int topK,
        CancellationToken cancellationToken)
    {
        if (queryEmbedding.Length != 768) throw new InvalidOperationException("The active query embedding must have 768 dimensions.");
        var queryVector = new Vector(queryEmbedding);
        var taskDocuments = db.Set<SemanticDocument>()
            .Where(x => x.WorkspaceId == scope.WorkspaceId &&
                        x.ProjectId != null &&
                        scope.ProjectIds.Contains(x.ProjectId.Value) &&
                        x.IsActive &&
                        x.SourceId != excludedTaskId &&
                        x.SourceType == SemanticSourceType.Task);
        var matches = await taskDocuments
            .Select(x => new
            {
                x.Id,
                x.SourceId,
                x.ProjectId,
                x.SourceText,
                Distance = EF.Property<Vector>(x, "EmbeddingVector").CosineDistance(queryVector)
            })
            .OrderBy(x => x.Distance)
            .Take(Math.Clamp(topK, 1, 20))
            .ToListAsync(cancellationToken);
        return matches.Select(x => new SemanticSearchCandidate(
            x.Id,
            SemanticSourceType.Task,
            x.SourceId,
            x.ProjectId,
            x.SourceText,
            0f,
            (float)Math.Clamp(1d - x.Distance, 0d, 1d))).ToList();
    }
}
