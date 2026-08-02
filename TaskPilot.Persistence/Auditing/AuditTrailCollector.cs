using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Persistence.Auditing;

internal static class AuditTrailCollector
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string[] SensitiveFragments = ["password", "token", "secret"];

    public static IReadOnlyList<PendingAuditEntry> Capture(ChangeTracker changeTracker)
    {
        return changeTracker.Entries<AuditEntity>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(entry => new PendingAuditEntry(
                entry.Entity,
                entry.State,
                entry.State is EntityState.Added ? null : SerializeEntry(entry, original: true)))
            .ToArray();
    }

    public static AuditLog Finalize(
        PendingAuditEntry pending,
        int? userId,
        string? correlationId,
        DateTime utcNow)
    {
        return new AuditLog
        {
            UserId = userId,
            EntityName = pending.Entity.GetType().Name,
            EntityId = ReadEntityId(pending.Entity),
            Action = pending.State.ToString(),
            OldValues = pending.OldValues,
            NewValues = pending.State == EntityState.Deleted ? null : SerializeEntity(pending.Entity),
            CreatedAt = utcNow,
            CorrelationId = correlationId
        };
    }

    private static string SerializeEntry(EntityEntry entry, bool original)
    {
        var values = entry.Properties.ToDictionary(
            property => property.Metadata.Name,
            property => Redact(property.Metadata.Name, original ? property.OriginalValue : property.CurrentValue));
        return JsonSerializer.Serialize(values, JsonOptions);
    }

    private static string SerializeEntity(object entity)
    {
        var values = entity.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.CanRead && IsScalar(property.PropertyType))
            .ToDictionary(property => property.Name, property => Redact(property.Name, property.GetValue(entity)));
        return JsonSerializer.Serialize(values, JsonOptions);
    }

    private static object? Redact(string propertyName, object? value)
    {
        return SensitiveFragments.Any(fragment => propertyName.Contains(fragment, StringComparison.OrdinalIgnoreCase))
            ? "[REDACTED]"
            : value;
    }

    private static bool IsScalar(Type type)
    {
        var unwrapped = Nullable.GetUnderlyingType(type) ?? type;
        return unwrapped.IsPrimitive || unwrapped.IsEnum ||
               unwrapped == typeof(string) || unwrapped == typeof(decimal) ||
               unwrapped == typeof(DateTime) || unwrapped == typeof(DateOnly) ||
               unwrapped == typeof(TimeOnly) || unwrapped == typeof(Guid);
    }

    private static int ReadEntityId(object entity)
    {
        return entity.GetType().GetProperty("Id", BindingFlags.Instance | BindingFlags.Public)?.GetValue(entity) as int?
               ?? 0;
    }

    internal sealed record PendingAuditEntry(object Entity, EntityState State, string? OldValues);
}
