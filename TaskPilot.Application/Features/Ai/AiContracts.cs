using TaskPilot.Domain.AI;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.Ai;

public interface IAiSuggestionRepository
{
    Task<AiSuggestion?> GetByIdAsync(int id, CancellationToken cancellationToken);
    ValueTask AddAsync(AiSuggestion suggestion, CancellationToken cancellationToken);
    Task<int> CountRequestedByUserOnUtcDateAsync(int userId, DateOnly utcDate, CancellationToken cancellationToken);
    Task<AiSuggestionClaimResult> ClaimForProcessingAsync(int suggestionId, AgentExecutionScope scope, CancellationToken cancellationToken);
    Task<bool> CompleteProcessingAsync(
        int suggestionId,
        int requestingUserId,
        Guid notificationSourceEventId,
        AiTaskSuggestion suggestion,
        AiGenerationMetadata metadata,
        DateTime completedAtUtc,
        CancellationToken cancellationToken);
    Task<bool> FailProcessingAsync(
        int suggestionId,
        int requestingUserId,
        Guid notificationSourceEventId,
        string publicErrorMessage,
        DateTime completedAtUtc,
        CancellationToken cancellationToken);
}

public interface IAiSuggestionProcessor { Task ProcessAsync(int suggestionId, AgentExecutionScope scope, CancellationToken cancellationToken); }
public interface IAiRunTelemetry
{
    void GenerationFailed(AgentExecutionScope scope, Exception exception);
    void OutputRejected(AgentExecutionScope scope, string reason);
}
public interface IProjectAiContextReader { Task<ProjectAiContext> ReadAsync(AgentExecutionScope scope, CancellationToken cancellationToken); }
public sealed record ProjectAiContext(IReadOnlySet<string> Labels, IReadOnlyList<string> Members, IReadOnlyList<string> OpenTasks)
{
    public static ProjectAiContext Empty { get; } = new(new HashSet<string>(StringComparer.OrdinalIgnoreCase), [], []);
}

public enum AiSuggestionClaimDisposition
{
    Claimed,
    MembershipRequired,
    AlreadyHandled,
    ScopeMismatch,
    NotFound
}

public sealed record AiSuggestionClaimResult(AiSuggestionClaimDisposition Disposition, AiSuggestion? Suggestion);

public interface IAiSuggestionApplyPort
{
    Task<AiSuggestionApplyResult> ApplyOnceAsync(
        int suggestionId,
        int requestingUserId,
        AiTaskSuggestion suggestion,
        AiGenerationMetadata metadata,
        DateTime appliedAtUtc,
        CancellationToken cancellationToken);
}

public enum AiSuggestionApplyDisposition
{
    Applied,
    AlreadyApplied,
    MembershipRequired,
    NotReady
}

public sealed record AiSuggestionApplyResult(AiSuggestionApplyDisposition Disposition, AiSuggestion? Suggestion);

public interface IAiStructuredGenerator
{
    Task<AiGenerationResult> GenerateTaskSuggestionAsync(string input, AgentExecutionScope scope, CancellationToken cancellationToken);
    Task<AiGenerationResult> GenerateTaskSuggestionAsync(string input, AgentExecutionScope scope, ProjectAiContext? context, CancellationToken cancellationToken) => GenerateTaskSuggestionAsync(input, scope, cancellationToken);
}

public interface IAiChatGenerator
{
    Task<string> CompleteAsync(IReadOnlyList<AiChatMessage> messages, AgentExecutionScope scope, CancellationToken cancellationToken);
    IAsyncEnumerable<string> StreamAsync(IReadOnlyList<AiChatMessage> messages, AgentExecutionScope scope, CancellationToken cancellationToken);
}

public sealed record AiChatMessage(string Role, string Content);
public sealed record AiGenerationResult(AiTaskSuggestion Suggestion, AiGenerationMetadata Metadata);

public interface IAiInputGuard
{
    AiGuardResult Validate(string input);
}

public interface IAiOutputGuard
{
    AiGuardResult Validate(AiTaskSuggestion suggestion);
}

public sealed record AiGuardResult(bool IsAllowed, string? Reason)
{
    public static AiGuardResult Allow() => new(true, null);
    public static AiGuardResult Deny(string reason) => new(false, reason);
}
