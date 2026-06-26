using System.Security.Claims;
using TaskPilot.Application.Interfaces.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace TaskPilot.Infrastructure;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor)
    : ICurrentUserService
{
    public int? UserId
    {
        get
        {
            var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public int GetRequiredUserId()
    {
        if (!IsAuthenticated || UserId is not { } userId)
        {
            throw new UnauthorizedAccessException("Current user id claim is missing or invalid.");
        }

        return userId;
    }
}
