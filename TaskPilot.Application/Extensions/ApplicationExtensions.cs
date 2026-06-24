using Microsoft.Extensions.DependencyInjection;
using TaskPilot.Application.Features.Auth.Services;
using TaskPilot.Application.Features.Workspace.Services;
using TaskPilot.Application.Features.WorkspaceMembers.Services;

namespace TaskPilot.Application.Extensions;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IWorkspaceService, WorkspaceService>();
        services.AddScoped<IWorkspaceMemberService, WorkspaceMemberService>();
        return services;
    }
    
}