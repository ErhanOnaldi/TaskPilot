using System.Linq.Expressions;
using System.Net;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TaskPilot.Application.Features.Auth.Dtos;
using TaskPilot.Application.Features.Auth.Factories;
using TaskPilot.Application.Features.Auth.RefreshTokens;
using TaskPilot.Application.Features.Auth.Services;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Application.Interfaces.Persistence.Auth;
using TaskPilot.Application.Interfaces.Persistence.User;
using TaskPilot.Application.Interfaces.Security;
using TaskPilot.Application.Mappings;
using TaskPilot.Domain.Entities;
using TaskPilot.Domain.Options;

namespace TaskPilot.Application.Tests;

public class AuthServiceGoogleTests
{
    private const string IdToken = "google-id-token";

    [Fact]
    public async Task GoogleLoginAsync_creates_a_passwordless_account_for_an_unknown_google_user()
    {
        var users = new FakeUserRepository();
        var service = CreateService(users, new FakeGoogleTokenValidator("google-sub-1", "New.User@Example.com"));

        var result = await service.GoogleLoginAsync(new GoogleLoginRequest(IdToken), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpStatusCode.Created, result.Status);
        var created = Assert.Single(users.Users);
        Assert.Equal("new.user@example.com", created.Email);
        Assert.Equal("google-sub-1", created.GoogleSubject);
        Assert.Null(created.PasswordHash);
        Assert.Single(created.RefreshTokens);
        Assert.Equal("access-token", result.Data!.AccessToken);
    }

    [Fact]
    public async Task GoogleLoginAsync_links_google_to_an_existing_account_with_the_same_email()
    {
        var users = new FakeUserRepository();
        users.Users.Add(new User { Id = 7, Email = "existing@example.com", PasswordHash = "hash" });
        var service = CreateService(users, new FakeGoogleTokenValidator("google-sub-2", "existing@example.com"));

        var result = await service.GoogleLoginAsync(new GoogleLoginRequest(IdToken), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpStatusCode.OK, result.Status);
        var user = Assert.Single(users.Users);
        Assert.Equal("google-sub-2", user.GoogleSubject);
        Assert.Equal("hash", user.PasswordHash);
        Assert.Equal(7, result.Data!.User.Id);
    }

    [Fact]
    public async Task GoogleLoginAsync_matches_on_google_subject_even_when_the_google_email_changed()
    {
        var users = new FakeUserRepository();
        users.Users.Add(new User { Id = 9, Email = "old@example.com", GoogleSubject = "google-sub-3" });
        var service = CreateService(users, new FakeGoogleTokenValidator("google-sub-3", "renamed@example.com"));

        var result = await service.GoogleLoginAsync(new GoogleLoginRequest(IdToken), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(users.Users);
        Assert.Equal(9, result.Data!.User.Id);
        Assert.Equal("old@example.com", users.Users[0].Email);
    }

    [Fact]
    public async Task GoogleLoginAsync_rejects_an_invalid_token()
    {
        var users = new FakeUserRepository();
        var service = CreateService(users, new FakeGoogleTokenValidator(identity: null));

        var result = await service.GoogleLoginAsync(new GoogleLoginRequest(IdToken), CancellationToken.None);

        Assert.True(result.IsFail);
        Assert.Equal(HttpStatusCode.Unauthorized, result.Status);
        Assert.Empty(users.Users);
    }

    [Fact]
    public async Task GoogleLoginAsync_rejects_an_unverified_google_email()
    {
        var users = new FakeUserRepository();
        var validator = new FakeGoogleTokenValidator("google-sub-4", "unverified@example.com", isEmailVerified: false);
        var service = CreateService(users, validator);

        var result = await service.GoogleLoginAsync(new GoogleLoginRequest(IdToken), CancellationToken.None);

        Assert.True(result.IsFail);
        Assert.Equal(HttpStatusCode.Unauthorized, result.Status);
        Assert.Empty(users.Users);
    }

    [Fact]
    public async Task GoogleLoginAsync_reports_unavailable_when_google_is_not_configured()
    {
        var validator = new FakeGoogleTokenValidator("google-sub-5", "user@example.com") { IsEnabled = false };
        var service = CreateService(new FakeUserRepository(), validator);

        var result = await service.GoogleLoginAsync(new GoogleLoginRequest(IdToken), CancellationToken.None);

        Assert.True(result.IsFail);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, result.Status);
    }

    [Fact]
    public async Task LoginAsync_tells_passwordless_accounts_to_use_google()
    {
        var users = new FakeUserRepository();
        users.Users.Add(new User { Id = 3, Email = "google-only@example.com", GoogleSubject = "google-sub-6" });
        var service = CreateService(users, new FakeGoogleTokenValidator("google-sub-6", "google-only@example.com"));

        var result = await service.LoginAsync(new LoginRequest("google-only@example.com", "whatever-1"), CancellationToken.None);

        Assert.True(result.IsFail);
        Assert.Equal(HttpStatusCode.Unauthorized, result.Status);
        Assert.Contains("Google", result.ErrorMessages![0], StringComparison.Ordinal);
    }

