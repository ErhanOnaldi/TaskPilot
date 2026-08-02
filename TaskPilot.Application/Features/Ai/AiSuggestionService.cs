using System.Net;
using System.Text.Json;
using TaskPilot.Application.Authorization.Abstractions;
using TaskPilot.Application.Authorization.Enums;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Domain.AI;
using TaskPilot.Domain.Entities;
using TaskPilot.Domain.Policies;

namespace TaskPilot.Application.Features.Ai;

public sealed class AiSuggestionService(
    IAiSuggestionRepository suggestions,
    IAiSuggestionApplyPort applyPort,
    IUnitOfWork unitOfWork,
    IAccessControlService accessControl,
    IAiInputGuard inputGuard,
    IAiOutputGuard outputGuard,
    IDateTimeProvider clock,
    TaskPilot.Application.Interfaces.Infrastructure.Messaging.IEventOutbox outbox) : IAiSuggestionService
{
    public async Task<ServiceResult<AiSuggestionResponse>> CreateAsync(int projectId, CreateTaskSuggestionRequest request, Guid? correlationId, CancellationToken cancellationToken)
    {
        if (request is null)
            return ServiceResult<AiSuggestionResponse>.Fail("AI suggestion input is required.", HttpStatusCode.BadRequest);
        var guard = inputGuard.Validate(request.Input);
        if (!guard.IsAllowed) return ServiceResult<AiSuggestionResponse>.Fail(guard.Reason!, HttpStatusCode.BadRequest);

        var access = await accessControl.AuthorizeProjectAsync(projectId, ProjectAccessLevel.Read, true, cancellationToken);
        if (access.Failure is not null)
            return Denied<AiSuggestionResponse>(access.Failure);

        if (await suggestions.CountRequestedByUserOnUtcDateAsync(access.CurrentUserId, DateOnly.FromDateTime(clock.UtcNow), cancellationToken) >= 20)
            return ServiceResult<AiSuggestionResponse>.Fail("Daily AI suggestion quota has been reached.", HttpStatusCode.TooManyRequests);
        var effectiveCorrelationId = correlationId ?? Guid.NewGuid();
        var scope = new AgentExecutionScope(
            access.CurrentUserId,
            access.Project.WorkspaceId,
            projectId,
            effectiveCorrelationId,
            request.CausationId ?? effectiveCorrelationId);
        var entity = new AiSuggestion
        {
            ProjectId = projectId,
            RequestedByUserId = scope.UserId,
            InputText = request.Input.Trim(),
            Status = AiSuggestionStatus.Pending.ToString(),
            CreatedAt = clock.UtcNow
        };
        await suggestions.AddAsync(entity, cancellationToken);
        var requestedEventId = Guid.NewGuid();
        var requestedAt = clock.UtcNow;
        await outbox.EnqueueAsync(
            () => new AiSuggestionRequestedEvent(
                requestedEventId,
                scope.CorrelationId,
                request.CausationId,
                entity.Id,
                scope.UserId,
                scope.WorkspaceId,
                projectId,
                requestedAt),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult<AiSuggestionResponse>.Success(AiSuggestionMetadataCodec.ToResponse(entity), HttpStatusCode.Accepted);
    }

    public async Task<ServiceResult<AiSuggestionResponse>> GetAsync(int suggestionId, CancellationToken cancellationToken)
    {
        var entity = await suggestions.GetByIdAsync(suggestionId, cancellationToken);
        if (entity is null) return ServiceResult<AiSuggestionResponse>.Fail("AI suggestion not found.", HttpStatusCode.NotFound);
        var access = await RequireRequestorMembershipAsync(entity, cancellationToken);
        if (access.Failure is not null)
            return ServiceResult<AiSuggestionResponse>.Fail(access.Failure.ErrorMessages!, access.Failure.Status);
        if (Enum.TryParse<AiSuggestionStatus>(entity.Status, out var status) &&
            status is AiSuggestionStatus.Completed or AiSuggestionStatus.Applied &&
            !HasValidStoredPayload(entity))
            return ServiceResult<AiSuggestionResponse>.Fail("AI suggestion payload is invalid.", HttpStatusCode.Conflict);
        return ServiceResult<AiSuggestionResponse>.Success(AiSuggestionMetadataCodec.ToResponse(entity));
    }

    public async Task<ServiceResult<AiSuggestionResponse>> ApplyAsync(int suggestionId, CancellationToken cancellationToken)
    {
        var entity = await suggestions.GetByIdAsync(suggestionId, cancellationToken);
        if (entity is null) return ServiceResult<AiSuggestionResponse>.Fail("AI suggestion not found.", HttpStatusCode.NotFound);
        var access = await RequireRequestorMembershipAsync(entity, cancellationToken);
        if (access.Failure is not null) return ServiceResult<AiSuggestionResponse>.Fail(access.Failure.ErrorMessages!, access.Failure.Status);
        if (!Enum.TryParse<AiSuggestionStatus>(entity.Status, out var status) || status == AiSuggestionStatus.Failed)
            return ServiceResult<AiSuggestionResponse>.Fail("AI suggestion cannot be applied.", HttpStatusCode.Conflict);
        if (status == AiSuggestionStatus.Applied)
            return ServiceResult<AiSuggestionResponse>.Success(AiSuggestionMetadataCodec.ToResponse(entity));
        if (status != AiSuggestionStatus.Completed)
            return ServiceResult<AiSuggestionResponse>.Fail("AI suggestion is not ready to apply.", HttpStatusCode.Conflict);

        var details = AiSuggestionMetadataCodec.Deserialize(entity.SuggestedSubtasks);
        if (details is null || !outputGuard.Validate(details.Suggestion).IsAllowed || !HasSafeMetadata(details.Metadata))
            return ServiceResult<AiSuggestionResponse>.Fail("AI suggestion payload is invalid.", HttpStatusCode.Conflict);
        if (!TaskCreationPolicy.IsValid(details.Suggestion.Title, details.Suggestion.DueDate, clock.UtcNow))
            return ServiceResult<AiSuggestionResponse>.Fail("AI suggestion no longer satisfies task creation rules.", HttpStatusCode.Conflict);

        try
        {
            var applied = await applyPort.ApplyOnceAsync(
                entity.Id,
                access.UserId,
                details.Suggestion,
                details.Metadata,
                clock.UtcNow,
                cancellationToken);

            return applied.Disposition switch
            {
                AiSuggestionApplyDisposition.Applied or AiSuggestionApplyDisposition.AlreadyApplied when applied.Suggestion is not null =>
                    ServiceResult<AiSuggestionResponse>.Success(AiSuggestionMetadataCodec.ToResponse(applied.Suggestion)),
                AiSuggestionApplyDisposition.MembershipRequired =>
                    ServiceResult<AiSuggestionResponse>.Fail("Project membership is required.", HttpStatusCode.Forbidden),
                _ => ServiceResult<AiSuggestionResponse>.Fail("AI suggestion is not ready to apply.", HttpStatusCode.Conflict)
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return ServiceResult<AiSuggestionResponse>.Fail("AI suggestion could not be applied.", HttpStatusCode.InternalServerError);
        }
    }

    private async Task<(ServiceResult? Failure, int UserId)> RequireRequestorMembershipAsync(AiSuggestion entity, CancellationToken cancellationToken)
    {
        var access = await accessControl.AuthorizeProjectAsync(entity.ProjectId, ProjectAccessLevel.Read, true, cancellationToken);
        if (access.Failure is not null) return (access.Failure, access.CurrentUserId);
        if (access.CurrentUserId != entity.RequestedByUserId)
            return (ServiceResult.Fail("Only the requesting project member may access this AI suggestion.", HttpStatusCode.Forbidden), access.CurrentUserId);
        return (null, access.CurrentUserId);
    }

    private void MarkFailed(AiSuggestion entity)
    {
        entity.Status = AiSuggestionStatus.Failed.ToString();
        entity.ErrorMessage = "AI generation could not be completed.";
        entity.CompletedAt = clock.UtcNow;
    }

    private static ServiceResult<T> Denied<T>(ServiceResult? failure) =>
        failure is null
            ? ServiceResult<T>.Fail("Project membership is required.", HttpStatusCode.Forbidden)
            : ServiceResult<T>.Fail(failure.ErrorMessages!, failure.Status);

    private static bool HasSafeMetadata(AiGenerationMetadata metadata) =>
        metadata is not null &&
        !string.IsNullOrWhiteSpace(metadata.Provider) && metadata.Provider.Length <= 100 &&
        !string.IsNullOrWhiteSpace(metadata.Model) && metadata.Model.Length <= 200 &&
        !string.IsNullOrWhiteSpace(metadata.PromptVersion) && metadata.PromptVersion.Length <= 100 &&
        metadata.InputTokens is >= 0 and <= 100_000 &&
        metadata.OutputTokens is >= 0 and <= 2_048 &&
        metadata.DurationMilliseconds is >= 0 and <= 600_000;

    private bool HasValidStoredPayload(AiSuggestion entity)
    {
        var stored = AiSuggestionMetadataCodec.Deserialize(entity.SuggestedSubtasks);
        return stored is not null &&
               outputGuard.Validate(stored.Suggestion).IsAllowed &&
               HasSafeMetadata(stored.Metadata);
    }
}

public sealed record AiStoredPayload(AiTaskSuggestion Suggestion, AiGenerationMetadata Metadata);

public static class AiSuggestionMetadataCodec
{
    public static string Serialize(AiTaskSuggestion suggestion, AiGenerationMetadata metadata) => JsonSerializer.Serialize(new AiStoredPayload(suggestion, metadata));
    public static AiStoredPayload? Deserialize(string? value)
    {
        try { return string.IsNullOrWhiteSpace(value) ? null : JsonSerializer.Deserialize<AiStoredPayload>(value); }
        catch (JsonException) { return null; }
    }
    public static AiSuggestionResponse ToResponse(AiSuggestion entity)
    {
        var stored = Deserialize(entity.SuggestedSubtasks);
        var metadata = stored?.Metadata ?? new AiGenerationMetadata("unknown", "unknown", "unknown", 0, 0, 0, null);
        Enum.TryParse<AiSuggestionStatus>(entity.Status, out var status);
        return new AiSuggestionResponse(entity.Id, entity.ProjectId, status, stored?.Suggestion.Title, stored?.Suggestion.Priority, stored?.Suggestion.Labels ?? [], stored?.Suggestion.Subtasks ?? [], stored?.Suggestion.DueDate, metadata.Provider, metadata.Model, metadata.PromptVersion, metadata.InputTokens, metadata.OutputTokens, metadata.DurationMilliseconds, metadata.SourceEventId, entity.CreatedAt, entity.CompletedAt, metadata.AppliedAtUtc, metadata.AppliedTaskId ?? entity.TaskId);
    }
}
