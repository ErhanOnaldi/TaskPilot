using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TaskPilot.Application.Features.Ai;
using TaskPilot.Domain.AI;
using TaskPilot.Infrastructure.Features.Ai;

namespace TaskPilot.Application.Tests;

public sealed class AiChatClientGeneratorTests
{
    [Fact]
    public async Task Function_invocation_pipeline_executes_the_scope_bound_project_tool()
    {
        var inner = new InspectingChatClient(requestLabelTool: true);
        using var pipeline = new ChatClientBuilder(inner)
            .UseFunctionInvocation(configure: functionClient =>
            {
                functionClient.MaximumIterationsPerRequest = 4;
                functionClient.MaximumConsecutiveErrorsPerRequest = 0;
            })
            .Build();
        var generator = CreateGenerator(pipeline);
        var context = new ProjectAiContext(
            new HashSet<string>(["authorized-only"], StringComparer.OrdinalIgnoreCase),
            ["Ada"],
            ["Task 1"]);

        var result = await generator.GenerateTaskSuggestionAsync(
            "Prepare release",
            new AgentExecutionScope(7, 5, 12, Guid.NewGuid(), Guid.NewGuid()),
            context,
            CancellationToken.None);

        Assert.Equal(2, inner.ResponseCount);
        Assert.Contains("authorized-only", inner.FunctionResult!);
        Assert.Equal(["authorized-only"], result.Suggestion.Labels);
    }

    [Fact]
    public async Task Structured_generation_exposes_only_authorized_context_tools_and_constrains_output()
    {
        var client = new InspectingChatClient();
        var generator = CreateGenerator(client);
        var context = new ProjectAiContext(
            new HashSet<string>(["release"], StringComparer.OrdinalIgnoreCase),
            ["Ada (Owner)"],
            ["Ship v2 (High)"]);

        var result = await generator.GenerateTaskSuggestionAsync(
            "Prepare the release",
            new AgentExecutionScope(7, 5, 12, Guid.NewGuid(), Guid.NewGuid()),
            context,
            CancellationToken.None);

        Assert.Equal(["release"], result.Suggestion.Labels);
        Assert.Equal(11, result.Metadata.InputTokens);
        Assert.Equal(7, result.Metadata.OutputTokens);
        Assert.NotNull(client.Options!.ResponseFormat);
        Assert.Equal(
            ["get_open_tasks", "get_project_labels", "get_project_members"],
            client.ToolResults.Keys.OrderBy(name => name));
        Assert.Contains("release", client.ToolResults["get_project_labels"]);
        Assert.Contains("Ada (Owner)", client.ToolResults["get_project_members"]);
        Assert.Contains("Ship v2 (High)", client.ToolResults["get_open_tasks"]);
    }

    [Fact]
    public async Task Chat_stream_forwards_real_provider_chunks_without_buffering()
    {
        var client = new InspectingChatClient();
        var generator = CreateGenerator(client);
        var chunks = new List<string>();

        await foreach (var chunk in generator.StreamAsync(
                           [new AiChatMessage("user", "question")],
                           new AgentExecutionScope(7, 5, 12, Guid.NewGuid(), Guid.NewGuid()),
                           CancellationToken.None))
            chunks.Add(chunk);

        Assert.Equal(["first ", "second"], chunks);
    }

    private static MicrosoftAiGenerator CreateGenerator(IChatClient client) =>
        new(
            client,
            Options.Create(new AiProviderOptions
            {
                Provider = "OpenAICompatible",
                Endpoint = "https://example.invalid/v1",
                Model = "test-model",
                TimeoutSeconds = 5,
                MaxOutputTokens = 128,
                MaxRetries = 0,
                MaxToolInvocations = 3
            }),
            new AiInputGuard(),
            new AiOutputGuard(),
            NullLogger<MicrosoftAiGenerator>.Instance);

    private sealed class InspectingChatClient(bool requestLabelTool = false) : IChatClient
    {
        public ChatOptions? Options { get; private set; }
        public Dictionary<string, string> ToolResults { get; } = [];
        public int ResponseCount { get; private set; }
        public string? FunctionResult { get; private set; }

        public async Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Options = options;
            ResponseCount++;
            if (requestLabelTool && ResponseCount == 1)
            {
                return new ChatResponse(new ChatMessage(
                    ChatRole.Assistant,
                    [new FunctionCallContent("labels-1", "get_project_labels", new Dictionary<string, object?>())]));
            }

            if (requestLabelTool)
            {
                var result = messages
                    .SelectMany(message => message.Contents)
                    .OfType<FunctionResultContent>()
                    .Single();
                FunctionResult = JsonSerializer.Serialize(result.Result);
            }

            foreach (var function in options?.Tools?.OfType<AIFunction>() ?? [])
            {
                if (requestLabelTool)
                    break;
                var value = await function.InvokeAsync(new AIFunctionArguments(), cancellationToken);
                ToolResults[function.Name] = value is JsonElement json ? json.GetRawText() : JsonSerializer.Serialize(value);
            }

            return new ChatResponse(new ChatMessage(
                ChatRole.Assistant,
                requestLabelTool
                    ? "{\"title\":\"Release\",\"priority\":\"High\",\"labels\":[\"authorized-only\"],\"subtasks\":[\"Draft\"],\"dueDate\":null}"
                    : "{\"title\":\"Release\",\"priority\":\"High\",\"labels\":[\"RELEASE\",\"invented\"],\"subtasks\":[\"Draft\"],\"dueDate\":null}"))
            {
                ModelId = "test-model",
                Usage = new UsageDetails { InputTokenCount = 11, OutputTokenCount = 7 }
            };
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Options = options;
            yield return new ChatResponseUpdate(ChatRole.Assistant, "first ");
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return new ChatResponseUpdate(ChatRole.Assistant, "second");
        }

        public object? GetService(Type serviceType, object? serviceKey = null) =>
            serviceType.IsInstanceOfType(this) ? this : null;

        public void Dispose() { }
    }
}
