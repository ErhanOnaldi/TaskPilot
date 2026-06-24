using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPilot.Application.Features.WorkspaceMembers.Dtos;
using TaskPilot.Application.Features.WorkspaceMembers.Services;

namespace TaskPilot.API.Controllers;

[Route("api/workspaces/{workspaceId:int}/members")]
[ApiController]
[Authorize]
public class WorkspaceMembersController(IWorkspaceMemberService workspaceMemberService) : CustomBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetMembers(
        [FromRoute] int workspaceId,
        CancellationToken cancellationToken)
    {
        var result = await workspaceMemberService.GetMembersAsync(workspaceId, cancellationToken);
        return CreateActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> AddMember(
        [FromRoute] int workspaceId,
        [FromBody] AddWorkspaceMemberRequest request,
        CancellationToken cancellationToken)
    {
        var result = await workspaceMemberService.AddMemberAsync(workspaceId, request, cancellationToken);
        return CreateActionResult(result);
    }

    [HttpPut("{userId:int}/role")]
    public async Task<IActionResult> UpdateMemberRole(
        [FromRoute] int workspaceId,
        [FromRoute] int userId,
        [FromBody] UpdateWorkspaceMemberRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await workspaceMemberService.UpdateMemberRoleAsync(workspaceId, userId, request, cancellationToken);
        return CreateActionResult(result);
    }

    [HttpDelete("{userId:int}")]
    public async Task<IActionResult> RemoveMember(
        [FromRoute] int workspaceId,
        [FromRoute] int userId,
        CancellationToken cancellationToken)
    {
        var result = await workspaceMemberService.RemoveMemberAsync(workspaceId, userId, cancellationToken);
        return CreateActionResult(result);
    }
}
