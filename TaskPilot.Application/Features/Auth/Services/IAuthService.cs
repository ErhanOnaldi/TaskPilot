using TaskPilot.Application.Features.Auth.Dtos;

namespace TaskPilot.Application.Features.Auth.Services;

public interface IAuthService
{
    Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<AuthUserResponse>> GetMeAsync(int userId, CancellationToken cancellationToken);
    Task<ServiceResult<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken);

    Task<ServiceResult> LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken);
}