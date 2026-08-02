using System.Text.Json;
using A2A;
using TaskPilot.Application.Features.Interop;

namespace TaskPilot.IntegrationTests;

/// <summary>
/// Represents the small manager-agent client an application outside TaskPilot would use.
/// It uses only the official A2A SDK's card resolution, binding selection, and message APIs.
/// </summary>
internal sealed class ExternalA2aCopilotClient(IA2AClient client)
{
    public static async Task<(ExternalA2aCopilotClient Client, AgentCard Card)> DiscoverAsync(
        HttpClient httpClient,
        CancellationToken cancellationToken = default)
    {
        var resolver = new A2ACardResolver(httpClient.BaseAddress!, httpClient);
        var card = await resolver.GetAgentCardAsync(cancellationToken);
        return (new ExternalA2aCopilotClient(A2AClientFactory.Create(card, httpClient)), card);
    }

    public async Task<ExternalA2aRun> RunAsync(
        A2aMessageRequest request,
        string contextId,
        CancellationToken cancellationToken = default)
    {
        var response = await client.SendMessageAsync(
            new SendMessageRequest
            {
                Message = new Message
                {
                    MessageId = Guid.NewGuid().ToString("N"),
                    Role = Role.User,
                    ContextId = contextId,
                    Parts = [Part.FromText(JsonSerializer.Serialize(request))]
                }
            },
            cancellationToken);

        if (response.PayloadCase != SendMessageResponseCase.Message || response.Message is null)
            throw new InvalidOperationException("TaskPilot A2A endpoint did not return an agent message.");

        var text = string.Concat(response.Message.Parts.Select(part => part.Text));
        return new ExternalA2aRun(response.Message.ContextId, text);
    }
}

internal sealed record ExternalA2aRun(string? ContextId, string Text);
