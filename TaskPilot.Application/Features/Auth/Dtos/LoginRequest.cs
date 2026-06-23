namespace TaskPilot.Application.Features.Auth.Dtos;

public record LoginRequest(string Email = null!, string Password = null!);