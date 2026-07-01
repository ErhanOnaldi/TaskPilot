using AutoMapper;
using TaskPilot.Application.Features.Auth.Dtos;
using TaskPilot.Application.Features.Auth.RefreshTokens;
using TaskPilot.Application.Interfaces.Security;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.Auth.Factories;

public sealed class AuthResponseFactory(IMapper mapper) : IAuthResponseFactory
{
    public AuthResponse Create(User user, AuthToken authToken, CreatedRefreshToken refreshToken)
    {
        return new AuthResponse(
            authToken.AccessToken,
            authToken.ExpiresAtUtc,
            refreshToken.RawToken,
            refreshToken.Entity.ExpiresAtUtc,
            mapper.Map<AuthUserResponse>(user));
    }
}