    private static AuthService CreateService(FakeUserRepository users, FakeGoogleTokenValidator googleTokenValidator)
    {
        var mapper = new MapperConfiguration(
            configuration => configuration.AddProfile<ApplicationMappingProfile>(),
            NullLoggerFactory.Instance).CreateMapper();

        return new AuthService(
            users,
            new FakeUnitOfWork(users),
            new FakePasswordHasher(),
            new FakeJwtTokenGenerator(),
            new RefreshTokenService(
                new FakeRefreshTokenRepository(),
                new FakeRefreshTokenGenerator(),
                new FakeRefreshTokenHasher(),
                new FakeDateTimeProvider(),
                Options.Create(new JwtOptions
                {
                    Issuer = "TaskPilot",
                    Audience = "TaskPilot",
                    Secret = "test-secret-that-is-long-enough-for-tests",
                    AccessTokenExpirationMinutes = 15,
                    RefreshTokenExpirationDays = 7
                })),
            new AuthResponseFactory(mapper),
            new FakeCurrentUserService(),
            googleTokenValidator,
            mapper);
    }

    private sealed class FakeGoogleTokenValidator(GoogleIdentity? identity) : IGoogleIdentityTokenValidator
    {
        public FakeGoogleTokenValidator(string subject, string email, bool isEmailVerified = true)
            : this(new GoogleIdentity(subject, email, isEmailVerified))
        {
        }

        public bool IsEnabled { get; init; } = true;

        public Task<GoogleIdentity?> ValidateAsync(string idToken, CancellationToken cancellationToken)
        {
            Assert.Equal(IdToken, idToken);
            return Task.FromResult(identity);
        }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private int _nextId = 100;

        public List<User> Users { get; } = [];

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
            => Task.FromResult(Users.FirstOrDefault(user => user.Email == email));

        public Task<User?> GetByGoogleSubjectAsync(string googleSubject, CancellationToken cancellationToken = default)
            => Task.FromResult(Users.FirstOrDefault(user => user.GoogleSubject == googleSubject));

        public Task<List<User>> GetAllAsync() => Task.FromResult(Users);
        public Task<List<User>> GetAllPagedAsync(int pageNumber, int pageSize) => Task.FromResult(Users);
        public IQueryable<User> Where(Expression<Func<User, bool>> predicate) => Users.AsQueryable().Where(predicate);
        public ValueTask<User?> GetByIdAsync(int id) => ValueTask.FromResult(Users.FirstOrDefault(user => user.Id == id));

        public ValueTask AddAsync(User entity)
        {
            entity.Id = entity.Id == 0 ? _nextId++ : entity.Id;
            Users.Add(entity);
            return ValueTask.CompletedTask;
        }

        public Task<bool> AnyAsync(Expression<Func<User, bool>> predicate) => Task.FromResult(Users.AsQueryable().Any(predicate));
        public void Update(User entity) { }
        public void Delete(User entity) => Users.Remove(entity);
    }

    private sealed class FakeUnitOfWork(FakeUserRepository users) : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(users.Users.Count);
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hash:{password}";
        public bool Verify(string password, string passwordHash) => passwordHash == $"hash:{password}";
    }

    private sealed class FakeJwtTokenGenerator : IJwtTokenGenerator
    {
        public AuthToken Generate(User user) => new("access-token", new DateTime(2026, 8, 2, 12, 15, 0, DateTimeKind.Utc));
    }

    private sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly List<RefreshToken> _tokens = [];

        public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
            => Task.FromResult(_tokens.FirstOrDefault(token => token.TokenHash == tokenHash));

        public Task RevokeActiveTokensForUserAsync(int userId, DateTime revokedAtUtc, CancellationToken cancellationToken = default)
        {
            _tokens.Where(token => token.UserId == userId).ToList()
                .ForEach(token => token.RevokedAtUtc = revokedAtUtc);
            return Task.CompletedTask;
        }

        public Task<List<RefreshToken>> GetAllAsync() => Task.FromResult(_tokens);
        public Task<List<RefreshToken>> GetAllPagedAsync(int pageNumber, int pageSize) => Task.FromResult(_tokens);
        public IQueryable<RefreshToken> Where(Expression<Func<RefreshToken, bool>> predicate) => _tokens.AsQueryable().Where(predicate);
        public ValueTask<RefreshToken?> GetByIdAsync(int id) => ValueTask.FromResult(_tokens.FirstOrDefault(token => token.Id == id));
        public ValueTask AddAsync(RefreshToken entity) { _tokens.Add(entity); return ValueTask.CompletedTask; }
        public Task<bool> AnyAsync(Expression<Func<RefreshToken, bool>> predicate) => Task.FromResult(_tokens.AsQueryable().Any(predicate));
        public void Update(RefreshToken entity) { }
        public void Delete(RefreshToken entity) => _tokens.Remove(entity);
    }

    private sealed class FakeRefreshTokenGenerator : IRefreshTokenGenerator
    {
        public string Generate() => "raw-refresh-token";
    }

    private sealed class FakeRefreshTokenHasher : IRefreshTokenHasher
    {
        public string Hash(string refreshToken) => $"hash:{refreshToken}";
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 8, 2, 12, 0, 0, DateTimeKind.Utc);
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public int? UserId => null;
        public bool IsAuthenticated => false;
        public int GetRequiredUserId() => throw new InvalidOperationException("Not used by the Google sign-in flow.");
    }
}
