using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.Auth.RefreshTokens;

public sealed record CreatedRefreshToken(string RawToken, RefreshToken Entity);
