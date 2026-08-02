using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TaskPilot.API.Features.Platform.RateLimiting;
using TaskPilot.Application.Features.Copilot;

namespace TaskPilot.API.Features.Copilot;

[ApiController, Authorize, Route("api")]
public sealed class CopilotController(ICopilotService copilot) : ControllerBase
{
    [HttpPost("projects/{projectId:int}/ai/chat"), EnableRateLimiting(TaskPilotRateLimitingExtensions.AiPolicy)]
    public Task<IActionResult> ChatProject(int projectId, [FromBody] CopilotRequest request, CancellationToken ct) => Result(copilot.ChatProjectAsync(projectId, request, Request.Headers["X-Correlation-ID"].FirstOrDefault(), ct));
    [HttpPost("workspaces/{workspaceId:int}/ai/chat"), EnableRateLimiting(TaskPilotRateLimitingExtensions.AiPolicy)]
    public Task<IActionResult> ChatWorkspace(int workspaceId, [FromBody] CopilotRequest request, CancellationToken ct) => Result(copilot.ChatWorkspaceAsync(workspaceId, request, Request.Headers["X-Correlation-ID"].FirstOrDefault(), ct));
    [HttpPost("projects/{projectId:int}/ai/chat/stream"), EnableRateLimiting(TaskPilotRateLimitingExtensions.AiPolicy)]
    public Task<IActionResult> StreamProject(int projectId, [FromBody] CopilotRequest request, CancellationToken ct) => Stream(copilot.StreamProjectAsync(projectId, request, Request.Headers["X-Correlation-ID"].FirstOrDefault(), ct), ct);
    [HttpPost("workspaces/{workspaceId:int}/ai/chat/stream"), EnableRateLimiting(TaskPilotRateLimitingExtensions.AiPolicy)]
    public Task<IActionResult> StreamWorkspace(int workspaceId, [FromBody] CopilotRequest request, CancellationToken ct) => Stream(copilot.StreamWorkspaceAsync(workspaceId, request, Request.Headers["X-Correlation-ID"].FirstOrDefault(), ct), ct);
    [HttpGet("ai/chat/{sessionId:int}")]
    public Task<IActionResult> Get(int sessionId, CancellationToken ct) => Result(copilot.GetSessionAsync(sessionId, ct));

    private async Task<IActionResult> Stream(Task<TaskPilot.Application.ServiceResult<CopilotStreamResponse>> startTask, CancellationToken ct)
    {
        var result = await startTask;
        if (result.IsFail || result.Data is null)
            return StatusCode((int)result.Status, result);

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache, no-transform";
        Response.Headers["X-Accel-Buffering"] = "no";
        await WriteEventAsync("session", new { sessionId = result.Data.SessionId }, ct);
        try
        {
            await foreach (var update in result.Data.ReadUpdatesAsync(ct).WithCancellation(ct))
            {
                if (update.Kind == CopilotStreamUpdateKind.Token)
                    await WriteEventAsync("token", new { token = update.Token }, ct);
                else
                    await WriteEventAsync("completed", update.Session, ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // The browser disconnected; the Application iterator deliberately does not persist a partial answer.
        }
        catch (Exception)
        {
            if (!ct.IsCancellationRequested)
                await WriteEventAsync("error", new { error = "Copilot streaming could not be completed." }, ct);
        }

        return new EmptyResult();
    }

    private async Task WriteEventAsync(string eventName, object? payload, CancellationToken ct)
    {
        await Response.WriteAsync($"event: {eventName}\ndata: {JsonSerializer.Serialize(payload)}\n\n", ct);
        await Response.Body.FlushAsync(ct);
    }

    private async Task<IActionResult> Result(Task<TaskPilot.Application.ServiceResult<CopilotSessionResponse>> task) { var result = await task; return StatusCode((int)result.Status, result); }
}
