using TaskPilot.Application.Features.Copilot;

namespace TaskPilot.Application.Features.Interop;

public sealed class A2aService(ICopilotService copilot) : IA2aService
{
    public async Task<InteropToolResult> SendAsync(A2aMessageRequest request, string? correlationId, CancellationToken ct)
    {
        var message = new CopilotRequest(request.Message, request.SessionId, request.CausationId);
        var result = request.ProjectId.HasValue ? await copilot.ChatProjectAsync(request.ProjectId.Value, message, correlationId, ct) : await copilot.ChatWorkspaceAsync(request.WorkspaceId, message, correlationId, ct);
        return new InteropToolResult(result);
    }
}
