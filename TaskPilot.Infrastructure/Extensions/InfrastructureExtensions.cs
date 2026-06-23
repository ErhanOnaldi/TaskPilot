using Microsoft.Extensions.DependencyInjection;
using TaskPilot.Application.Interfaces.Security;
using TaskPilot.Infrastructure.Security;

namespace TaskPilot.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IPasswordHasher, PasswordHasherService>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
