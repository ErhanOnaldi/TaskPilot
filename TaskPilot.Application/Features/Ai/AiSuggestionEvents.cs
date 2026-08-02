using TaskPilot.Application.Interfaces.Infrastructure.Messaging;

namespace TaskPilot.Application.Features.Ai;

public sealed record AiSuggestionRequestedEvent(
    Guid EventId,
    Guid CorrelationId,
    Guid? CausationId,
    int SuggestionId,
    int UserId,
    int WorkspaceId,
    int ProjectId,
    DateTime OccurredAt) : IIntegrationEvent
{
    public string EventType => "ai.suggestion.requested";
    public int SchemaVersion => 1;
}

public sealed record AiSuggestionCompletedEvent(
    Guid EventId,
    Guid CorrelationId,
    Guid? CausationId,
    int SuggestionId,
    int UserId,
    int ProjectId,
    bool Succeeded,
    DateTime OccurredAt) : IIntegrationEvent
{
    public string EventType => "ai.suggestion.completed";
    public int SchemaVersion => 1;
}
