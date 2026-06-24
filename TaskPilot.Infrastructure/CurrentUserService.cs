using System.Security.Claims;
using TaskPilot.Application.Interfaces.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace TaskPilot.Infrastructure;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor)
    : ICurrentUserService
{
    public int UserId
    {
        get
        {
            var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
            {
                throw new InvalidOperationException("Current user id claim is missing or invalid.");
            }

            return userId;
        }
    }
}
