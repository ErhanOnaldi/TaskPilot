using TaskPilot.Application.Features.Semantic;
using TaskPilot.Application.Features.Semantic.Contracts;
using TaskPilot.Application.Features.Semantic.Dtos;
using TaskPilot.Application.Features.Semantic.Services;
using TaskPilot.Domain.AI.Semantic;

namespace TaskPilot.Application.Tests;

public sealed class SemanticFeatureTests
{
    [Fact]
    public void Content_hash_is_stable_for_trimmed_content() => Assert.Equal(SemanticContentHasher.Compute("hello"), SemanticContentHasher.Compute(" hello "));

    [Fact]
    public void Hybrid_ranker_prefers_stronger_vector_match()
    {
        var ranked = SemanticHybridRanker.Rank([
            new(1, SemanticSourceType.Note, 10, null, "a", .9f, .1f),
            new(2, SemanticSourceType.Note, 11, null, "b", .2f, .9f)]);
        Assert.Equal(2, ranked[0].DocumentId);
    }

    [Fact]
    public async Task Search_uses_authorized_scope_before_repository_query()
    {
        var repository = new FakeRepository();
        var service = new SemanticSearchService(new ScopePort(), repository, new Embeddings());
        var result = await service.SearchAsync(10, new SemanticSearchRequest("roadmap"), CancellationToken.None);
        Assert.True(result.IsSuccess); Assert.Equal(10, repository.Scope?.WorkspaceId);
    }

    [Fact]
    public async Task Indexing_deactivation_is_idempotent()
    {
        var repository = new FakeRepository();
        var service = new SemanticIndexingService(repository, new Embeddings());
        var @event = new SemanticContentChangedEvent(Guid.NewGuid(), SemanticSourceType.Note, 2, 10, null, "", DateTime.UtcNow, true);
        await service.HandleAsync(@event, CancellationToken.None); await service.HandleAsync(@event, CancellationToken.None);
        Assert.Equal(2, repository.Deactivated);
    }

    private sealed class ScopePort : ISemanticAuthorizationPort
    {
        public Task<SemanticSearchScope?> GetReadScopeAsync(int workspaceId, CancellationToken ct) => Task.FromResult<SemanticSearchScope?>(new(workspaceId, new HashSet<int> { 1 }, true));
        public Task<SemanticSearchScope?> GetProjectReadScopeAsync(int projectId, CancellationToken ct) => Task.FromResult<SemanticSearchScope?>(new(10, new HashSet<int> { projectId }, false));
    }
    private sealed class Embeddings : IEmbeddingGenerator { public Task<(float[] Vector, string Model)> GenerateAsync(string content, CancellationToken ct) => Task.FromResult((new[] { 1f }, "test")); }
    private sealed class FakeRepository : ISemanticDocumentRepository
    {
        public SemanticSearchScope? Scope { get; private set; } public int Deactivated { get; private set; }
        public Task<bool> UpsertIfContentChangedAsync(SemanticDocument document, CancellationToken ct) => Task.FromResult(true);
        public Task DeactivateAsync(SemanticSourceType sourceType, int sourceId, CancellationToken ct) { Deactivated++; return Task.CompletedTask; }
        public Task<IReadOnlyList<SemanticSearchCandidate>> SearchScopedAsync(SemanticSearchScope scope, string query, float[]? embedding, int max, CancellationToken ct) { Scope = scope; return Task.FromResult<IReadOnlyList<SemanticSearchCandidate>>([]); }
    }
}
