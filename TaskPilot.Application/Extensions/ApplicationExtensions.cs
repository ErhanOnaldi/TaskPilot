using Microsoft.Extensions.DependencyInjection;
using TaskPilot.Application.Features.Auth.Services;
using TaskPilot.Application.Features.Comments.Services;
using TaskPilot.Application.Features.Dashboard.Services;
using TaskPilot.Application.Features.Labels.Services;
using TaskPilot.Application.Features.Notifications.Services;
using TaskPilot.Application.Features.Project.Services;
using TaskPilot.Application.Features.ProjectMembers.Services;
using TaskPilot.Application.Features.Tasks.Services;
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
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IProjectMemberService, ProjectMemberService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<ILabelService, LabelService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<INotificationService, NotificationService>();
        return services;
    }
    
}
