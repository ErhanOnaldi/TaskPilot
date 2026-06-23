namespace TaskPilot.Application.Features.Auth.Dtos;

public sealed record AuthResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    AuthUserResponse User
);