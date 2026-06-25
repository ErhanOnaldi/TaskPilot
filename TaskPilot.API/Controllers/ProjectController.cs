using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPilot.Application.Features.Project.Dtos;
using TaskPilot.Application.Features.Project.Services;

namespace TaskPilot.API.Controllers;

[Route("api/projects")]
[ApiController]
[Authorize]
public class ProjectsController(IProjectService projectService) : CustomBaseController
{
    [HttpGet("{projectId:int}")]
    public async Task<IActionResult> GetProject(
        [FromRoute] int projectId,
        CancellationToken cancellationToken)
    {
        var result = await projectService.GetProjectAsync(projectId, cancellationToken);
        return CreateActionResult(result);
    }

    [HttpPut("{projectId:int}")]
    public async Task<IActionResult> UpdateProject(
        [FromRoute] int projectId,
        [FromBody] UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var result = await projectService.UpdateProjectAsync(projectId, request, cancellationToken);
        return CreateActionResult(result);
    }

    [HttpPatch("{projectId:int}/archive")]
    public async Task<IActionResult> ArchiveProject(
        [FromRoute] int projectId,
        CancellationToken cancellationToken)
    {
        var result = await projectService.ArchiveProjectAsync(projectId, cancellationToken);
        return CreateActionResult(result);
    }
}
