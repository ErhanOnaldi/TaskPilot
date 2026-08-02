using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using Microsoft.Agents.AI.Hosting;
using TaskPilot.Infrastructure.Features.Interop;

namespace TaskPilot.API.Features.Interop;

public static class InteropApiExtensions
{
    public const string PatScheme = "TaskPilotPat";
    public const string McpAccessPolicy = "McpAccess";

    public static IServiceCollection AddInteropApi(this IServiceCollection services)
    {
        services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, PatAuthenticationHandler>(PatScheme, _ => { });
        services.AddAuthorization(options => options.AddPolicy(McpAccessPolicy, policy =>
        {
            policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, PatScheme);
            policy.RequireAuthenticatedUser();
        }));
        services.AddMcpServer()
            .WithHttpTransport(options => options.Stateless = true)
            .WithTools<TaskPilotMcpTools>();
        services.AddAIAgent(
            "taskpilot-copilot",
            (provider, _) => new TaskPilotCopilotAgent(provider.GetRequiredService<IServiceScopeFactory>()))
            .WithSessionStore(
                (provider, _) => new RedisAgentSessionStore(
                    provider.GetRequiredService<Microsoft.Extensions.Caching.Distributed.IDistributedCache>()),
                ServiceLifetime.Singleton);
        services.AddA2AServer("taskpilot-copilot");
        services.UseClaimsBasedSessionIsolation();
        return services;
    }
}

internal sealed class PatAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = Request.Headers["X-TaskPilot-PAT"].FirstOrDefault();
        var expectedHash = configuration["Features:Interop:Pat:Sha256"];
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(expectedHash))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        byte[] expected;
        try { expected = Convert.FromHexString(expectedHash); }
        catch (FormatException) { return Task.FromResult(AuthenticateResult.Fail("PAT configuration is invalid.")); }
        var actual = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        if (!CryptographicOperations.FixedTimeEquals(actual, expected))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid personal access token."));
        }

        if (!int.TryParse(configuration["Features:Interop:Pat:UserId"], out var userId) || userId <= 0)
        {
            return Task.FromResult(AuthenticateResult.Fail("PAT user scope is invalid."));
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString(System.Globalization.CultureInfo.InvariantCulture))],
            Scheme.Name));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }
}
