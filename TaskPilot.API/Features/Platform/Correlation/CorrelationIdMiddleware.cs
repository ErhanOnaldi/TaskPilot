using System.Diagnostics;

namespace TaskPilot.API.Features.Platform.Correlation;

/// <summary>
/// Establishes one safe, request-scoped correlation identifier for HTTP responses, logs, and tracing.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";
    public const string ItemKey = "TaskPilot.CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetIncomingCorrelationId(context.Request.Headers[HeaderName])
                            ?? Guid.NewGuid().ToString("N");

        context.Items[ItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        Activity? createdActivity = null;
        if (Activity.Current is null)
        {
            createdActivity = new Activity("TaskPilot.Correlation");
            createdActivity.Start();
        }

        try
        {
            Activity.Current?.SetTag("correlation.id", correlationId);

            using (logger.BeginScope(new Dictionary<string, object?>
                   {
                       ["CorrelationId"] = correlationId
                   }))
            {
                await next(context);
            }
        }
        finally
        {
            createdActivity?.Stop();
        }
    }

    public static string? GetCorrelationId(HttpContext context) =>
        context.Items.TryGetValue(ItemKey, out var value) ? value as string : null;

    private static string? GetIncomingCorrelationId(Microsoft.Extensions.Primitives.StringValues values)
    {
        if (values.Count != 1)
        {
            return null;
        }

        var value = values[0];
        if (string.IsNullOrWhiteSpace(value) || value.Length > 128)
        {
            return null;
        }

        foreach (var character in value)
        {
            if (!char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_' and not '.')
            {
                return null;
            }
        }

        return value;
    }
}
