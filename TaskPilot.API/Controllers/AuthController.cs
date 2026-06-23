using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPilot.Application.Features.Auth.Dtos;
using TaskPilot.Application.Features.Auth.Services;

namespace TaskPilot.API.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController(IAuthService authService): CustomBaseController
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        return CreateActionResult(result);
    }

    [HttpPost("register")] 
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(request, cancellationToken);
        return CreateActionResult(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var result = await authService.GetMeAsync(userId, cancellationToken);
        return CreateActionResult(result);
    }
}
