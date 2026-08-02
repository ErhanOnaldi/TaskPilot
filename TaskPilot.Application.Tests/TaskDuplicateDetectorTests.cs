using TaskPilot.Application.Features.Semantic.Contracts;
using TaskPilot.Application.Features.Semantic.Services;
using TaskPilot.Domain.AI.Semantic;

namespace TaskPilot.Application.Tests;

public sealed class TaskDuplicateDetectorTests
{
    [Fact]
    public async Task DetectAsync_returns_warning_for_task_results_at_or_above_the_similarity_threshold()
    {
        var authorization = new AuthorizationPort(new SemanticSearchScope(10, new HashSet<int> { 20 }, false));
        var repository = new Repository(
        [
            new SemanticSearchCandidate(1, SemanticSourceType.Task, 71, 20, "Repair sign-in flow", 0, .91f),
            new SemanticSearchCandidate(2, SemanticSourceType.Task, 72, 20, "Refresh documentation", 0, .79f)
        ]);
        var detector = new TaskDuplicateDetector(
            authorization,
            repository,
            new Embeddings(),
            new TaskDuplicateDetectionOptions(TopK: 4, SimilarityThreshold: .8f));

        var warning = await detector.DetectAsync(20, 99, "Fix login failure", CancellationToken.None);

        var match = Assert.Single(warning!.Matches);
        Assert.Equal(71, match.TaskId);
        Assert.Equal("Repair sign-in flow", match.Content);
        Assert.Equal(.91f, match.Similarity);
        Assert.Equal(new HashSet<int> { 20 }, repository.Scope!.ProjectIds);
        Assert.False(repository.Scope.IncludesWorkspaceWideDocuments);
        Assert.Equal(4, repository.TopK);
        Assert.Equal(99, repository.ExcludedTaskId);
    }

    [Fact]
    public async Task DetectAsync_returns_no_warning_when_no_result_meets_the_similarity_threshold()
    {
        var detector = new TaskDuplicateDetector(
            new AuthorizationPort(new SemanticSearchScope(10, new HashSet<int> { 20 }, false)),
            new Repository([new SemanticSearchCandidate(1, SemanticSourceType.Task, 71, 20, "Repair sign-in flow", 0, .79f)]),
            new Embeddings(),
            new TaskDuplicateDetectionOptions(TopK: 3, SimilarityThreshold: .8f));

        var warning = await detector.DetectAsync(20, 99, "Fix login failure", CancellationToken.None);

        Assert.Null(warning);
    }

    private sealed class AuthorizationPort(SemanticSearchScope? scope) : ISemanticAuthorizationPort
    {
        public Task<SemanticSearchScope?> GetReadScopeAsync(int workspaceId, CancellationToken cancellationToken) => Task.FromResult(scope);
        public Task<SemanticSearchScope?> GetProjectReadScopeAsync(int projectId, CancellationToken cancellationToken) => Task.FromResult(scope);
    }

    private sealed class Embeddings : IEmbeddingGenerator
    {
        public Task<(float[] Vector, string Model)> GenerateAsync(string content, CancellationToken cancellationToken) => Task.FromResult((new[] { 1f }, "test"));
    }

    private sealed class Repository(IReadOnlyList<SemanticSearchCandidate> results) : ISemanticDocumentRepository
    {
        public SemanticSearchScope? Scope { get; private set; }
        public int TopK { get; private set; }
        public int ExcludedTaskId { get; private set; }

        public Task<bool> UpsertIfContentChangedAsync(SemanticDocument document, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task DeactivateAsync(SemanticSourceType sourceType, int sourceId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<SemanticSearchCandidate>> SearchScopedAsync(SemanticSearchScope scope, string query, float[]? queryEmbedding, int maxCount, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<SemanticSearchCandidate>>([]);
        public Task<IReadOnlyList<SemanticSearchCandidate>> SearchTaskDuplicatesAsync(SemanticSearchScope scope, int excludedTaskId, string query, float[] queryEmbedding, int topK, CancellationToken cancellationToken)
        {
            Scope = scope;
            TopK = topK;
            ExcludedTaskId = excludedTaskId;
            return Task.FromResult(results);
        }
    }
}
