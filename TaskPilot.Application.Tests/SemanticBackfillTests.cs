using TaskPilot.Application.Features.Semantic.Contracts;
using TaskPilot.Application.Features.Semantic.Services;
using TaskPilot.Domain.AI.Semantic;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using TaskPilot.Application.Features.Semantic;
using TaskPilot.Infrastructure.Features.Semantic;

namespace TaskPilot.Application.Tests;

public sealed class SemanticBackfillTests
{
    [Fact]
    public void Semantic_application_registers_the_checkpointed_backfill_service()
    {
        var services = new ServiceCollection();

        services.AddSemanticApplication();

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(ISemanticBackfillService) &&
            descriptor.ImplementationType == typeof(SemanticBackfillService) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public async Task RunAsync_skips_current_hash_and_advances_checkpoint()
    {
        var source = new Sources([new(SemanticSourceType.Task, 11, 5, 2, "same", DateTime.UtcNow), new(SemanticSourceType.Task, 12, 5, 2, "changed", DateTime.UtcNow)]);
        var docs = new Documents { CurrentSourceId = 11 };
        var service = new SemanticBackfillService(source, docs, new Embeddings(), new Model());

        var checkpoint = await service.RunAsync(new SemanticBackfillCheckpoint(SemanticSourceType.Task, 0, false), 10, CancellationToken.None);

        Assert.Equal(12, checkpoint.LastSourceId);
        Assert.True(checkpoint.IsComplete);
        Assert.Single(docs.Upserts);
        Assert.Equal(12, docs.Upserts[0].SourceId);
    }

    [Fact]
    public async Task RunAsync_marks_empty_page_complete_without_embedding()
    {
        var service = new SemanticBackfillService(new Sources([]), new Documents(), new Embeddings(), new Model());
        var checkpoint = await service.RunAsync(new SemanticBackfillCheckpoint(SemanticSourceType.Note, 9, false), 50, CancellationToken.None);
        Assert.True(checkpoint.IsComplete); Assert.Equal(9, checkpoint.LastSourceId);
    }

    [Fact]
    public async Task Configuration_model_provider_returns_configured_active_model()
    {
        var provider = new ConfigurationEmbeddingModelProvider(Options.Create(new SemanticEmbeddingOptions { Model = "active-v2" }));
        Assert.Equal("active-v2", await provider.GetActiveModelAsync(CancellationToken.None));
    }

    private sealed class Sources(IReadOnlyList<SemanticBackfillSourceItem> items) : ISemanticBackfillSourcePort
    { public Task<IReadOnlyList<SemanticBackfillSourceItem>> GetPageAsync(SemanticSourceType t, int after, int size, CancellationToken ct) => Task.FromResult<IReadOnlyList<SemanticBackfillSourceItem>>(items.Where(x => x.SourceId > after).Take(size).ToList()); }
    private sealed class Model : IActiveEmbeddingModelProvider { public Task<string> GetActiveModelAsync(CancellationToken ct) => Task.FromResult("active-v1"); }
    private sealed class Embeddings : IEmbeddingGenerator { public Task<(float[] Vector, string Model)> GenerateAsync(string content, CancellationToken ct) => Task.FromResult((new[] { 1f }, "active-v1")); }
    private sealed class Documents : ISemanticDocumentRepository
    {
        public int? CurrentSourceId { get; init; } public List<SemanticDocument> Upserts { get; } = [];
        public Task<bool> IsContentCurrentAsync(SemanticSourceType t, int id, string hash, string model, CancellationToken ct) => Task.FromResult(CurrentSourceId == id);
        public Task<bool> UpsertIfContentChangedAsync(SemanticDocument document, CancellationToken ct) { Upserts.Add(document); return Task.FromResult(true); }
        public Task DeactivateAsync(SemanticSourceType t, int id, CancellationToken ct) => Task.CompletedTask;
        public Task<IReadOnlyList<SemanticSearchCandidate>> SearchScopedAsync(SemanticSearchScope scope, string query, float[]? embedding, int max, CancellationToken ct) => Task.FromResult<IReadOnlyList<SemanticSearchCandidate>>([]);
    }
}
