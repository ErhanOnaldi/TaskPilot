using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Interfaces.Persistence.Auth;

public interface IRefreshTokenRepository : IGenericRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task RevokeActiveTokensForUserAsync(int userId, DateTime revokedAtUtc, CancellationToken cancellationToken = default);
}
