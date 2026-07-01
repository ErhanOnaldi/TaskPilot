using Microsoft.Extensions.Options;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Persistence.Auth;
using TaskPilot.Application.Interfaces.Security;
using TaskPilot.Domain.Entities;
using TaskPilot.Domain.Options;

namespace TaskPilot.Application.Features.Auth.RefreshTokens;

public sealed class RefreshTokenService(
    IRefreshTokenRepository refreshTokenRepository,
    IRefreshTokenGenerator refreshTokenGenerator,
    IRefreshTokenHasher refreshTokenHasher,
    IDateTimeProvider dateTimeProvider,
    IOptions<JwtOptions> jwtOptions) : IRefreshTokenService
{
    public CreatedRefreshToken CreateForUser(User user)
    {
        var rawToken = refreshTokenGenerator.Generate();
        var now = dateTimeProvider.UtcNow;
        var tokenHash = refreshTokenHasher.Hash(rawToken);

        return new CreatedRefreshToken(
            rawToken,
            new RefreshToken
            {
                UserId = user.Id,
                TokenHash = tokenHash,
                CreatedAtUtc = now,
                ExpiresAtUtc = now.AddDays(jwtOptions.Value.RefreshTokenExpirationDays)
            });
    }

    public async ValueTask<CreatedRefreshToken> CreateAndAddForUserAsync(User user)
    {
        var refreshToken = CreateForUser(user);
        await refreshTokenRepository.AddAsync(refreshToken.Entity);
        return refreshToken;
    }

    public async Task<RefreshTokenRotationResult> RotateAsync(
        string rawRefreshToken,
        CancellationToken cancellationToken)
    {
        var tokenHash = refreshTokenHasher.Hash(rawRefreshToken);
        var existingToken = await refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (existingToken is null)
        {
            return RefreshTokenRotationResult.Fail(RefreshTokenRotationFailureReason.Invalid);
        }

        var now = dateTimeProvider.UtcNow;
        if (existingToken.IsRevoked)
        {
            await refreshTokenRepository.RevokeActiveTokensForUserAsync(existingToken.UserId, now, cancellationToken);
            return RefreshTokenRotationResult.Fail(RefreshTokenRotationFailureReason.Invalid, requiresSaveChanges: true);
        }

        if (existingToken.IsExpiredAt(now))
        {
            existingToken.RevokedAtUtc = now;
            return RefreshTokenRotationResult.Fail(RefreshTokenRotationFailureReason.Expired, requiresSaveChanges: true);
        }

        if (existingToken.User is null)
        {
            return RefreshTokenRotationResult.Fail(RefreshTokenRotationFailureReason.UserNotFound);
        }

        var newRefreshToken = CreateForUser(existingToken.User);
        existingToken.RevokedAtUtc = now;
        existingToken.ReplacedByTokenHash = newRefreshToken.Entity.TokenHash;

        await refreshTokenRepository.AddAsync(newRefreshToken.Entity);

        return RefreshTokenRotationResult.Success(existingToken.User, newRefreshToken);
    }

    public async Task<RefreshTokenLogoutResult> LogoutAsync(
        string rawRefreshToken,
        CancellationToken cancellationToken)
    {
        var tokenHash = refreshTokenHasher.Hash(rawRefreshToken);
        var existingToken = await refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (existingToken is null || existingToken.IsRevoked)
        {
            return new RefreshTokenLogoutResult(RequiresSaveChanges: false);
        }

        existingToken.RevokedAtUtc = dateTimeProvider.UtcNow;
        return new RefreshTokenLogoutResult(RequiresSaveChanges: true);
    }
}
