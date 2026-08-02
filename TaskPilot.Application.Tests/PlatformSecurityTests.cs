using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using TaskPilot.API.Features.Platform.Correlation;
using TaskPilot.API.Features.Platform.Options;
using TaskPilot.Domain.Options;
using TaskPilot.Infrastructure.Messaging;

namespace TaskPilot.Application.Tests;

public class PlatformSecurityTests
{
    [Fact]
    public async Task CorrelationMiddleware_uses_safe_incoming_id_for_response_scope_and_activity()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "request-42_abc";
        string? observedCorrelationId = null;
        string? observedActivityTag = null;
        var parentActivity = new Activity("test").Start();
        var middleware = new CorrelationIdMiddleware(
            next: httpContext =>
            {
                observedCorrelationId = CorrelationIdMiddleware.GetCorrelationId(httpContext);
                observedActivityTag = Activity.Current?.GetTagItem("correlation.id") as string;
                return Task.CompletedTask;
            },
            NullLogger<CorrelationIdMiddleware>.Instance);

        try
        {
            await middleware.InvokeAsync(context);
        }
        finally
        {
            parentActivity.Stop();
        }

        Assert.Equal("request-42_abc", context.Response.Headers[CorrelationIdMiddleware.HeaderName]);
        Assert.Equal("request-42_abc", observedCorrelationId);
        Assert.Equal("request-42_abc", observedActivityTag);
    }

    [Fact]
    public async Task CorrelationMiddleware_replaces_unsafe_incoming_id()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "unsafe value";
        var middleware = new CorrelationIdMiddleware(
            _ => Task.CompletedTask,
            NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        var correlationId = Assert.Single(context.Response.Headers[CorrelationIdMiddleware.HeaderName]);
        Assert.Matches("^[a-f0-9]{32}$", correlationId);
        Assert.Equal(correlationId, CorrelationIdMiddleware.GetCorrelationId(context));
    }

    [Fact]
    public void ProblemDetails_helper_adds_request_correlation_id_without_overwriting_existing_value()
    {
        var context = new DefaultHttpContext();
        context.Items[CorrelationIdMiddleware.ItemKey] = "request-42";
        var problemDetails = new ProblemDetails();

        problemDetails.WithCorrelationId(context);

        Assert.Equal("request-42", problemDetails.Extensions["correlationId"]);
        problemDetails.Extensions["correlationId"] = "explicit-value";
        problemDetails.WithCorrelationId(context);
        Assert.Equal("explicit-value", problemDetails.Extensions["correlationId"]);
    }

    [Fact]
    public void JwtValidator_rejects_short_secret_without_disclosing_it()
    {
        var result = new JwtOptionsValidator().Validate(null, new JwtOptions
        {
            Issuer = "taskpilot",
            Audience = "taskpilot-client",
            Secret = "too-short",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        });

        Assert.True(result.Failed);
        Assert.DoesNotContain("too-short", string.Join(" ", result.Failures ?? []));
    }

    [Fact]
    public void JwtValidator_accepts_connection_critical_values()
    {
        var result = new JwtOptionsValidator().Validate(null, new JwtOptions
        {
            Issuer = "taskpilot",
            Audience = "taskpilot-client",
            Secret = new string('x', 32),
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void RabbitMqValidator_rejects_invalid_port_and_never_discloses_password()
    {
        var result = new RabbitMqOptionsValidator().Validate(null, new RabbitMqOptions
        {
            HostName = "rabbitmq",
            Port = 0,
            UserName = "taskpilot",
            Password = "sensitive-password",
            VirtualHost = "/"
        });

        Assert.True(result.Failed);
        Assert.DoesNotContain("sensitive-password", string.Join(" ", result.Failures ?? []));
    }
}
