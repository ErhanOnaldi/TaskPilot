namespace TaskPilot.Application.Features.Auth.Dtos;

public sealed record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc,
    AuthUserResponse User
);