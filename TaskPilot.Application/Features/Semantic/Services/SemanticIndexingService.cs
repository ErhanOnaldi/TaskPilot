using TaskPilot.Application.Features.Semantic.Contracts;
using TaskPilot.Domain.AI.Semantic;

namespace TaskPilot.Application.Features.Semantic.Services;

public sealed class SemanticIndexingService(
    ISemanticDocumentRepository repository,
    IEmbeddingGenerator embeddingGenerator,
    IActiveEmbeddingModelProvider? modelProvider = null) : ISemanticIndexingService
{
    public async Task HandleAsync(SemanticContentChangedEvent @event, CancellationToken cancellationToken)
    {
        if (@event.IsDeleted)
        {
            await repository.DeactivateAsync(
                @event.SourceType,
                @event.SourceId,
                @event.WorkspaceId,
                @event.ProjectId,
                @event.ChunkIndex,
                @event.OccurredAt,
                cancellationToken);
            return;
        }
        var hash = SemanticContentHasher.Compute(@event.Content);
        var activeModel = modelProvider is null
            ? null
            : await modelProvider.GetActiveModelAsync(cancellationToken);
        if (activeModel is not null &&
            await repository.IsContentCurrentAsync(@event.SourceType, @event.SourceId, hash, activeModel, cancellationToken))
            return;
        var embedding = activeModel is null
            ? await embeddingGenerator.GenerateAsync(@event.Content, cancellationToken)
            : await embeddingGenerator.GenerateAsync(@event.Content, activeModel, cancellationToken);
        await repository.UpsertIfContentChangedAsync(new SemanticDocument { WorkspaceId = @event.WorkspaceId, ProjectId = @event.ProjectId, SourceType = @event.SourceType, SourceId = @event.SourceId, ChunkIndex = @event.ChunkIndex, SourceText = @event.Content.Trim(), ContentHash = hash, Embedding = embedding.Vector, EmbeddingModel = embedding.Model, Dimensions = embedding.Vector.Length, IsActive = true, UpdatedAt = @event.OccurredAt }, cancellationToken);
    }
}
