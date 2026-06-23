namespace TaskPilot.Application.Features.Auth.Dtos;

public sealed record RegisterRequest(string Email = null!, string Password = null!);