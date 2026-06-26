using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Persistence.Interceptors;

public sealed class AuditableEntitySaveChangesInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyAudit(eventData.Context?.ChangeTracker, DateTime.UtcNow);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAudit(eventData.Context?.ChangeTracker, DateTime.UtcNow);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public static void ApplyAudit(ChangeTracker? changeTracker, DateTime utcNow)
    {
        if (changeTracker is null)
        {
            return;
        }

        foreach (var entry in changeTracker.Entries<AuditEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = utcNow;
                    entry.Entity.UpdatedAt = utcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = utcNow;
                    entry.Property(x => x.CreatedAt).IsModified = false;
                    break;
            }
        }
    }
}
