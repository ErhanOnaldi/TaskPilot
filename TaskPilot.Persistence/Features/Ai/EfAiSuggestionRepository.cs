using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskPilot.Application.Features.Ai;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Domain.AI;
using TaskPilot.Domain.Entities;
using TaskPilot.Domain.Policies;

namespace TaskPilot.Persistence.Features.Ai;

public sealed class EfAiSuggestionRepository(AppDbContext context, IUnitOfWork unitOfWork)
    : IAiSuggestionRepository, IAiSuggestionApplyPort
{
    public Task<AiSuggestion?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        context.Set<AiSuggestion>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async ValueTask AddAsync(AiSuggestion suggestion, CancellationToken cancellationToken) =>
        _ = await context.Set<AiSuggestion>().AddAsync(suggestion, cancellationToken);

    public Task<int> CountRequestedByUserOnUtcDateAsync(int userId, DateOnly utcDate, CancellationToken cancellationToken)
    {
        var start = utcDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = start.AddDays(1);
        return context.Set<AiSuggestion>().CountAsync(x => x.RequestedByUserId == userId && x.CreatedAt >= start && x.CreatedAt < end, cancellationToken);
    }

    public async Task<AiSuggestionClaimResult> ClaimForProcessingAsync(
        int suggestionId,
        AgentExecutionScope scope,
        CancellationToken cancellationToken)
    {
        if (scope.ProjectId is null)
            return new AiSuggestionClaimResult(AiSuggestionClaimDisposition.ScopeMismatch, null);

        var scoped = context.Set<AiSuggestion>().Where(suggestion =>
            suggestion.Id == suggestionId &&
            suggestion.RequestedByUserId == scope.UserId &&
            suggestion.ProjectId == scope.ProjectId.Value &&
            context.Set<Project>().Any(project =>
                project.Id == suggestion.ProjectId && project.WorkspaceId == scope.WorkspaceId));
        var current = await scoped.AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (current is null)
        {
            var exists = await context.Set<AiSuggestion>().AnyAsync(x => x.Id == suggestionId, cancellationToken);
            return new AiSuggestionClaimResult(
                exists ? AiSuggestionClaimDisposition.ScopeMismatch : AiSuggestionClaimDisposition.NotFound,
                null);
        }

        if (!string.Equals(current.Status, AiSuggestionStatus.Pending.ToString(), StringComparison.Ordinal))
            return new AiSuggestionClaimResult(AiSuggestionClaimDisposition.AlreadyHandled, current);

        var authorizedClaimCount = await AuthorizedSuggestionQuery(suggestionId, scope.UserId, scope.WorkspaceId)
            .Where(item => item.ProjectId == scope.ProjectId.Value && item.Status == AiSuggestionStatus.Pending.ToString())
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(item => item.Status, AiSuggestionStatus.Processing.ToString()),
                cancellationToken);
        if (authorizedClaimCount == 1)
        {
            current.Status = AiSuggestionStatus.Processing.ToString();
            return new AiSuggestionClaimResult(AiSuggestionClaimDisposition.Claimed, current);
        }

        var unauthorizedClaimCount = await scoped
            .Where(item => item.Status == AiSuggestionStatus.Pending.ToString())
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(item => item.Status, AiSuggestionStatus.Processing.ToString()),
                cancellationToken);
        if (unauthorizedClaimCount == 1)
        {
            current.Status = AiSuggestionStatus.Processing.ToString();
            return new AiSuggestionClaimResult(AiSuggestionClaimDisposition.MembershipRequired, current);
        }

        return new AiSuggestionClaimResult(AiSuggestionClaimDisposition.AlreadyHandled, current);
    }

    public Task<bool> CompleteProcessingAsync(
        int suggestionId,
        int requestingUserId,
        Guid notificationSourceEventId,
        AiTaskSuggestion suggestion,
        AiGenerationMetadata metadata,
        DateTime completedAtUtc,
        CancellationToken cancellationToken) =>
        PersistTerminalStateAsync(
            suggestionId,
            requestingUserId,
            notificationSourceEventId,
            AiSuggestionStatus.Completed,
            "AI suggestion ready",
            "Your AI task suggestion is ready to review.",
            null,
            suggestion,
            metadata,
            completedAtUtc,
            cancellationToken);

    public Task<bool> FailProcessingAsync(
        int suggestionId,
        int requestingUserId,
        Guid notificationSourceEventId,
        string publicErrorMessage,
        DateTime completedAtUtc,
        CancellationToken cancellationToken) =>
        PersistTerminalStateAsync(
            suggestionId,
            requestingUserId,
            notificationSourceEventId,
            AiSuggestionStatus.Failed,
            "AI suggestion failed",
            "Your AI task suggestion could not be completed.",
            publicErrorMessage,
            null,
            null,
            completedAtUtc,
            cancellationToken);

    private async Task<bool> PersistTerminalStateAsync(
        int suggestionId,
        int requestingUserId,
        Guid notificationSourceEventId,
        AiSuggestionStatus terminalStatus,
        string notificationTitle,
        string notificationMessage,
        string? errorMessage,
        AiTaskSuggestion? suggestion,
        AiGenerationMetadata? metadata,
        DateTime completedAtUtc,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            var storedPayload = suggestion is null || metadata is null
                ? null
                : AiSuggestionMetadataCodec.Serialize(suggestion, metadata);
            var suggestedLabels = suggestion is null ? null : System.Text.Json.JsonSerializer.Serialize(suggestion.Labels);
            var updateCount = await context.Set<AiSuggestion>()
                .Where(item =>
                    item.Id == suggestionId &&
                    item.RequestedByUserId == requestingUserId &&
                    item.Status == AiSuggestionStatus.Processing.ToString())
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(item => item.Status, terminalStatus.ToString())
                        .SetProperty(item => item.SuggestedPriority, suggestion == null ? null : suggestion.Priority)
                        .SetProperty(item => item.SuggestedLabels, suggestedLabels)
                        .SetProperty(item => item.SuggestedSubtasks, storedPayload)
                        .SetProperty(item => item.SuggestedDueDate, suggestion == null ? null : suggestion.DueDate)
                        .SetProperty(item => item.ErrorMessage, errorMessage)
                        .SetProperty(item => item.CompletedAt, completedAtUtc),
                    cancellationToken);
            if (updateCount != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            await context.Set<Notification>().AddAsync(new Notification
            {
                UserId = requestingUserId,
                Type = terminalStatus == AiSuggestionStatus.Completed ? "AiSuggestionCompleted" : "AiSuggestionFailed",
                Title = notificationTitle,
                Message = notificationMessage,
                RelatedEntityId = suggestionId,
                SourceEventId = notificationSourceEventId,
                CreatedAt = completedAtUtc,
                UpdatedAt = completedAtUtc
            }, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<AiSuggestionApplyResult> ApplyOnceAsync(
        int suggestionId,
        int requestingUserId,
        AiTaskSuggestion suggestion,
        AiGenerationMetadata metadata,
        DateTime appliedAtUtc,
        CancellationToken cancellationToken)
    {
        if (!TaskCreationPolicy.IsValid(suggestion.Title, suggestion.DueDate, appliedAtUtc))
            throw new InvalidOperationException("AI suggestion violates task creation rules.");

        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            var claimCount = await AuthorizedSuggestionQuery(suggestionId, requestingUserId)
                .Where(item => item.Status == AiSuggestionStatus.Completed.ToString())
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(item => item.Status, "Applying"),
                    cancellationToken);

            if (claimCount == 0)
            {
                var stillAuthorized = await AuthorizedSuggestionQuery(suggestionId, requestingUserId)
                    .AnyAsync(cancellationToken);
                var current = stillAuthorized
                    ? await context.Set<AiSuggestion>().AsNoTracking().SingleOrDefaultAsync(item => item.Id == suggestionId, cancellationToken)
                    : null;
                await transaction.RollbackAsync(cancellationToken);

                if (!stillAuthorized)
                    return new AiSuggestionApplyResult(AiSuggestionApplyDisposition.MembershipRequired, null);
                return string.Equals(current?.Status, AiSuggestionStatus.Applied.ToString(), StringComparison.Ordinal)
                    ? new AiSuggestionApplyResult(AiSuggestionApplyDisposition.AlreadyApplied, current)
                    : new AiSuggestionApplyResult(AiSuggestionApplyDisposition.NotReady, current);
            }

            var task = new TaskItem
            {
                ProjectId = await context.Set<AiSuggestion>()
                    .Where(item => item.Id == suggestionId)
                    .Select(item => item.ProjectId)
                    .SingleAsync(cancellationToken),
                Title = suggestion.Title.Trim(),
                Priority = Enum.TryParse<TaskItemPriority>(suggestion.Priority, true, out var priority)
                    ? priority
                    : TaskItemPriority.Medium,
                DueDate = suggestion.DueDate,
                CreatedByUserId = requestingUserId,
                CreatedAt = appliedAtUtc,
                UpdatedAt = appliedAtUtc
            };
            await context.Set<TaskItem>().AddAsync(task, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var storedPayload = AiSuggestionMetadataCodec.Serialize(
                suggestion,
                metadata with { AppliedAtUtc = appliedAtUtc, AppliedTaskId = task.Id });
            var completionCount = await context.Set<AiSuggestion>()
                .Where(item => item.Id == suggestionId &&
                               item.RequestedByUserId == requestingUserId &&
                               item.Status == "Applying")
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(item => item.Status, AiSuggestionStatus.Applied.ToString())
                        .SetProperty(item => item.TaskId, task.Id)
                        .SetProperty(item => item.SuggestedSubtasks, storedPayload),
                    cancellationToken);
            if (completionCount != 1)
                throw new InvalidOperationException("The AI suggestion apply claim was lost.");

            var applied = await context.Set<AiSuggestion>()
                .AsNoTracking()
                .SingleAsync(item => item.Id == suggestionId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new AiSuggestionApplyResult(AiSuggestionApplyDisposition.Applied, applied);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private IQueryable<AiSuggestion> AuthorizedSuggestionQuery(int suggestionId, int requestingUserId, int? workspaceId = null) =>
        context.Set<AiSuggestion>().Where(suggestion =>
            suggestion.Id == suggestionId &&
            suggestion.RequestedByUserId == requestingUserId &&
            context.Set<Project>().Any(project =>
                project.Id == suggestion.ProjectId &&
                (!workspaceId.HasValue || project.WorkspaceId == workspaceId.Value) &&
                project.Status != ProjectStatus.Archived &&
                context.Set<WorkSpace>().Any(workspace =>
                    workspace.Id == project.WorkspaceId &&
                    !workspace.IsArchived &&
                    context.Set<WorkspaceMember>().Any(member =>
                        member.WorkspaceId == workspace.Id && member.UserId == requestingUserId &&
                        (member.Role == Role.Owner || context.Set<ProjectMember>().Any(projectMember =>
                            projectMember.ProjectId == project.Id && projectMember.UserId == requestingUserId))))));
}

public static class AiPersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddAiPersistence(this IServiceCollection services)
    {
        services.AddScoped<EfAiSuggestionRepository>();
        services.AddScoped<IAiSuggestionRepository>(provider => provider.GetRequiredService<EfAiSuggestionRepository>());
        services.AddScoped<IAiSuggestionApplyPort>(provider => provider.GetRequiredService<EfAiSuggestionRepository>());
        services.AddScoped<ProjectAiContextReader>();
        services.AddScoped<IProjectAiContextReader>(provider => provider.GetRequiredService<ProjectAiContextReader>());
        return services;
    }
}
