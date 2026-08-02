namespace TaskPilot.Application.Features.Auth.Dtos;

public sealed record GoogleLoginRequest(string IdToken = null!);
