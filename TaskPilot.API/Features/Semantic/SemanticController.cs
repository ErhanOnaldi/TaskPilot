using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPilot.API.Controllers;
using TaskPilot.Application.Features.Semantic.Dtos;
using TaskPilot.Application.Features.Semantic.Services;

namespace TaskPilot.API.Features.Semantic;

[ApiController]
[Authorize]
[Route("api")]
public sealed class SemanticController(ISemanticSearchService searchService) : CustomBaseController
{
    [HttpGet("projects/{projectId:int}/tasks/semantic-search")]
    public async Task<IActionResult> SearchProjectTasks(
        int projectId,
        [FromQuery(Name = "q")] string query,
        [FromQuery] int limit,
        CancellationToken cancellationToken) =>
        CreateActionResult(await searchService.SearchProjectAsync(
            projectId,
            new SemanticSearchRequest(query, SemanticSearchMode.Semantic, limit <= 0 ? 20 : limit),
            cancellationToken));
}
