using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TaskPilot.Application.Features.Semantic.Contracts;

namespace TaskPilot.Infrastructure.Features.Semantic;

public sealed class SemanticEmbeddingOptions
{
    public string Provider { get; set; } = "Ollama";
    public string Endpoint { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "nomic-embed-text";
    public string? ApiKey { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxInputCharacters { get; set; } = 12_000;
    public int DuplicateTopK { get; set; } = 3;
    public float DuplicateSimilarityThreshold { get; set; } = .8f;
}

public static class SemanticInfrastructureExtensions
{
    public static IServiceCollection AddSemanticInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SemanticEmbeddingOptions>()
            .Bind(configuration.GetSection("Semantic"))
            .PostConfigure(options =>
            {
                options.Endpoint = configuration["Semantic:Endpoint"]
                                   ?? configuration["Ai:Endpoint"]
                                   ?? options.Endpoint;
                options.ApiKey = configuration["Semantic:ApiKey"]
                                 ?? configuration["Ai:ApiKey"]
                                 ?? options.ApiKey;
            })
            .Validate(
                options => options.Provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase) ||
                           options.Provider.Equals("OpenAICompatible", StringComparison.OrdinalIgnoreCase),
                "Semantic:Provider must be Ollama or OpenAICompatible.")
            .Validate(options => Uri.TryCreate(options.Endpoint, UriKind.Absolute, out _), "Semantic:Endpoint must be an absolute URI.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Model), "Semantic:Model must be configured.")
            .Validate(
                options => options.Provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase) ||
                           !string.IsNullOrWhiteSpace(options.ApiKey),
                "Semantic:ApiKey must be configured for OpenAI-compatible providers.")
            .ValidateOnStart();
        services.AddHttpClient<OllamaEmbeddingGenerator>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<SemanticEmbeddingOptions>>().Value;
            client.BaseAddress = new Uri(options.Endpoint, UriKind.Absolute);
        });
        services.AddHttpClient<OpenAiCompatibleEmbeddingGenerator>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<SemanticEmbeddingOptions>>().Value;
            client.BaseAddress = new Uri(options.Endpoint.TrimEnd('/') + "/", UriKind.Absolute);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        });
        services.AddScoped<IEmbeddingGenerator>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<SemanticEmbeddingOptions>>().Value;
            return options.Provider.Equals("OpenAICompatible", StringComparison.OrdinalIgnoreCase)
                ? provider.GetRequiredService<OpenAiCompatibleEmbeddingGenerator>()
                : provider.GetRequiredService<OllamaEmbeddingGenerator>();
        });
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

internal sealed class OpenAiCompatibleEmbeddingGenerator(
    HttpClient client,
    IOptions<SemanticEmbeddingOptions> options) : IEmbeddingGenerator
{
    public async Task<(float[] Vector, string Model)> GenerateAsync(string content, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var input = content.Length <= settings.MaxInputCharacters
            ? content
            : content[..settings.MaxInputCharacters];
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(settings.TimeoutSeconds, 1, 120)));
        using var response = await client.PostAsJsonAsync(
            "embeddings",
            new { model = settings.Model, input },
            timeout.Token);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<OpenAiEmbeddingResponse>(cancellationToken: timeout.Token);
        var vector = payload?.Data?.FirstOrDefault()?.Embedding;
        if (vector is null || vector.Length == 0)
            throw new InvalidOperationException("Embedding provider returned an empty vector.");
        return (vector, settings.Model);
    }

    private sealed record OpenAiEmbeddingResponse(OpenAiEmbeddingData[] Data);
    private sealed record OpenAiEmbeddingData(float[] Embedding);
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
