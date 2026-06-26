using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPilot.Application.Features.Comments.Dtos;
using TaskPilot.Application.Features.Comments.Services;

namespace TaskPilot.API.Controllers;

[Route("api/comments")]
[ApiController]
[Authorize]
public class CommentsController(ICommentService commentService) : CustomBaseController
{
    [HttpPut("{commentId:int}")]
    public async Task<IActionResult> UpdateComment([FromRoute] int commentId, [FromBody] UpdateCommentRequest request, CancellationToken cancellationToken)
    {
        return CreateActionResult(await commentService.UpdateCommentAsync(commentId, request, cancellationToken));
    }

    [HttpDelete("{commentId:int}")]
    public async Task<IActionResult> DeleteComment([FromRoute] int commentId, CancellationToken cancellationToken)
    {
        return CreateActionResult(await commentService.DeleteCommentAsync(commentId, cancellationToken));
    }
}
