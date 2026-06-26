using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPilot.Application.Features.ProjectMembers.Dtos;
using TaskPilot.Application.Features.ProjectMembers.Services;

namespace TaskPilot.API.Controllers;

[Route("api/projects/{projectId:int}/members")]
[ApiController]
[Authorize]
public class ProjectMembersController(IProjectMemberService projectMemberService) : CustomBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetMembers([FromRoute] int projectId, CancellationToken cancellationToken)
    {
        return CreateActionResult(await projectMemberService.GetMembersAsync(projectId, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> AddMember([FromRoute] int projectId, [FromBody] AddProjectMemberRequest request, CancellationToken cancellationToken)
    {
        return CreateActionResult(await projectMemberService.AddMemberAsync(projectId, request, cancellationToken));
    }

    [HttpPut("{userId:int}/role")]
    public async Task<IActionResult> UpdateMemberRole([FromRoute] int projectId, [FromRoute] int userId, [FromBody] UpdateProjectMemberRoleRequest request, CancellationToken cancellationToken)
    {
        return CreateActionResult(await projectMemberService.UpdateMemberRoleAsync(projectId, userId, request, cancellationToken));
    }

    [HttpDelete("{userId:int}")]
    public async Task<IActionResult> RemoveMember([FromRoute] int projectId, [FromRoute] int userId, CancellationToken cancellationToken)
    {
        return CreateActionResult(await projectMemberService.RemoveMemberAsync(projectId, userId, cancellationToken));
    }
}
