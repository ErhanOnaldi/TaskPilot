using TaskPilot.Application.Features.Semantic.Contracts;

namespace TaskPilot.Application.Features.Semantic.Services;

public sealed class SemanticBackfillService(
    ISemanticBackfillSourcePort sourcePort,
    ISemanticDocumentRepository documentRepository,
    IEmbeddingGenerator embeddingGenerator,
    IActiveEmbeddingModelProvider modelProvider) : ISemanticBackfillService
{
    public async Task<SemanticBackfillCheckpoint> RunAsync(SemanticBackfillCheckpoint checkpoint, int batchSize, CancellationToken cancellationToken)
    {
        if (checkpoint.IsComplete) return checkpoint;
        var activeModel = await modelProvider.GetActiveModelAsync(cancellationToken);
        var page = await sourcePort.GetPageAsync(checkpoint.SourceType, checkpoint.LastSourceId, Math.Clamp(batchSize, 1, 500), cancellationToken);
        foreach (var item in page)
        {
            var contentHash = SemanticContentHasher.Compute(item.Content);
            if (await documentRepository.IsContentCurrentAsync(item.SourceType, item.SourceId, contentHash, activeModel, cancellationToken)) continue;
            var embedding = await embeddingGenerator.GenerateAsync(item.Content, activeModel, cancellationToken);
            await documentRepository.UpsertIfContentChangedAsync(new TaskPilot.Domain.AI.Semantic.SemanticDocument { WorkspaceId = item.WorkspaceId, ProjectId = item.ProjectId, SourceType = item.SourceType, SourceId = item.SourceId, ChunkIndex = 0, SourceText = item.Content.Trim(), ContentHash = contentHash, Embedding = embedding.Vector, EmbeddingModel = embedding.Model, Dimensions = embedding.Vector.Length, IsActive = true, UpdatedAt = item.UpdatedAt }, cancellationToken);
        }
        return page.Count == 0 ? checkpoint with { IsComplete = true } : new SemanticBackfillCheckpoint(checkpoint.SourceType, page[^1].SourceId, page.Count < batchSize);
    }
}
