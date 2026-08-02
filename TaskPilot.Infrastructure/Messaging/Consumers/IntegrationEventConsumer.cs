using System.Text.Json;
using MassTransit;
using TaskPilot.Application.Events;
using TaskPilot.Application.Features.Ai;
using TaskPilot.Application.Features.Notifications.Services;
using TaskPilot.Application.Interfaces.Infrastructure.Caching;
using TaskPilot.Application.Interfaces.Infrastructure.Messaging;
using TaskPilot.Application.Messaging;
using TaskPilot.Application.Features.Semantic.Contracts;
using TaskPilot.Infrastructure.Features.Semantic;
using TaskPilot.Domain.AI;

namespace TaskPilot.Infrastructure.Messaging.Consumers;

public sealed class IntegrationEventConsumer(
    INotificationEventHandler notificationEventHandler,
    IDashboardCacheInvalidator dashboardCacheInvalidator,
    SemanticOutboxConsumer semanticConsumer,
    IAiSuggestionProcessor aiSuggestionProcessor) : IConsumer<IntegrationEventEnvelope>
{
    public async Task Consume(ConsumeContext<IntegrationEventEnvelope> context)
    {
        var envelope = context.Message;
        switch (envelope.EventType)
        {
            case "task.created":
                await notificationEventHandler.HandleAsync(Deserialize<TaskCreatedEvent>(envelope), context.CancellationToken);
                break;
            case "task.assigned":
                await notificationEventHandler.HandleAsync(Deserialize<TaskAssignedEvent>(envelope), context.CancellationToken);
                break;
            case "comment.added":
                await notificationEventHandler.HandleAsync(Deserialize<CommentAddedEvent>(envelope), context.CancellationToken);
                break;
            case "workspace.member-invited":
                await notificationEventHandler.HandleAsync(Deserialize<WorkspaceMemberInvitedEvent>(envelope), context.CancellationToken);
                break;
            case "project.dashboard-invalidation.requested":
                var dashboardEvent = Deserialize<ProjectDashboardInvalidationRequestedEvent>(envelope);
                await dashboardCacheInvalidator.InvalidateProjectDashboardAsync(dashboardEvent.ProjectId, context.CancellationToken);
                break;
            case "semantic.content-changed":
                await semanticConsumer.ConsumeAsync(Deserialize<SemanticContentChangedEvent>(envelope), context.CancellationToken);
                break;
            case "ai.suggestion.requested":
                var requested = Deserialize<AiSuggestionRequestedEvent>(envelope);
                EnsureEnvelopeMatches(envelope, requested);
                var executionScope = new AgentExecutionScope(
                    requested.UserId,
                    requested.WorkspaceId,
                    requested.ProjectId,
                    envelope.CorrelationId,
                    envelope.EventId);
                await aiSuggestionProcessor.ProcessAsync(requested.SuggestionId, executionScope, context.CancellationToken);
                break;
            case "ai.suggestion.completed":
                EnsureEnvelopeMatches(envelope, Deserialize<AiSuggestionCompletedEvent>(envelope));
                break;
            default:
                throw new InvalidOperationException($"Unsupported integration event type '{envelope.EventType}' (schema {envelope.SchemaVersion}).");
        }
    }

    private static TEvent Deserialize<TEvent>(IntegrationEventEnvelope envelope)
    {
        return JsonSerializer.Deserialize<TEvent>(envelope.Payload)
            ?? throw new InvalidOperationException($"Event payload for '{envelope.EventType}' is invalid.");
    }

    private static void EnsureEnvelopeMatches(IntegrationEventEnvelope envelope, IIntegrationEvent integrationEvent)
    {
        if (integrationEvent.EventId != envelope.EventId ||
            integrationEvent.CorrelationId != envelope.CorrelationId ||
            integrationEvent.CausationId != envelope.CausationId ||
            integrationEvent.EventType != envelope.EventType ||
            integrationEvent.SchemaVersion != envelope.SchemaVersion)
            throw new InvalidOperationException($"Event payload metadata for '{envelope.EventType}' does not match its envelope.");
    }
}
