using TaskPilot.Application.Features.Auth.Dtos;
using TaskPilot.Application.Features.Auth.RefreshTokens;
using TaskPilot.Application.Interfaces.Security;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.Auth.Factories;

public interface IAuthResponseFactory
{
    AuthResponse Create(User user, AuthToken authToken, CreatedRefreshToken refreshToken);
}
