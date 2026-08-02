using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using TaskPilot.Application.Features.Ai;
using TaskPilot.Application.Features.Copilot;
using TaskPilot.Application.Features.Semantic.Contracts;
using TaskPilot.Domain.AI.Copilot;
using TaskPilot.Infrastructure.Messaging;

namespace TaskPilot.IntegrationTests;

/// <summary>
/// Boots the production composition root with disposable infrastructure connection overrides.
/// No external endpoint or developer configuration is read by the integration host.
/// </summary>
public sealed class TaskPilotWebApplicationFactory(IReadOnlyDictionary<string, string?> configurationOverrides)
    : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Minimal hosting evaluates Program before ConfigureWebHost's configuration callback.
        // Temporarily project the dynamic Testcontainers values into the environment so the
        // production composition root sees them while it is being assembled.
        var previousValues = configurationOverrides.ToDictionary(
            pair => pair.Key.Replace(":", "__", StringComparison.Ordinal),
            pair => Environment.GetEnvironmentVariable(pair.Key.Replace(":", "__", StringComparison.Ordinal)));

        try
        {
            foreach (var (key, value) in configurationOverrides)
            {
                Environment.SetEnvironmentVariable(key.Replace(":", "__", StringComparison.Ordinal), value);
            }

            return base.CreateHost(builder);
        }
        finally
        {
            foreach (var (key, value) in previousValues)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Integration");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(configurationOverrides));
        builder.ConfigureServices(services =>
        {
            var outboxWorkers = services
                .Where(descriptor => descriptor.ServiceType == typeof(IHostedService) &&
                                     descriptor.ImplementationType == typeof(OutboxDispatcherWorker))
                .ToArray();
            foreach (var outboxWorker in outboxWorkers)
            {
                services.Remove(outboxWorker);
            }

            services.RemoveAll<IAiStructuredGenerator>();
            services.RemoveAll<IAiChatGenerator>();
            services.RemoveAll<IEmbeddingGenerator>();
            services.RemoveAll<ICopilotContextRetriever>();
            services.AddSingleton<NoExternalAiGenerator>();
            services.AddSingleton<IAiStructuredGenerator>(provider => provider.GetRequiredService<NoExternalAiGenerator>());
            services.AddSingleton<IAiChatGenerator>(provider => provider.GetRequiredService<NoExternalAiGenerator>());
            services.AddSingleton<IEmbeddingGenerator, NoExternalEmbeddingGenerator>();
            services.AddSingleton<ICopilotContextRetriever, ScopeEchoCopilotContextRetriever>();
        });
    }

    private sealed class NoExternalAiGenerator : IAiStructuredGenerator, IAiChatGenerator
    {
        public Task<AiGenerationResult> GenerateTaskSuggestionAsync(
            string input,
            TaskPilot.Domain.AI.AgentExecutionScope scope,
            CancellationToken cancellationToken) =>
            Task.FromException<AiGenerationResult>(new InvalidOperationException("External AI is disabled in integration tests."));

        public Task<string> CompleteAsync(
            IReadOnlyList<AiChatMessage> messages,
            TaskPilot.Domain.AI.AgentExecutionScope scope,
            CancellationToken cancellationToken)
        {
            var history = string.Join("|", messages
                .Where(message => string.Equals(message.Role, "user", StringComparison.Ordinal))
                .Select(message => message.Content));
            var authorizedContext = messages
                .Single(message => message.Content.StartsWith("Authorized Task ", StringComparison.Ordinal))
                .Content;
            return Task.FromResult($"history={history}; {authorizedContext}");
        }

        public async IAsyncEnumerable<string> StreamAsync(
            IReadOnlyList<AiChatMessage> messages,
            TaskPilot.Domain.AI.AgentExecutionScope scope,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return await CompleteAsync(messages, scope, cancellationToken);
        }
    }

    private sealed class NoExternalEmbeddingGenerator : IEmbeddingGenerator
    {
        public Task<(float[] Vector, string Model)> GenerateAsync(string content, CancellationToken cancellationToken) =>
            Task.FromException<(float[] Vector, string Model)>(
                new InvalidOperationException("External embeddings are disabled in integration tests."));
    }

    private sealed class ScopeEchoCopilotContextRetriever : ICopilotContextRetriever
    {
        public Task<IReadOnlyList<CopilotContextItem>> RetrieveAsync(
            CopilotAuthorizedScope scope,
            string query,
            CancellationToken cancellationToken)
        {
            var sourceId = scope.ExecutionScope.ProjectId ?? scope.ExecutionScope.WorkspaceId;
            IReadOnlyList<CopilotContextItem> context =
            [
                new(
                    new CopilotCitation(CopilotCitationType.Task, sourceId),
                    $"scope=user:{scope.ExecutionScope.UserId};workspace:{scope.ExecutionScope.WorkspaceId};project:{scope.ExecutionScope.ProjectId}")
            ];
            return Task.FromResult(context);
        }
    }
}
