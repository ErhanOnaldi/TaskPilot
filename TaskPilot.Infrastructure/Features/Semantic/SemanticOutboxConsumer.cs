using TaskPilot.Application.Features.Semantic.Contracts;

namespace TaskPilot.Infrastructure.Features.Semantic;

/// <summary>Adapter target for the outbox/inbox pipeline; registration is intentionally feature-local integration work.</summary>
public sealed class SemanticOutboxConsumer(ISemanticIndexingService indexingService)
{
    public Task ConsumeAsync(SemanticContentChangedEvent @event, CancellationToken cancellationToken) => indexingService.HandleAsync(@event, cancellationToken);
}
