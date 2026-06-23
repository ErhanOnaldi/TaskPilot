using Microsoft.EntityFrameworkCore;
using TaskPilot.Application.Interfaces.Persistence.Auth;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Persistence.EntityRepositories;

public sealed class RefreshTokenRepository : GenericRepository<RefreshToken>, IRefreshTokenRepository
{
    private readonly AppDbContext _dbContext;

    public RefreshTokenRepository(AppDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return _dbContext.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
    }

    public async Task RevokeActiveTokensForUserAsync(
        int userId,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var activeTokens = await _dbContext.RefreshTokens
            .Where(x => x.UserId == userId &&
                        x.RevokedAtUtc == null &&
                        x.ExpiresAtUtc > revokedAtUtc)
            .ToListAsync(cancellationToken);

        foreach (var refreshToken in activeTokens)
        {
            refreshToken.RevokedAtUtc = revokedAtUtc;
        }
    }
}
