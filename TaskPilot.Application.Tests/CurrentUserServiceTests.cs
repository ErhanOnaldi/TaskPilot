using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TaskPilot.Infrastructure;

namespace TaskPilot.Application.Tests;

public class CurrentUserServiceTests
{
    [Fact]
    public void UserId_returns_null_when_name_identifier_claim_is_missing()
    {
        var service = CreateService(new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "Test")));

        Assert.Null(service.UserId);
    }

    [Fact]
    public void UserId_returns_null_when_name_identifier_claim_is_invalid()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "not-an-int")
        ], "Test"));
        var service = CreateService(principal);

        Assert.Null(service.UserId);
    }

    [Fact]
    public void IsAuthenticated_returns_true_when_identity_is_authenticated()
    {
        var service = CreateService(new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "Test")));

        Assert.True(service.IsAuthenticated);
    }

    [Fact]
    public void GetRequiredUserId_returns_user_id_when_claim_is_valid()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "42")
        ], "Test"));
        var service = CreateService(principal);

        Assert.Equal(42, service.GetRequiredUserId());
    }

    [Fact]
    public void GetRequiredUserId_throws_unauthorized_when_user_id_is_missing()
    {
        var service = CreateService(new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "Test")));

        var exception = Assert.Throws<UnauthorizedAccessException>(() => service.GetRequiredUserId());
        Assert.Equal("Current user id claim is missing or invalid.", exception.Message);
    }

    private static CurrentUserService CreateService(ClaimsPrincipal principal)
    {
        var httpContext = new DefaultHttpContext
        {
            User = principal
        };

        return new CurrentUserService(new HttpContextAccessor
        {
            HttpContext = httpContext
        });
    }
}
