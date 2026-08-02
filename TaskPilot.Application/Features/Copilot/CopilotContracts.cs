using TaskPilot.Domain.AI;
using TaskPilot.Domain.AI.Copilot;

namespace TaskPilot.Application.Features.Copilot;

public sealed record CopilotRequest(string Message, int? SessionId = null, Guid? CausationId = null);
public sealed record CopilotMessageResponse(int Id, string Role, string Content, IReadOnlyList<CopilotCitation> Citations, DateTime CreatedAtUtc);
public sealed record CopilotSessionResponse(int Id, int WorkspaceId, int? ProjectId, IReadOnlyList<CopilotMessageResponse> Messages);
public sealed record CopilotAuthorizedScope(AgentExecutionScope ExecutionScope, IReadOnlySet<int> AuthorizedProjectIds, bool IncludeWorkspaceWideDocuments);
public enum CopilotStreamUpdateKind { Token, Completed }
public sealed record CopilotStreamUpdate(CopilotStreamUpdateKind Kind, string? Token = null, CopilotSessionResponse? Session = null);

public sealed class CopilotStreamResponse
{
    private readonly IAsyncEnumerable<CopilotStreamUpdate> _updates;
    private int _consumed;

    public CopilotStreamResponse(int sessionId, IAsyncEnumerable<CopilotStreamUpdate> updates)
    {
        SessionId = sessionId;
        _updates = updates;
    }

    public int SessionId { get; }

    public IAsyncEnumerable<CopilotStreamUpdate> ReadUpdatesAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _consumed, 1) != 0)
            throw new InvalidOperationException("A Copilot stream can only be consumed once.");
        return ConsumeAsync(cancellationToken);
    }

    private async IAsyncEnumerable<CopilotStreamUpdate> ConsumeAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var update in _updates.WithCancellation(cancellationToken))
            yield return update;
    }
}

public interface ICopilotRepository
{
    Task<CopilotChatSession?> GetSessionAsync(int id, CancellationToken cancellationToken);
    ValueTask AddSessionAsync(CopilotChatSession session, CancellationToken cancellationToken);
    ValueTask AddMessageAsync(CopilotChatMessage message, CancellationToken cancellationToken);
}
public interface ICopilotAuthorizationPort
{
    Task<CopilotAuthorizedScope?> AuthorizeProjectAsync(int projectId, string? correlationId, Guid? causationId, CancellationToken cancellationToken);
    Task<CopilotAuthorizedScope?> AuthorizeWorkspaceAsync(int workspaceId, string? correlationId, Guid? causationId, CancellationToken cancellationToken);
}
public interface ICopilotContextRetriever
{
    Task<IReadOnlyList<CopilotContextItem>> RetrieveAsync(CopilotAuthorizedScope scope, string query, CancellationToken cancellationToken);
}
public interface ICopilotService
{
    Task<ServiceResult<CopilotSessionResponse>> ChatProjectAsync(int projectId, CopilotRequest request, string? correlationId, CancellationToken cancellationToken);
    Task<ServiceResult<CopilotSessionResponse>> ChatWorkspaceAsync(int workspaceId, CopilotRequest request, string? correlationId, CancellationToken cancellationToken);
    Task<ServiceResult<CopilotStreamResponse>> StreamProjectAsync(int projectId, CopilotRequest request, string? correlationId, CancellationToken cancellationToken);
    Task<ServiceResult<CopilotStreamResponse>> StreamWorkspaceAsync(int workspaceId, CopilotRequest request, string? correlationId, CancellationToken cancellationToken);
    Task<ServiceResult<CopilotSessionResponse>> GetSessionAsync(int sessionId, CancellationToken cancellationToken);
}
