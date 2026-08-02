using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TaskPilot.API.Features.Platform.RateLimiting;
using TaskPilot.Application.Features.Ai;

namespace TaskPilot.API.Features.Ai;

[ApiController]
[Authorize]
[Route("api")]
public sealed class AiSuggestionsController(IAiSuggestionService suggestions) : ControllerBase
{
    [HttpPost("projects/{projectId:int}/ai/task-suggestions")]
    [EnableRateLimiting(TaskPilotRateLimitingExtensions.AiPolicy)]
    public async Task<IActionResult> Create(int projectId, [FromBody] CreateTaskSuggestionRequest request, CancellationToken cancellationToken)
    {
        var correlationId = Guid.TryParse(Request.Headers["X-Correlation-ID"].FirstOrDefault(), out var parsedCorrelationId) ? parsedCorrelationId : (Guid?)null;
        var result = await suggestions.CreateAsync(projectId, request, correlationId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("ai/suggestions/{id:int}")]
    [EnableRateLimiting(TaskPilotRateLimitingExtensions.AiPolicy)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken) =>
        ToActionResult(await suggestions.GetAsync(id, cancellationToken));

    [HttpPost("ai/suggestions/{id:int}/apply")]
    [EnableRateLimiting(TaskPilotRateLimitingExtensions.AiPolicy)]
    public async Task<IActionResult> Apply(int id, CancellationToken cancellationToken) =>
        ToActionResult(await suggestions.ApplyAsync(id, cancellationToken));

    private IActionResult ToActionResult(TaskPilot.Application.ServiceResult<AiSuggestionResponse> result) =>
        StatusCode((int)result.Status, result);
}
