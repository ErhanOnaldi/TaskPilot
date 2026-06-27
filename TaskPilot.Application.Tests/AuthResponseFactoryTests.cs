using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using TaskPilot.Application.Features.Auth.Services;
using TaskPilot.Application.Interfaces.Security;
using TaskPilot.Application.Mappings;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Tests;

public class AuthResponseFactoryTests
{
    [Fact]
    public void Create_maps_token_and_user_data_to_auth_response()
    {
        var factory = new AuthResponseFactory(CreateMapper());
        var user = new User { Id = 7, Email = "user@example.com" };
        var accessToken = new AuthToken("access-token", new DateTime(2026, 6, 26, 10, 15, 0, DateTimeKind.Utc));
        var refreshToken = new CreatedRefreshToken(
            "refresh-token",
            new RefreshToken
            {
                UserId = 7,
                TokenHash = "hash",
                ExpiresAtUtc = new DateTime(2026, 7, 3, 10, 0, 0, DateTimeKind.Utc)
            });

        var response = factory.Create(user, accessToken, refreshToken);

        Assert.Equal("access-token", response.AccessToken);
        Assert.Equal(accessToken.ExpiresAtUtc, response.AccessTokenExpiresAtUtc);
        Assert.Equal("refresh-token", response.RefreshToken);
        Assert.Equal(refreshToken.Entity.ExpiresAtUtc, response.RefreshTokenExpiresAtUtc);
        Assert.Equal(7, response.User.Id);
        Assert.Equal("user@example.com", response.User.Email);
    }

    private static IMapper CreateMapper()
    {
        return new MapperConfiguration(
            configuration => configuration.AddProfile<ApplicationMappingProfile>(),
            NullLoggerFactory.Instance).CreateMapper();
    }
}
