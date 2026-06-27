using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.Auth.Services;

public sealed record CreatedRefreshToken(string RawToken, RefreshToken Entity);
