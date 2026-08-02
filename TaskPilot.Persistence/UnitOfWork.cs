using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Infrastructure.Auditing;
using TaskPilot.Application.Interfaces.Infrastructure.Messaging;
using TaskPilot.Persistence.Auditing;

namespace TaskPilot.Persistence;

public class UnitOfWork(
    AppDbContext dbContext,
    IAuditContextAccessor auditContextAccessor,
    IDateTimeProvider dateTimeProvider,
    IEventOutbox eventOutbox) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var pendingAuditEntries = AuditTrailCollector.Capture(dbContext.ChangeTracker);
        var ownsTransaction = dbContext.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            var affectedRows = await dbContext.SaveChangesAsync(cancellationToken);
            await eventOutbox.FlushAsync(cancellationToken);
            var auditLogs = pendingAuditEntries.Select(entry => AuditTrailCollector.Finalize(
                entry,
                auditContextAccessor.UserId,
                auditContextAccessor.CorrelationId,
                dateTimeProvider.UtcNow));

            await dbContext.AuditLogs.AddRangeAsync(auditLogs, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return affectedRows;
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            throw;
        }
    }
}
