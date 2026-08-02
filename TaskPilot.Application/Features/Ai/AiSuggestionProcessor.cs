using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Infrastructure.Messaging;
using TaskPilot.Domain.AI;

namespace TaskPilot.Application.Features.Ai;

public sealed class AiSuggestionProcessor(
    IAiSuggestionRepository repository,
    IProjectAiContextReader contextReader,
    IAiStructuredGenerator generator,
    IAiOutputGuard outputGuard,
    IEventOutbox outbox,
    IDateTimeProvider clock,
    IAiRunTelemetry? telemetry = null) : IAiSuggestionProcessor
{
    private const string FailureMessage = "AI generation could not be completed.";

    public async Task ProcessAsync(int suggestionId, AgentExecutionScope scope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);
        if (scope.ProjectId is null)
            return;

        var claim = await repository.ClaimForProcessingAsync(suggestionId, scope, cancellationToken);
        if (claim.Disposition is AiSuggestionClaimDisposition.AlreadyHandled or
            AiSuggestionClaimDisposition.ScopeMismatch or
            AiSuggestionClaimDisposition.NotFound)
            return;

        if (claim.Suggestion is null)
            return;

        if (claim.Disposition == AiSuggestionClaimDisposition.MembershipRequired)
        {
            await FailAsync(claim.Suggestion, scope, CancellationToken.None);
            return;
        }

        AiGenerationResult generated;
        ProjectAiContext context;
        try
        {
            context = await contextReader.ReadAsync(scope, cancellationToken);
            generated = await generator.GenerateTaskSuggestionAsync(
                claim.Suggestion.InputText,
                scope,
                context,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await FailAsync(claim.Suggestion, scope, CancellationToken.None);
            return;
        }
        catch (Exception exception)
        {
            telemetry?.GenerationFailed(scope, exception);
            await FailAsync(claim.Suggestion, scope, CancellationToken.None);
            return;
        }

        var constrained = ConstrainLabels(generated.Suggestion, context.Labels);
        var outputValidation = outputGuard.Validate(constrained);
        if (!outputValidation.IsAllowed || !HasSafeMetadata(generated.Metadata))
        {
            telemetry?.OutputRejected(
                scope,
                outputValidation.Reason ?? "AI generation metadata failed the safety policy.");
            await FailAsync(claim.Suggestion, scope, CancellationToken.None);
            return;
        }

        var completionEventId = Guid.NewGuid();
        var completedAt = clock.UtcNow;
        await outbox.EnqueueAsync(
            () => new AiSuggestionCompletedEvent(
                completionEventId,
                scope.CorrelationId,
                scope.CausationId,
                claim.Suggestion.Id,
                scope.UserId,
                scope.ProjectId.Value,
                true,
                completedAt),
            cancellationToken);
        await repository.CompleteProcessingAsync(
            claim.Suggestion.Id,
            scope.UserId,
            scope.CausationId,
            constrained,
            generated.Metadata with { SourceEventId = scope.CausationId },
            completedAt,
            cancellationToken);
    }

    private async Task FailAsync(
        TaskPilot.Domain.Entities.AiSuggestion suggestion,
        AgentExecutionScope scope,
        CancellationToken cancellationToken)
    {
        var completionEventId = Guid.NewGuid();
        var completedAt = clock.UtcNow;
        await outbox.EnqueueAsync(
            () => new AiSuggestionCompletedEvent(
                completionEventId,
                scope.CorrelationId,
                scope.CausationId,
                suggestion.Id,
                scope.UserId,
                scope.ProjectId!.Value,
                false,
                completedAt),
            cancellationToken);
        await repository.FailProcessingAsync(
            suggestion.Id,
            scope.UserId,
            scope.CausationId,
            FailureMessage,
            completedAt,
            cancellationToken);
    }

    private static AiTaskSuggestion ConstrainLabels(AiTaskSuggestion suggestion, IReadOnlySet<string> availableLabels)
    {
        var canonicalLabels = availableLabels.ToDictionary(label => label, StringComparer.OrdinalIgnoreCase);
        var labels = suggestion.Labels
            .Where(label => canonicalLabels.TryGetValue(label, out _))
            .Select(label => canonicalLabels[label])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return suggestion with { Labels = labels };
    }

    private static bool HasSafeMetadata(AiGenerationMetadata metadata) =>
        metadata is not null &&
        !string.IsNullOrWhiteSpace(metadata.Provider) && metadata.Provider.Length <= 100 &&
        !string.IsNullOrWhiteSpace(metadata.Model) && metadata.Model.Length <= 200 &&
        !string.IsNullOrWhiteSpace(metadata.PromptVersion) && metadata.PromptVersion.Length <= 100 &&
        metadata.InputTokens is >= 0 and <= 100_000 &&
        metadata.OutputTokens is >= 0 and <= 2_048 &&
        metadata.DurationMilliseconds is >= 0 and <= 600_000;
}
