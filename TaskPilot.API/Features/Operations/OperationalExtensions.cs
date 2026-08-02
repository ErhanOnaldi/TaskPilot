using System.Data.Common;
using System.Diagnostics;
using System.Net.Sockets;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Json;
using Serilog.Sinks.OpenTelemetry;
using Npgsql;

namespace TaskPilot.API.Features.Operations;

public static class OperationalExtensions
{
    public static IServiceCollection AddTaskPilotOperations(this IServiceCollection services, IConfiguration configuration)
    {
        Activity.DefaultIdFormat = ActivityIdFormat.W3C;
        Activity.ForceDefaultIdFormat = true;
        var otlpEndpoint = configuration["Observability:OtlpEndpoint"];
        services.AddSerilog((serviceProvider, logger) =>
        {
            logger
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("service.name", "TaskPilot.API")
                .WriteTo.Console(new JsonFormatter());

            if (Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var _))
            {
                logger.WriteTo.OpenTelemetry(options =>
                {
                    options.Endpoint = otlpEndpoint!;
                    options.Protocol = OtlpProtocol.Grpc;
                    options.ResourceAttributes = new Dictionary<string, object>
                    {
                        ["service.name"] = "TaskPilot.API"
                    };
                });
            }
        });

        var telemetry = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("TaskPilot.API"))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddNpgsql()
                .AddSource("MassTransit"))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddNpgsqlInstrumentation()
                .AddMeter("MassTransit", "Npgsql"));

        if (Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var exporterEndpoint))
        {
            telemetry.WithTracing(tracing => tracing.AddOtlpExporter(options => options.Endpoint = exporterEndpoint));
            telemetry.WithMetrics(metrics => metrics.AddOtlpExporter(options => options.Endpoint = exporterEndpoint));
        }

        var readinessChecks = services.AddHealthChecks();
        readinessChecks.AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);
        readinessChecks.Add(new HealthCheckRegistration(
            "postgres",
            CreatePostgresCheck(configuration),
            failureStatus: HealthStatus.Unhealthy,
            tags: ["ready"],
            timeout: GetTimeout(configuration, "PostgreSqlTimeoutSeconds")));
        readinessChecks.Add(new HealthCheckRegistration(
            "redis",
            CreateRedisCheck(configuration),
            failureStatus: HealthStatus.Unhealthy,
            tags: ["ready"],
            timeout: GetTimeout(configuration, "RedisTimeoutSeconds")));
        readinessChecks.Add(new HealthCheckRegistration(
            "rabbitmq",
            CreateRabbitMqCheck(configuration),
            failureStatus: HealthStatus.Unhealthy,
            tags: ["ready"],
            timeout: GetTimeout(configuration, "RabbitMqTimeoutSeconds")));

        return services;
    }

    public static IApplicationBuilder MapTaskPilotHealthEndpoints(this IApplicationBuilder app)
    {
        app.UseHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live")
        });
        app.UseHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        return app;
    }

    private static IHealthCheck CreatePostgresCheck(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PostgreSql");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new UnconfiguredHealthCheck("PostgreSql connection string is missing.");
        }

        var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
        return TcpEndpointHealthCheck.FromHostAndPort(
            builder.TryGetValue("Host", out var host) ? host?.ToString() : null,
            builder.TryGetValue("Port", out var port) && int.TryParse(port?.ToString(), out var parsedPort)
                ? parsedPort
                : 5432);
    }

    private static IHealthCheck CreateRedisCheck(IConfiguration configuration)
    {
        var endpoint = configuration.GetConnectionString("Redis")?.Split(',', 2)[0].Trim();
        var parts = endpoint?.Split(':', 2);
        return parts is [var host, var port] && int.TryParse(port, out var parsedPort)
            ? TcpEndpointHealthCheck.FromHostAndPort(host, parsedPort)
            : TcpEndpointHealthCheck.FromHostAndPort(endpoint, 6379);
    }

    private static IHealthCheck CreateRabbitMqCheck(IConfiguration configuration) =>
        TcpEndpointHealthCheck.FromHostAndPort(
            configuration["RabbitMq:HostName"],
            int.TryParse(configuration["RabbitMq:Port"], out var port) ? port : 5672);

    private static TimeSpan GetTimeout(IConfiguration configuration, string key) =>
        TimeSpan.FromSeconds(Math.Clamp(configuration.GetValue($"HealthChecks:{key}", 3), 1, 30));
}

public sealed class TcpEndpointHealthCheck(string? host, int port) : IHealthCheck
{
    public static IHealthCheck FromHostAndPort(string? host, int port) =>
        string.IsNullOrWhiteSpace(host) || port is < 1 or > 65535
            ? new UnconfiguredHealthCheck("Endpoint configuration is missing or invalid.")
            : new TcpEndpointHealthCheck(host, port);

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using var client = new TcpClient();

        try
        {
            await client.ConnectAsync(host!, port, cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception exception) when (exception is SocketException or OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("TCP dependency is unavailable.", exception);
        }
    }
}

public sealed class UnconfiguredHealthCheck(string description) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(HealthCheckResult.Unhealthy(description));
}
