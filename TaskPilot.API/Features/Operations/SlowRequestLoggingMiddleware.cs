using System.Diagnostics;

namespace TaskPilot.API.Features.Operations;

public sealed class SlowRequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<SlowRequestLoggingMiddleware> logger,
    IConfiguration configuration)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var started = Stopwatch.GetTimestamp();
        try
        {
            await next(context);
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(started);
            var threshold = TimeSpan.FromMilliseconds(Math.Clamp(
                configuration.GetValue("Observability:SlowRequestThresholdMilliseconds", 300),
                50,
                60_000));
            if (elapsed >= threshold)
            {
                logger.LogWarning(
                    "Slow HTTP request {Method} {Path} returned {StatusCode} in {ElapsedMilliseconds} ms with correlation {CorrelationId}",
                    context.Request.Method,
                    context.Request.Path.Value,
                    context.Response.StatusCode,
                    elapsed.TotalMilliseconds,
                    context.TraceIdentifier);
            }
        }
    }
}
