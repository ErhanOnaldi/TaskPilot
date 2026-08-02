using System.Text;
using Microsoft.Extensions.Options;
using TaskPilot.Domain.Options;
using TaskPilot.Infrastructure.Messaging;

namespace TaskPilot.API.Features.Platform.Options;

/// <summary>
/// Fails application startup when security-sensitive connection settings are missing or unsafe.
/// Validation messages deliberately never include option values.
/// </summary>
public static class ConnectionCriticalOptionsValidation
{
    public static IServiceCollection AddConnectionCriticalOptionsValidation(this IServiceCollection services)
    {
        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
        services.AddSingleton<IValidateOptions<RabbitMqOptions>, RabbitMqOptionsValidator>();
        services.AddOptions<JwtOptions>().ValidateOnStart();
        services.AddOptions<RabbitMqOptions>().ValidateOnStart();

        return services;
    }
}

public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            failures.Add("Jwt:Issuer must be configured.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            failures.Add("Jwt:Audience must be configured.");
        }

        if (string.IsNullOrEmpty(options.Secret) || Encoding.UTF8.GetByteCount(options.Secret) < 32)
        {
            failures.Add("Jwt:Secret must be configured with at least 32 UTF-8 bytes.");
        }

        if (options.AccessTokenExpirationMinutes is < 1 or > 1440)
        {
            failures.Add("Jwt:AccessTokenExpirationMinutes must be between 1 and 1440.");
        }

        if (options.RefreshTokenExpirationDays is < 1 or > 365)
        {
            failures.Add("Jwt:RefreshTokenExpirationDays must be between 1 and 365.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}

public sealed class RabbitMqOptionsValidator : IValidateOptions<RabbitMqOptions>
{
    public ValidateOptionsResult Validate(string? name, RabbitMqOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.HostName))
        {
            failures.Add("RabbitMq:HostName must be configured.");
        }

        if (options.Port is < 1 or > 65535)
        {
            failures.Add("RabbitMq:Port must be between 1 and 65535.");
        }

        if (string.IsNullOrWhiteSpace(options.UserName))
        {
            failures.Add("RabbitMq:UserName must be configured.");
        }

        if (string.IsNullOrWhiteSpace(options.Password))
        {
            failures.Add("RabbitMq:Password must be configured.");
        }

        if (string.IsNullOrWhiteSpace(options.VirtualHost))
        {
            failures.Add("RabbitMq:VirtualHost must be configured.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
