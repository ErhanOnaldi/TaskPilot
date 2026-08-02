using TaskPilot.Domain.AI;

namespace TaskPilot.Application.Features.Ai;

public sealed record CreateTaskSuggestionRequest(string Input, Guid? CausationId = null);

public sealed record AiSuggestionResponse(
    int Id,
    int ProjectId,
    AiSuggestionStatus Status,
    string? Title,
    string? Priority,
    IReadOnlyList<string> Labels,
    IReadOnlyList<string> Subtasks,
    DateTime? DueDate,
    string Provider,
    string Model,
    string PromptVersion,
    int InputTokens,
    int OutputTokens,
    long DurationMilliseconds,
    Guid? SourceEventId,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? AppliedAtUtc,
    int? AppliedTaskId);

public interface IAiSuggestionService
{
    Task<ServiceResult<AiSuggestionResponse>> CreateAsync(int projectId, CreateTaskSuggestionRequest request, Guid? correlationId, CancellationToken cancellationToken);
    Task<ServiceResult<AiSuggestionResponse>> GetAsync(int suggestionId, CancellationToken cancellationToken);
    Task<ServiceResult<AiSuggestionResponse>> ApplyAsync(int suggestionId, CancellationToken cancellationToken);
}
