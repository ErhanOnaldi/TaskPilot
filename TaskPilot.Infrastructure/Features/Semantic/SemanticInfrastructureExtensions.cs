using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TaskPilot.Application.Features.Semantic.Contracts;

namespace TaskPilot.Infrastructure.Features.Semantic;

public sealed class SemanticEmbeddingOptions
{
    public string Endpoint { get; init; } = "http://localhost:11434";
    public string Model { get; init; } = "nomic-embed-text";
    public int TimeoutSeconds { get; init; } = 30;
    public int MaxInputCharacters { get; init; } = 12_000;
    public int DuplicateTopK { get; init; } = 3;
    public float DuplicateSimilarityThreshold { get; init; } = .8f;
}

public static class SemanticInfrastructureExtensions
{
    public static IServiceCollection AddSemanticInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SemanticEmbeddingOptions>(configuration.GetSection("Semantic"));
        services.AddHttpClient<OllamaEmbeddingGenerator>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<SemanticEmbeddingOptions>>().Value;
            client.BaseAddress = new Uri(options.Endpoint, UriKind.Absolute);
        });
        services.AddScoped<IEmbeddingGenerator>(provider => provider.GetRequiredService<OllamaEmbeddingGenerator>());
        services.AddScoped(provider =>
        {
            var options = provider.GetRequiredService<IOptions<SemanticEmbeddingOptions>>().Value;
            return new TaskDuplicateDetectionOptions(options.DuplicateTopK, options.DuplicateSimilarityThreshold);
        });
        services.AddSingleton<IActiveEmbeddingModelProvider, ConfigurationEmbeddingModelProvider>();
        services.AddScoped<SemanticOutboxConsumer>();
        return services;
    }
}

public sealed class ConfigurationEmbeddingModelProvider(IOptions<SemanticEmbeddingOptions> options) : IActiveEmbeddingModelProvider
{
    public Task<string> GetActiveModelAsync(CancellationToken cancellationToken)
    {
        var model = options.Value.Model?.Trim();
        if (string.IsNullOrWhiteSpace(model)) throw new InvalidOperationException("An active semantic embedding model must be configured.");
        return Task.FromResult(model);
    }
}

internal sealed class OllamaEmbeddingGenerator(HttpClient client, IOptions<SemanticEmbeddingOptions> options)
    : IEmbeddingGenerator
{
    public async Task<(float[] Vector, string Model)> GenerateAsync(string content, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var input = content.Length <= settings.MaxInputCharacters
            ? content
            : content[..settings.MaxInputCharacters];
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(settings.TimeoutSeconds, 1, 120)));
        using var response = await client.PostAsJsonAsync("api/embed", new { model = settings.Model, input }, timeout.Token);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>(cancellationToken: timeout.Token);
        var vector = payload?.Embeddings?.FirstOrDefault();
        if (vector is null || vector.Length == 0) throw new InvalidOperationException("Embedding provider returned an empty vector.");
        return (vector, settings.Model);
    }

    private sealed record OllamaEmbeddingResponse(float[][] Embeddings);
}
