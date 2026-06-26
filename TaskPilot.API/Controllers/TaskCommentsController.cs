using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPilot.Application.Features.Comments.Dtos;
using TaskPilot.Application.Features.Comments.Services;

namespace TaskPilot.API.Controllers;

[Route("api/tasks/{taskId:int}/comments")]
[ApiController]
[Authorize]
public class TaskCommentsController(ICommentService commentService) : CustomBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetComments([FromRoute] int taskId, CancellationToken cancellationToken)
    {
        return CreateActionResult(await commentService.GetCommentsAsync(taskId, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> CreateComment([FromRoute] int taskId, [FromBody] CreateCommentRequest request, CancellationToken cancellationToken)
    {
        return CreateActionResult(await commentService.CreateCommentAsync(taskId, request, cancellationToken));
    }
}
