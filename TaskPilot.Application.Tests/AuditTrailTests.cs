using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Domain.Entities;
using TaskPilot.Infrastructure.Auditing;
using TaskPilot.Persistence;
using TaskPilot.Persistence.Auditing;

namespace TaskPilot.Application.Tests;

public sealed class AuditTrailTests
{
    [Fact]
    public void Finalize_uses_generated_id_and_redacts_sensitive_values()
    {
        using var context = CreateContext();
        var user = new User { Email = "person@example.com", PasswordHash = "do-not-log" };
        context.Users.Add(user);
        var pending = Assert.Single(AuditTrailCollector.Capture(context.ChangeTracker));

        user.Id = 42;
        var auditLog = AuditTrailCollector.Finalize(
            pending,
            userId: 7,
            correlationId: "correlation-1",
            utcNow: new DateTime(2026, 8, 2, 12, 0, 0, DateTimeKind.Utc));

        Assert.Equal(42, auditLog.EntityId);
        Assert.Equal(7, auditLog.UserId);
        Assert.Equal("correlation-1", auditLog.CorrelationId);
        Assert.Contains("[REDACTED]", auditLog.NewValues);
        Assert.DoesNotContain("do-not-log", auditLog.NewValues);
    }

    [Fact]
    public void Audit_context_supports_background_scope_without_http_context()
    {
        var accessor = new AuditContextAccessor(
            new AnonymousCurrentUserService(),
            new HttpContextAccessor());

        using (accessor.BeginScope(99, "message-correlation"))
        {
            Assert.Equal(99, accessor.UserId);
            Assert.Equal("message-correlation", accessor.CorrelationId);
        }

        Assert.Null(accessor.UserId);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=taskpilot_tests;Username=test;Password=test",
                npgsql => npgsql.UseVector())
            .Options;
        return new AppDbContext(options);
    }

    private sealed class AnonymousCurrentUserService : ICurrentUserService
    {
        public int? UserId => null;
        public bool IsAuthenticated => false;
        public int GetRequiredUserId() => throw new UnauthorizedAccessException();
    }
}
