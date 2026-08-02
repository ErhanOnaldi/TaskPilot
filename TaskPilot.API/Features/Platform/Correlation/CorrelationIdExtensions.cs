using Microsoft.AspNetCore.Mvc;

namespace TaskPilot.API.Features.Platform.Correlation;

public static class CorrelationIdExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app) =>
        app.UseMiddleware<CorrelationIdMiddleware>();

    public static ProblemDetails WithCorrelationId(this ProblemDetails problemDetails, HttpContext httpContext)
    {
        var correlationId = CorrelationIdMiddleware.GetCorrelationId(httpContext);
        if (!string.IsNullOrEmpty(correlationId) && !problemDetails.Extensions.ContainsKey("correlationId"))
        {
            problemDetails.Extensions["correlationId"] = correlationId;
        }

        return problemDetails;
    }

    public static IServiceCollection AddCorrelationProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
                context.ProblemDetails.WithCorrelationId(context.HttpContext);
        });

        return services;
    }
}
