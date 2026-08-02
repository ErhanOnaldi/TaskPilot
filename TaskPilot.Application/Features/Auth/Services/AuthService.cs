using System.Net;
using AutoMapper;
using TaskPilot.Application.Features.Auth.Dtos;
using TaskPilot.Application.Features.Auth.Factories;
using TaskPilot.Application.Features.Auth.RefreshTokens;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Application.Interfaces.Persistence.User;
using TaskPilot.Application.Interfaces.Security;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.Auth.Services;

public class AuthService(
    IUserRepository repository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IRefreshTokenService refreshTokenService,
    IAuthResponseFactory authResponseFactory,
    ICurrentUserService currentUserService,
    IGoogleIdentityTokenValidator googleIdentityTokenValidator,
    IMapper mapper) : IAuthService
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
        var refreshToken = refreshTokenService.CreateForUser(user);
        user.RefreshTokens.Add(refreshToken.Entity);

        await repository.AddAsync(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var authToken = jwtTokenGenerator.Generate(user);
        return ServiceResult<AuthResponse>.Success(
            authResponseFactory.Create(user, authToken, refreshToken),
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
        if (user.PasswordHash is null)
        {
            return ServiceResult<AuthResponse>.Fail(
                "This account was created with Google. Continue with Google.",
                HttpStatusCode.Unauthorized);
        }
        var verifyResult = passwordHasher.Verify(request.Password, user.PasswordHash);
        if (!verifyResult)
        {
            return ServiceResult<AuthResponse>.Fail("Email or password is incorrect.", HttpStatusCode.Unauthorized);
        }
        var refreshToken = await refreshTokenService.CreateAndAddForUserAsync(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var authToken = jwtTokenGenerator.Generate(user);
        return ServiceResult<AuthResponse>.Success(authResponseFactory.Create(user, authToken, refreshToken));
    }

    public async Task<ServiceResult<AuthResponse>> GoogleLoginAsync(GoogleLoginRequest request, CancellationToken cancellationToken)
    {
        if (!googleIdentityTokenValidator.IsEnabled)
        {
            return ServiceResult<AuthResponse>.Fail(
                "Google sign-in is not configured.",
                HttpStatusCode.ServiceUnavailable);
        }

        var identity = await googleIdentityTokenValidator.ValidateAsync(request.IdToken, cancellationToken);
        if (identity is null)
        {
            return ServiceResult<AuthResponse>.Fail("Google sign-in could not be verified.", HttpStatusCode.Unauthorized);
        }

        if (!identity.IsEmailVerified)
        {
            return ServiceResult<AuthResponse>.Fail(
                "The Google account email is not verified.",
                HttpStatusCode.Unauthorized);
        }

        var normalizedEmail = identity.Email.Trim().ToLowerInvariant();
        var isNewUser = false;
        var user = await repository.GetByGoogleSubjectAsync(identity.Subject, cancellationToken);
        if (user is null)
        {
            // Same person signing in with Google for the first time keeps their existing local account.
            user = await repository.GetByEmailAsync(normalizedEmail, cancellationToken);
            if (user is null)
            {
                isNewUser = true;
                user = new User
                {
                    Email = normalizedEmail,
                    PasswordHash = null,
                    GoogleSubject = identity.Subject
                };
                await repository.AddAsync(user);
            }
            else
            {
                user.GoogleSubject = identity.Subject;
            }
        }

        var refreshToken = isNewUser
            ? CreateAndAttachRefreshToken(user)
            : await refreshTokenService.CreateAndAddForUserAsync(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var authToken = jwtTokenGenerator.Generate(user);
        return ServiceResult<AuthResponse>.Success(
            authResponseFactory.Create(user, authToken, refreshToken),
            isNewUser ? HttpStatusCode.Created : HttpStatusCode.OK);
    }

    private CreatedRefreshToken CreateAndAttachRefreshToken(User user)
    {
        var refreshToken = refreshTokenService.CreateForUser(user);
        user.RefreshTokens.Add(refreshToken.Entity);
        return refreshToken;
    }

    public async Task<ServiceResult<AuthUserResponse>> GetMeAsync(CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var user = await repository.GetByIdAsync(userId);
        if (user is null)
        {
            return ServiceResult<AuthUserResponse>.Fail("User not found.", HttpStatusCode.NotFound);
        }

        return ServiceResult<AuthUserResponse>.Success(mapper.Map<AuthUserResponse>(user));
    }

    public async Task<ServiceResult<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var rotationResult = await refreshTokenService.RotateAsync(request.RefreshToken, cancellationToken);
        if (!rotationResult.IsSuccess)
        {
            if (rotationResult.RequiresSaveChanges)
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return ServiceResult<AuthResponse>.Fail(
                MapRefreshTokenFailureMessage(rotationResult.FailureReason),
                HttpStatusCode.Unauthorized);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var authToken = jwtTokenGenerator.Generate(rotationResult.User!);
        return ServiceResult<AuthResponse>.Success(
            authResponseFactory.Create(rotationResult.User!, authToken, rotationResult.RefreshToken!));
    }

    public async Task<ServiceResult> LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var logoutResult = await refreshTokenService.LogoutAsync(request.RefreshToken, cancellationToken);
        if (logoutResult.RequiresSaveChanges)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    private static string MapRefreshTokenFailureMessage(RefreshTokenRotationFailureReason? reason)
    {
        return reason == RefreshTokenRotationFailureReason.Expired
            ? "Refresh token has expired."
            : "Invalid refresh token.";
    }
}
