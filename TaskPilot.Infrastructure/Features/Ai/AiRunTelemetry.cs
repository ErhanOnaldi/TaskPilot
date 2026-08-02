using Microsoft.Extensions.Logging;
using TaskPilot.Application.Features.Ai;
using TaskPilot.Domain.AI;

namespace TaskPilot.Infrastructure.Features.Ai;

public sealed class AiRunTelemetry(ILogger<AiRunTelemetry> logger) : IAiRunTelemetry
{
    public void GenerationFailed(AgentExecutionScope scope, Exception exception) =>
        logger.LogWarning(
            exception,
            "AI generation failed for correlation {CorrelationId}, causation {CausationId}, workspace {WorkspaceId}, project {ProjectId}.",
            scope.CorrelationId,
            scope.CausationId,
            scope.WorkspaceId,
            scope.ProjectId);

    public void OutputRejected(AgentExecutionScope scope, string reason) =>
        logger.LogWarning(
            "AI output rejected for correlation {CorrelationId}, causation {CausationId}, workspace {WorkspaceId}, project {ProjectId}: {Reason}",
            scope.CorrelationId,
            scope.CausationId,
            scope.WorkspaceId,
            scope.ProjectId,
            reason);
}
