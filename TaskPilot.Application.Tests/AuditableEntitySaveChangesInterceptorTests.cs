using Microsoft.EntityFrameworkCore;
using TaskPilot.Domain.Entities;
using TaskPilot.Persistence;
using TaskPilot.Persistence.Interceptors;

namespace TaskPilot.Application.Tests;

public class AuditableEntitySaveChangesInterceptorTests
{
    [Fact]
    public void ApplyAudit_sets_created_at_and_updated_at_for_added_entities()
    {
        var now = new DateTime(2026, 6, 26, 10, 0, 0, DateTimeKind.Utc);
        using var context = CreateContext();
        var workspace = new WorkSpace
        {
            Name = "Engineering",
            CreatedByUserId = 1,
            CreatedAt = DateTime.MinValue,
            UpdatedAt = DateTime.MinValue
        };

        context.WorkSpaces.Add(workspace);

        AuditableEntitySaveChangesInterceptor.ApplyAudit(context.ChangeTracker, now);

        Assert.Equal(now, workspace.CreatedAt);
        Assert.Equal(now, workspace.UpdatedAt);
    }

    [Fact]
    public void ApplyAudit_sets_only_updated_at_for_modified_entities()
    {
        var createdAt = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2026, 6, 26, 10, 0, 0, DateTimeKind.Utc);
        using var context = CreateContext();
        var workspace = new WorkSpace
        {
            Id = 1,
            Name = "Engineering",
            CreatedByUserId = 1,
            CreatedAt = createdAt,
            UpdatedAt = DateTime.MinValue
        };

        context.WorkSpaces.Attach(workspace);
        context.Entry(workspace).State = EntityState.Modified;

        AuditableEntitySaveChangesInterceptor.ApplyAudit(context.ChangeTracker, updatedAt);

        Assert.Equal(createdAt, workspace.CreatedAt);
        Assert.Equal(updatedAt, workspace.UpdatedAt);
        Assert.False(context.Entry(workspace).Property(nameof(AuditEntity.CreatedAt)).IsModified);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=taskpilot_tests;Username=test;Password=test")
            .Options;

        return new AppDbContext(options);
    }
}
