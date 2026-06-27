using System.Linq.Expressions;
using Microsoft.Extensions.Options;
using TaskPilot.Application.Features.Auth.Services;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Persistence.Auth;
using TaskPilot.Application.Interfaces.Security;
using TaskPilot.Domain.Entities;
using TaskPilot.Domain.Options;

namespace TaskPilot.Application.Tests;

public class RefreshTokenServiceTests
{
    private static readonly DateTime Now = new(2026, 6, 26, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CreateForUser_creates_hashed_refresh_token_entity()
    {
        var repository = new FakeRefreshTokenRepository();
        var service = CreateService(repository);
        var user = new User { Id = 42, Email = "user@example.com" };

        var result = service.CreateForUser(user);

        Assert.Equal("raw-token-1", result.RawToken);
        Assert.Equal(42, result.Entity.UserId);
        Assert.Equal("hash:raw-token-1", result.Entity.TokenHash);
        Assert.Equal(Now, result.Entity.CreatedAtUtc);
        Assert.Equal(Now.AddDays(7), result.Entity.ExpiresAtUtc);
    }

    [Fact]
    public async Task CreateAndAddForUserAsync_creates_and_adds_refresh_token_entity()
    {
        var repository = new FakeRefreshTokenRepository();
        var service = CreateService(repository);
        var user = new User { Id = 42, Email = "user@example.com" };

        var result = await service.CreateAndAddForUserAsync(user);

        Assert.Equal("raw-token-1", result.RawToken);
        Assert.Single(repository.Tokens, token => token.TokenHash == "hash:raw-token-1");
    }

    [Fact]
    public async Task RotateAsync_returns_invalid_when_token_does_not_exist()
    {
        var service = CreateService(new FakeRefreshTokenRepository());

        var result = await service.RotateAsync("missing-token", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(RefreshTokenRotationFailureReason.Invalid, result.FailureReason);
        Assert.False(result.RequiresSaveChanges);
    }

    [Fact]
    public async Task RotateAsync_revokes_active_tokens_when_revoked_token_is_reused()
    {
        var repository = new FakeRefreshTokenRepository();
        repository.Tokens.Add(new RefreshToken
        {
            Id = 1,
            UserId = 42,
            TokenHash = "hash:stolen-token",
            CreatedAtUtc = Now.AddDays(-1),
            ExpiresAtUtc = Now.AddDays(1),
            RevokedAtUtc = Now.AddHours(-1),
            User = new User { Id = 42, Email = "user@example.com" }
        });
        repository.Tokens.Add(new RefreshToken
        {
            Id = 2,
            UserId = 42,
            TokenHash = "hash:active-token",
            CreatedAtUtc = Now.AddHours(-1),
            ExpiresAtUtc = Now.AddDays(1)
        });
        var service = CreateService(repository);

        var result = await service.RotateAsync("stolen-token", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(RefreshTokenRotationFailureReason.Invalid, result.FailureReason);
        Assert.True(result.RequiresSaveChanges);
        Assert.Equal(Now, repository.Tokens.Single(token => token.Id == 2).RevokedAtUtc);
    }

    [Fact]
    public async Task RotateAsync_revokes_expired_token_and_returns_expired()
    {
        var repository = new FakeRefreshTokenRepository();
        var expiredToken = new RefreshToken
        {
            Id = 1,
            UserId = 42,
            TokenHash = "hash:expired-token",
            CreatedAtUtc = Now.AddDays(-10),
            ExpiresAtUtc = Now.AddSeconds(-1),
            User = new User { Id = 42, Email = "user@example.com" }
        };
        repository.Tokens.Add(expiredToken);
        var service = CreateService(repository);

        var result = await service.RotateAsync("expired-token", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(RefreshTokenRotationFailureReason.Expired, result.FailureReason);
        Assert.True(result.RequiresSaveChanges);
        Assert.Equal(Now, expiredToken.RevokedAtUtc);
    }

    [Fact]
    public async Task RotateAsync_rotates_valid_token()
    {
        var repository = new FakeRefreshTokenRepository();
        var user = new User { Id = 42, Email = "user@example.com" };
        var existingToken = new RefreshToken
        {
            Id = 1,
            UserId = 42,
            TokenHash = "hash:valid-token",
            CreatedAtUtc = Now.AddHours(-1),
            ExpiresAtUtc = Now.AddDays(1),
            User = user
        };
        repository.Tokens.Add(existingToken);
        var service = CreateService(repository);

        var result = await service.RotateAsync("valid-token", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.RequiresSaveChanges);
        Assert.Same(user, result.User);
        Assert.Equal(Now, existingToken.RevokedAtUtc);
        Assert.Equal("hash:raw-token-1", existingToken.ReplacedByTokenHash);
        Assert.Equal("raw-token-1", result.RefreshToken?.RawToken);
        Assert.Single(repository.Tokens, token => token.TokenHash == "hash:raw-token-1");
    }

    [Fact]
    public async Task LogoutAsync_is_idempotent_when_token_does_not_exist()
    {
        var service = CreateService(new FakeRefreshTokenRepository());

        var result = await service.LogoutAsync("missing-token", CancellationToken.None);

        Assert.False(result.RequiresSaveChanges);
    }

    [Fact]
    public async Task LogoutAsync_revokes_existing_token()
    {
        var repository = new FakeRefreshTokenRepository();
        var token = new RefreshToken
        {
            Id = 1,
            UserId = 42,
            TokenHash = "hash:valid-token",
            CreatedAtUtc = Now.AddHours(-1),
            ExpiresAtUtc = Now.AddDays(1)
        };
        repository.Tokens.Add(token);
        var service = CreateService(repository);

        var result = await service.LogoutAsync("valid-token", CancellationToken.None);

        Assert.True(result.RequiresSaveChanges);
        Assert.Equal(Now, token.RevokedAtUtc);
    }

    private static RefreshTokenService CreateService(FakeRefreshTokenRepository repository)
    {
        return new RefreshTokenService(
            repository,
            new FakeRefreshTokenGenerator(),
            new FakeRefreshTokenHasher(),
            new FakeDateTimeProvider(Now),
            Options.Create(new JwtOptions
            {
                Issuer = "issuer",
                Audience = "audience",
                Secret = "super-secret",
                AccessTokenExpirationMinutes = 15,
                RefreshTokenExpirationDays = 7
            }));
    }

    private sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
    {
        public List<RefreshToken> Tokens { get; } = [];

        public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Tokens.FirstOrDefault(token => token.TokenHash == tokenHash));
        }

        public Task RevokeActiveTokensForUserAsync(int userId, DateTime revokedAtUtc, CancellationToken cancellationToken = default)
        {
            foreach (var token in Tokens.Where(token => token.UserId == userId && token.RevokedAtUtc is null && token.ExpiresAtUtc > revokedAtUtc))
            {
                token.RevokedAtUtc = revokedAtUtc;
            }

            return Task.CompletedTask;
        }

        public Task<List<RefreshToken>> GetAllAsync() => Task.FromResult(Tokens);
        public Task<List<RefreshToken>> GetAllPagedAsync(int pageNumber, int pageSize) => Task.FromResult(Tokens.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList());
        public IQueryable<RefreshToken> Where(Expression<Func<RefreshToken, bool>> predicate) => Tokens.AsQueryable().Where(predicate);
        public ValueTask<RefreshToken?> GetByIdAsync(int id) => ValueTask.FromResult(Tokens.FirstOrDefault(token => token.Id == id));

        public ValueTask AddAsync(RefreshToken entity)
        {
            entity.Id = entity.Id == 0 ? Tokens.Count + 1 : entity.Id;
            Tokens.Add(entity);
            return ValueTask.CompletedTask;
        }

        public Task<bool> AnyAsync(Expression<Func<RefreshToken, bool>> predicate) => Task.FromResult(Tokens.AsQueryable().Any(predicate));
        public void Update(RefreshToken entity) { }
        public void Delete(RefreshToken entity) => Tokens.Remove(entity);
    }

    private sealed class FakeRefreshTokenGenerator : IRefreshTokenGenerator
    {
        private int _counter;
        public string Generate() => $"raw-token-{++_counter}";
    }

    private sealed class FakeRefreshTokenHasher : IRefreshTokenHasher
    {
        public string Hash(string refreshToken) => $"hash:{refreshToken}";
    }

    private sealed class FakeDateTimeProvider(DateTime utcNow) : IDateTimeProvider
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}
