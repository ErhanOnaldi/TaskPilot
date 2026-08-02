namespace TaskPilot.Domain.AI;

public enum AiSuggestionStatus { Pending, Processing, Completed, Failed, Applied }

public sealed record AgentExecutionScope(
    int UserId,
    int WorkspaceId,
    int? ProjectId,
    Guid CorrelationId,
    Guid CausationId);

public sealed record AiTaskSuggestion(string Title, string? Priority, IReadOnlyList<string> Labels, IReadOnlyList<string> Subtasks, DateTime? DueDate);

public sealed record AiGenerationMetadata(
    string Provider,
    string Model,
    string PromptVersion,
    int InputTokens,
    int OutputTokens,
    long DurationMilliseconds,
    Guid? SourceEventId,
    DateTime? AppliedAtUtc = null,
    int? AppliedTaskId = null);
