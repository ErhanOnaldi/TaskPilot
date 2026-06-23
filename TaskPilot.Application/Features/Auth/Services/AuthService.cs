using System.Net;
using Microsoft.Extensions.Options;
using TaskPilot.Application.Features.Auth.Dtos;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Application.Interfaces.Persistence.Auth;
using TaskPilot.Application.Interfaces.Persistence.User;
using TaskPilot.Application.Interfaces.Security;
using TaskPilot.Domain.Entities;
using TaskPilot.Domain.Options;

namespace TaskPilot.Application.Features.Auth.Services;

public class AuthService(
    IUserRepository repository,
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IRefreshTokenGenerator refreshTokenGenerator,
    IRefreshTokenHasher refreshTokenHasher,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    public async Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var isExist = await repository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (isExist is not null)
        {
            return ServiceResult<AuthResponse>.Fail("User already exists.", HttpStatusCode.Conflict);
        }
        var passwordHash = passwordHasher.Hash(request.Password);
        var user = new User()
        {
            Email = normalizedEmail,
            PasswordHash = passwordHash,
        };
        var refreshToken = CreateRefreshToken(user);
        user.RefreshTokens.Add(refreshToken.Entity);

        await repository.AddAsync(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var authToken = jwtTokenGenerator.Generate(user);
        return ServiceResult<AuthResponse>.Success(
            CreateAuthResponse(user, authToken, refreshToken),
            HttpStatusCode.Created);

    }

    public async Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await repository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user == null)
        {
            return ServiceResult<AuthResponse>.Fail("Email or password is incorrect.", HttpStatusCode.Unauthorized);
        }
        var verifyResult = passwordHasher.Verify(request.Password, user.PasswordHash);
        if (!verifyResult)
        {
            return ServiceResult<AuthResponse>.Fail("Email or password is incorrect.", HttpStatusCode.Unauthorized);
        }
        var refreshToken = CreateRefreshToken(user);
        await refreshTokenRepository.AddAsync(refreshToken.Entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var authToken = jwtTokenGenerator.Generate(user);
        return ServiceResult<AuthResponse>.Success(CreateAuthResponse(user, authToken, refreshToken));
    }

    public async Task<ServiceResult<AuthUserResponse>> GetMeAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await repository.GetByIdAsync(userId);
        if (user is null)
        {
            return ServiceResult<AuthUserResponse>.Fail("User not found.", HttpStatusCode.NotFound);
        }

        return ServiceResult<AuthUserResponse>.Success(new AuthUserResponse(user.Id, user.Email));
    }

    public async Task<ServiceResult<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var tokenHash = refreshTokenHasher.Hash(request.RefreshToken);
        var existingToken = await refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (existingToken is null)
        {
            return ServiceResult<AuthResponse>.Fail("Invalid refresh token.", HttpStatusCode.Unauthorized);
        }

        var now = DateTime.UtcNow;
        if (existingToken.IsRevoked)
        {
            await refreshTokenRepository.RevokeActiveTokensForUserAsync(existingToken.UserId, now, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return ServiceResult<AuthResponse>.Fail("Invalid refresh token.", HttpStatusCode.Unauthorized);
        }

        if (existingToken.IsExpired)
        {
            existingToken.RevokedAtUtc = now;
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return ServiceResult<AuthResponse>.Fail("Refresh token has expired.", HttpStatusCode.Unauthorized);
        }

        if (existingToken.User is null)
        {
            return ServiceResult<AuthResponse>.Fail("User not found.", HttpStatusCode.Unauthorized);
        }

        var newRefreshToken = CreateRefreshToken(existingToken.User);
        existingToken.RevokedAtUtc = now;
        existingToken.ReplacedByTokenHash = newRefreshToken.Entity.TokenHash;

        await refreshTokenRepository.AddAsync(newRefreshToken.Entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var authToken = jwtTokenGenerator.Generate(existingToken.User);
        return ServiceResult<AuthResponse>.Success(CreateAuthResponse(existingToken.User, authToken, newRefreshToken));
    }

    public async Task<ServiceResult> LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var tokenHash = refreshTokenHasher.Hash(request.RefreshToken);
        var existingToken = await refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (existingToken is null || existingToken.IsRevoked)
        {
            return ServiceResult.Success(HttpStatusCode.NoContent);
        }

        existingToken.RevokedAtUtc = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    private CreatedRefreshToken CreateRefreshToken(User user)
    {
        var rawToken = refreshTokenGenerator.Generate();
        var expiresAtUtc = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenExpirationDays);
        var tokenHash = refreshTokenHasher.Hash(rawToken);

        return new CreatedRefreshToken(
            rawToken,
            new RefreshToken
            {
                UserId = user.Id,
                TokenHash = tokenHash,
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = expiresAtUtc,
            });
    }

    private static AuthResponse CreateAuthResponse(User user, AuthToken authToken, CreatedRefreshToken refreshToken)
    {
        return new AuthResponse(
            authToken.AccessToken,
            authToken.ExpiresAtUtc,
            refreshToken.RawToken,
            refreshToken.Entity.ExpiresAtUtc,
            new AuthUserResponse(user.Id, user.Email));
    }

    private sealed record CreatedRefreshToken(string RawToken, RefreshToken Entity);
}
