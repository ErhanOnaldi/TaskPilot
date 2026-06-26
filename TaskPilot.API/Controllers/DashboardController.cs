using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPilot.Application.Features.Dashboard.Services;

namespace TaskPilot.API.Controllers;

[Route("api/projects/{projectId:int}/dashboard")]
[ApiController]
[Authorize]
public class DashboardController(IDashboardService dashboardService) : CustomBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetProjectDashboard([FromRoute] int projectId, CancellationToken cancellationToken)
    {
        return CreateActionResult(await dashboardService.GetProjectDashboardAsync(projectId, cancellationToken));
    }
}
