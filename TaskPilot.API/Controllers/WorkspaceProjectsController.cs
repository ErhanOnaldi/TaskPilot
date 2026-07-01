using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPilot.Application.Features.Project.Dtos;
using TaskPilot.Application.Features.Project.Services;

namespace TaskPilot.API.Controllers;

[Route("api/workspaces/{workspaceId:int}/projects")]
[ApiController]
[Authorize]
public class WorkspaceProjectsController(IProjectService projectService) : CustomBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetProjects(
        [FromRoute] int workspaceId,
        [FromQuery] ProjectQueryParameters query,
        CancellationToken cancellationToken)
    {
        var result = await projectService.GetProjectsAsync(workspaceId, query, cancellationToken);
        return CreateActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProject(
        [FromRoute] int workspaceId,
        [FromBody] CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var result = await projectService.CreateProjectAsync(workspaceId, request, cancellationToken);
        return CreateActionResult(result);
    }
}
