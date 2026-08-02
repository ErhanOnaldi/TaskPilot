using System.Text.Json;

namespace TaskPilot.Application.Features.Interop;

public sealed record InteropToolDefinition(string Name, string Description, bool IsReadOnly);
public sealed record InteropToolCall(string Name, JsonElement Arguments);
public sealed record InteropToolResult(object? Data, string? Error = null);
public sealed record A2aMessageRequest(int WorkspaceId, int? ProjectId, string Message, int? SessionId = null, Guid? CausationId = null);
public interface IInteropToolCatalog { IReadOnlyList<InteropToolDefinition> List(); Task<InteropToolResult> InvokeAsync(InteropToolCall call, CancellationToken cancellationToken); }
public interface IA2aService { Task<InteropToolResult> SendAsync(A2aMessageRequest request, string? correlationId, CancellationToken cancellationToken); }
