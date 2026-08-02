using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TaskPilot.API.Features.Platform.Correlation;

namespace TaskPilot.API.Features.Platform.RateLimiting;

public static class TaskPilotRateLimitingExtensions
{
    public const string AuthPolicy = "auth";
    public const string AiPolicy = "ai";

    public static IServiceCollection AddTaskPilotRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(AuthPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetClientPartitionKey(httpContext),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
            options.AddPolicy(AiPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetAuthenticatedUserOrClientPartitionKey(httpContext),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
            options.OnRejected = async (context, cancellationToken) =>
            {
                var problemDetailsService = context.HttpContext.RequestServices
                    .GetService<IProblemDetailsService>();
                if (problemDetailsService is null)
                {
                    return;
                }

                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too many requests",
                    Detail = "Request rate limit exceeded.",
                    Instance = context.HttpContext.Request.Path
                }.WithCorrelationId(context.HttpContext);

                await problemDetailsService.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context.HttpContext,
                    ProblemDetails = problemDetails
                });
            };
        });

        return services;
    }

    private static string GetAuthenticatedUserOrClientPartitionKey(HttpContext httpContext)
    {
        var userId = httpContext.User.FindFirst("sub")?.Value
                     ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        return string.IsNullOrWhiteSpace(userId)
            ? GetClientPartitionKey(httpContext)
            : $"user:{userId}";
    }

    private static string GetClientPartitionKey(HttpContext httpContext) =>
        $"client:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
}
