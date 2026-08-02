using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using TaskPilot.Application.Features.Copilot;
using TaskPilot.Application.Features.Interop;

namespace TaskPilot.Infrastructure.Features.Interop;

/// <summary>
/// Agent Framework adapter over the scope-bound TaskPilot Copilot application service.
/// Each run resolves a fresh request scope and the application service reauthorizes the caller.
/// </summary>
public sealed class TaskPilotCopilotAgent(IServiceScopeFactory scopeFactory) : AIAgent
{
    private const string CopilotSessionIdKey = "taskpilot.copilot-session-id";

    public override string? Name => "taskpilot-copilot";
    public override string? Description => "Authorized TaskPilot project and workspace knowledge copilot.";
    protected override string? IdCore => "taskpilot-copilot";

    protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken) =>
        ValueTask.FromResult<AgentSession>(new TaskPilotAgentSession());

    protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
        AgentSession session,
        JsonSerializerOptions? jsonSerializerOptions,
        CancellationToken cancellationToken) =>
        ValueTask.FromResult(session.StateBag.Serialize());

    protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
        JsonElement serializedState,
        JsonSerializerOptions? jsonSerializerOptions,
        CancellationToken cancellationToken) =>
        ValueTask.FromResult<AgentSession>(new TaskPilotAgentSession(
            AgentSessionStateBag.Deserialize(serializedState)));

    protected override async Task<AgentResponse> RunCoreAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session,
        AgentRunOptions? options,
        CancellationToken cancellationToken)
    {
        session ??= await CreateSessionAsync(cancellationToken);
        var parsed = ParseRequest(messages, session);
        var request = new CopilotRequest(parsed.Message, parsed.SessionId, parsed.CausationId);
        await using var scope = scopeFactory.CreateAsyncScope();
        var copilot = scope.ServiceProvider.GetRequiredService<ICopilotService>();
        var result = parsed.ProjectId.HasValue
            ? await copilot.ChatProjectAsync(parsed.ProjectId.Value, request, null, cancellationToken)
            : await copilot.ChatWorkspaceAsync(parsed.WorkspaceId, request, null, cancellationToken);
        if (result.IsFail || result.Data is null)
            throw new InvalidOperationException(string.Join("; ", result.ErrorMessages ?? ["Copilot request failed."]));

        session.StateBag.SetValue(CopilotSessionIdKey, result.Data.Id.ToString());
        var responseText = result.Data.Messages.LastOrDefault()?.Content ?? string.Empty;
        return new AgentResponse(new ChatMessage(ChatRole.Assistant, responseText))
        {
            AgentId = Id
        };
    }

    protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session,
        AgentRunOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var response = await RunCoreAsync(messages, session, options, cancellationToken);
        foreach (var update in response.ToAgentResponseUpdates())
            yield return update;
    }

    private static A2aMessageRequest ParseRequest(IEnumerable<ChatMessage> messages, AgentSession session)
    {
        var content = messages.LastOrDefault(message => message.Role == ChatRole.User)?.Text;
        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("A2A request must contain a user message.");

        A2aMessageRequest wireRequest;
        try
        {
            wireRequest = JsonSerializer.Deserialize<A2aMessageRequest>(content,
                new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new JsonException();
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                "A2A message text must be JSON with workspaceId, optional projectId, and message.",
                exception);
        }

        var storedSessionValue = session.StateBag.GetValue<string>(CopilotSessionIdKey);
        var storedSessionId = int.TryParse(storedSessionValue, out var parsedSessionId)
            ? parsedSessionId
            : (int?)null;
        return new A2aMessageRequest(
            wireRequest.WorkspaceId,
            wireRequest.ProjectId,
            wireRequest.Message,
            wireRequest.SessionId ?? storedSessionId,
            wireRequest.CausationId);
    }

    private sealed class TaskPilotAgentSession : AgentSession
    {
        public TaskPilotAgentSession()
        {
        }

        public TaskPilotAgentSession(AgentSessionStateBag stateBag) : base(stateBag)
        {
        }
    }
}
