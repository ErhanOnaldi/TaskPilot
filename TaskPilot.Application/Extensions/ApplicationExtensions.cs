using Microsoft.Extensions.DependencyInjection;
using TaskPilot.Application.Features.Auth.Services;
using TaskPilot.Application.Features.Workspace.Services;

namespace TaskPilot.Application.Extensions;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IWorkspaceService, WorkspaceService>();
        return services;
    }
    
}