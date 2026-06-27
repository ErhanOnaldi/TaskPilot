using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.Auth.Services;

public interface IRefreshTokenService
{
    CreatedRefreshToken CreateForUser(User user);
    ValueTask<CreatedRefreshToken> CreateAndAddForUserAsync(User user);
    Task<RefreshTokenRotationResult> RotateAsync(string rawRefreshToken, CancellationToken cancellationToken);
    Task<RefreshTokenLogoutResult> LogoutAsync(string rawRefreshToken, CancellationToken cancellationToken);
}
