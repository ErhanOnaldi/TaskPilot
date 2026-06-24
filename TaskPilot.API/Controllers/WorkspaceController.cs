using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPilot.Application.Features.Workspace.Dtos;
using TaskPilot.Application.Features.Workspace.Services;

namespace TaskPilot.API.Controllers;

[Route("api/workspaces")]
[ApiController]
[Authorize]
public class WorkspaceController(IWorkspaceService workspaceService) : CustomBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return CreateActionResult(await workspaceService.GetWorkSpacesAsync(cancellationToken));
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById([FromRoute]int id, CancellationToken cancellationToken)
    {
        return CreateActionResult(await workspaceService.GetWorkspaceAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody]CreateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        return CreateActionResult(await workspaceService.CreateWorkspaceAsync(request, cancellationToken));
    }
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody]UpdateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        return CreateActionResult(await workspaceService.UpdateWorkspaceAsync(id, request, cancellationToken));
    }
    [HttpPatch("{id}/archive")]
    public async Task<IActionResult> Archive(int id, CancellationToken cancellationToken)
    {
        return CreateActionResult(await workspaceService.ArchiveWorkspaceAsync(id, cancellationToken));
    }
    
}
