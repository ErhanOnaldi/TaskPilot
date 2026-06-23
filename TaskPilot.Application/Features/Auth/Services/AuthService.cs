using System.Net;
using TaskPilot.Application.Features.Auth.Dtos;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Application.Interfaces.Persistence.User;
using TaskPilot.Application.Interfaces.Security;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.Auth.Services;

public class AuthService(IUserRepository repository, IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator) : IAuthService
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
        await repository.AddAsync(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var authToken = jwtTokenGenerator.Generate(user);
        return ServiceResult<AuthResponse>.Success(
            new AuthResponse(authToken.AccessToken, authToken.ExpiresAtUtc, new AuthUserResponse(user.Id, user.Email)),
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
        var authToken = jwtTokenGenerator.Generate(user);
        return ServiceResult<AuthResponse>.Success(new AuthResponse(authToken.AccessToken, authToken.ExpiresAtUtc, new AuthUserResponse(user.Id, user.Email)));
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
}
