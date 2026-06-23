using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Interfaces.Security;

public interface IJwtTokenGenerator
{
    AuthToken Generate(User user);
}
public sealed record AuthToken(string AccessToken, DateTime ExpiresAtUtc);