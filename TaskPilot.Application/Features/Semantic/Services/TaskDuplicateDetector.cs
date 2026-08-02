using TaskPilot.Application.Features.Semantic.Contracts;

namespace TaskPilot.Application.Features.Semantic.Services;

/// <summary>
/// Finds already-indexed tasks that are semantically close to a new task. The caller owns
/// mutation semantics; this service only reports optional warning metadata.
/// </summary>
public sealed class TaskDuplicateDetector(
    ISemanticAuthorizationPort authorizationPort,
    ISemanticDocumentRepository repository,
    IEmbeddingGenerator embeddingGenerator,
    TaskDuplicateDetectionOptions? options = null) : ITaskDuplicateDetector
{
    private readonly TaskDuplicateDetectionOptions _options = options ?? new TaskDuplicateDetectionOptions();

    public async Task<DuplicateTaskWarning?> DetectAsync(int projectId, int taskId, string content, CancellationToken cancellationToken)
    {
        var scope = await authorizationPort.GetProjectReadScopeAsync(projectId, cancellationToken);
        if (scope is null) return null;

        var embedding = await embeddingGenerator.GenerateAsync(content.Trim(), cancellationToken);
        var matches = await repository.SearchTaskDuplicatesAsync(
            scope,
            taskId,
            content.Trim(),
            embedding.Vector,
            Math.Clamp(_options.TopK, 1, 20),
            cancellationToken);
        var threshold = Math.Clamp(_options.SimilarityThreshold, 0f, 1f);
        var similarTasks = matches
            .Where(x => x.SourceType == TaskPilot.Domain.AI.Semantic.SemanticSourceType.Task && x.VectorScore >= threshold)
            .Select(x => new SimilarTaskMatch(x.SourceId, x.Content, x.VectorScore))
            .ToList();

        return similarTasks.Count == 0 ? null : new DuplicateTaskWarning(similarTasks);
    }
}
