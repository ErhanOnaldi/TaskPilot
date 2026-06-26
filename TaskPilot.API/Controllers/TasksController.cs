using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPilot.Application.Features.Tasks.Dtos;
using TaskPilot.Application.Features.Tasks.Services;

namespace TaskPilot.API.Controllers;

[Route("api/tasks")]
[ApiController]
[Authorize]
public class TasksController(ITaskService taskService) : CustomBaseController
{
    [HttpGet("{taskId:int}")]
    public async Task<IActionResult> GetTask([FromRoute] int taskId, CancellationToken cancellationToken)
    {
        return CreateActionResult(await taskService.GetTaskAsync(taskId, cancellationToken));
    }

    [HttpPut("{taskId:int}")]
    public async Task<IActionResult> UpdateTask([FromRoute] int taskId, [FromBody] UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        return CreateActionResult(await taskService.UpdateTaskAsync(taskId, request, cancellationToken));
    }

    [HttpDelete("{taskId:int}")]
    public async Task<IActionResult> DeleteTask([FromRoute] int taskId, CancellationToken cancellationToken)
    {
        return CreateActionResult(await taskService.DeleteTaskAsync(taskId, cancellationToken));
    }

    [HttpPatch("{taskId:int}/status")]
    public async Task<IActionResult> UpdateStatus([FromRoute] int taskId, [FromBody] UpdateTaskStatusRequest request, CancellationToken cancellationToken)
    {
        return CreateActionResult(await taskService.UpdateStatusAsync(taskId, request, cancellationToken));
    }

    [HttpPatch("{taskId:int}/assign")]
    public async Task<IActionResult> AssignTask([FromRoute] int taskId, [FromBody] AssignTaskRequest request, CancellationToken cancellationToken)
    {
        return CreateActionResult(await taskService.AssignTaskAsync(taskId, request, cancellationToken));
    }
}
