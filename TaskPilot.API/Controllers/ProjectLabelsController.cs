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

    [HttpPut("{labelId:int}")]
    public async Task<IActionResult> UpdateLabel(
        [FromRoute] int projectId,
        [FromRoute] int labelId,
        [FromBody] UpdateLabelRequest request,
        CancellationToken cancellationToken)
    {
        return CreateActionResult(await labelService.UpdateLabelAsync(projectId, labelId, request, cancellationToken));
    }

    [HttpDelete("{labelId:int}")]
    public async Task<IActionResult> DeleteLabel(
        [FromRoute] int projectId,
        [FromRoute] int labelId,
        CancellationToken cancellationToken)
    {
        return CreateActionResult(await labelService.DeleteLabelAsync(projectId, labelId, cancellationToken));
    }
}
