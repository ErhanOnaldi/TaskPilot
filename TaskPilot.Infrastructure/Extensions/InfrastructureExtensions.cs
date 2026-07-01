using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using TaskPilot.Application.Authorization.Abstractions;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Security;
using TaskPilot.Infrastructure.Authorization.Handlers;
using TaskPilot.Infrastructure.Authorization.Services;
using TaskPilot.Infrastructure.Security;

namespace TaskPilot.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IPasswordHasher, PasswordHasherService>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddScoped<IRefreshTokenHasher, RefreshTokenHasher>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAccessControlService, AccessControlService>();
        services.AddScoped<IAuthorizationHandler, WorkspaceAccessHandler>();
        services.AddScoped<IAuthorizationHandler, ProjectAccessHandler>();
        
        return services;
    }
}
