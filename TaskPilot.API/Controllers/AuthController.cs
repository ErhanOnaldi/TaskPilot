using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TaskPilot.API.Features.Platform.RateLimiting;
using TaskPilot.Application.Features.Auth.Dtos;
using TaskPilot.Application.Features.Auth.Services;

namespace TaskPilot.API.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController(IAuthService authService): CustomBaseController
{
    [HttpPost("login")]
    [EnableRateLimiting(TaskPilotRateLimitingExtensions.AuthPolicy)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        return CreateActionResult(result);
    }

    [HttpPost("register")] 
    [EnableRateLimiting(TaskPilotRateLimitingExtensions.AuthPolicy)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(request, cancellationToken);
        return CreateActionResult(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var result = await authService.GetMeAsync(cancellationToken);
        return CreateActionResult(result);
    }

    [HttpPost("refresh-token")]
    [EnableRateLimiting(TaskPilotRateLimitingExtensions.AuthPolicy)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.RefreshTokenAsync(request, cancellationToken);
        return CreateActionResult(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LogoutAsync(request, cancellationToken);
        return CreateActionResult(result);
    }
    
}
