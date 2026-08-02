using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Infrastructure.Auditing;

namespace TaskPilot.Infrastructure.Auditing;

public sealed class AuditContextAccessor(
    ICurrentUserService currentUserService,
    IHttpContextAccessor httpContextAccessor) : IAuditContextAccessor
{
    private const string CorrelationItemKey = "TaskPilot.CorrelationId";
    private static readonly AsyncLocal<ScopeState?> AmbientScope = new();

    public int? UserId => AmbientScope.Value?.UserId ?? currentUserService.UserId;

    public string? CorrelationId => AmbientScope.Value?.CorrelationId
                                    ?? GetHttpCorrelationId()
                                    ?? Activity.Current?.TraceId.ToString();

    public IDisposable BeginScope(int? userId, string? correlationId)
    {
        var previous = AmbientScope.Value;
        AmbientScope.Value = new ScopeState(userId, correlationId);
        return new Scope(() => AmbientScope.Value = previous);
    }

    private string? GetHttpCorrelationId()
    {
        var context = httpContextAccessor.HttpContext;
        return context?.Items.TryGetValue(CorrelationItemKey, out var value) == true
            ? value as string
            : null;
    }

    private sealed record ScopeState(int? UserId, string? CorrelationId);

    private sealed class Scope(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose;
        public void Dispose() => Interlocked.Exchange(ref _dispose, null)?.Invoke();
    }
}
