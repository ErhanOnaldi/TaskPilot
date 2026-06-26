using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPilot.Application.Features.Labels.Services;

namespace TaskPilot.API.Controllers;

[Route("api/tasks/{taskId:int}/labels/{labelId:int}")]
[ApiController]
[Authorize]
public class TaskLabelsController(ILabelService labelService) : CustomBaseController
{
    [HttpPost]
    public async Task<IActionResult> AddLabel([FromRoute] int taskId, [FromRoute] int labelId, CancellationToken cancellationToken)
    {
        return CreateActionResult(await labelService.AddLabelToTaskAsync(taskId, labelId, cancellationToken));
    }

    [HttpDelete]
    public async Task<IActionResult> RemoveLabel([FromRoute] int taskId, [FromRoute] int labelId, CancellationToken cancellationToken)
    {
        return CreateActionResult(await labelService.RemoveLabelFromTaskAsync(taskId, labelId, cancellationToken));
    }
}
