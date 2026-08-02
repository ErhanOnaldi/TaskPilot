namespace TaskPilot.Application.Interfaces.Infrastructure.Auditing;

public interface IAuditContextAccessor
{
    int? UserId { get; }
    string? CorrelationId { get; }
    IDisposable BeginScope(int? userId, string? correlationId);
}
