using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPilot.Application.Features.Tasks.Dtos;
using TaskPilot.Application.Features.Tasks.Services;

namespace TaskPilot.API.Controllers;

[Route("api/projects/{projectId:int}/tasks")]
[ApiController]
[Authorize]
public class ProjectTasksController(ITaskService taskService) : CustomBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetTasks([FromRoute] int projectId, CancellationToken cancellationToken)
    {
        return CreateActionResult(await taskService.GetTasksAsync(projectId, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> CreateTask([FromRoute] int projectId, [FromBody] CreateTaskRequest request, CancellationToken cancellationToken)
    {
        return CreateActionResult(await taskService.CreateTaskAsync(projectId, request, cancellationToken));
    }
}
