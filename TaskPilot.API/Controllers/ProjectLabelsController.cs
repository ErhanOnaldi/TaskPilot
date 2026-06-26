using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPilot.Application.Features.Labels.Dtos;
using TaskPilot.Application.Features.Labels.Services;

namespace TaskPilot.API.Controllers;

[Route("api/projects/{projectId:int}/labels")]
[ApiController]
[Authorize]
public class ProjectLabelsController(ILabelService labelService) : CustomBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetLabels([FromRoute] int projectId, CancellationToken cancellationToken)
    {
        return CreateActionResult(await labelService.GetLabelsAsync(projectId, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> CreateLabel([FromRoute] int projectId, [FromBody] CreateLabelRequest request, CancellationToken cancellationToken)
    {
        return CreateActionResult(await labelService.CreateLabelAsync(projectId, request, cancellationToken));
    }
}
