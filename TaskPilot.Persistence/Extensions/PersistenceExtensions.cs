using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Application.Interfaces.Persistence.Auth;
using TaskPilot.Application.Interfaces.Persistence.Comments;
using TaskPilot.Application.Interfaces.Persistence.Dashboard;
using TaskPilot.Application.Interfaces.Persistence.Labels;
using TaskPilot.Application.Interfaces.Persistence.Notifications;
using TaskPilot.Application.Interfaces.Persistence.Project;
using TaskPilot.Application.Interfaces.Persistence.Tasks;
using TaskPilot.Application.Interfaces.Persistence.User;
using TaskPilot.Application.Interfaces.Persistence.Workspace;
using TaskPilot.Application.Interfaces.Infrastructure.Messaging;
using TaskPilot.Application.Interfaces.Persistence.Messaging;
using TaskPilot.Persistence.EntityRepositories;
using TaskPilot.Persistence.Interceptors;
using TaskPilot.Persistence.Messaging;
using Pgvector.EntityFrameworkCore;
using TaskPilot.Application.Features.Notifications.Services;
using TaskPilot.Persistence.Features.Notifications;

namespace TaskPilot.Persistence.Extensions;

public static class PersistenceExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<AuditableEntitySaveChangesInterceptor>();
        var connectionString = PostgreSqlConnectionString.Normalize(
            configuration.GetConnectionString("PostgreSql"));

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(
                connectionString,
                npgsqlOptions =>
                {
                    npgsqlOptions.UseVector();
                    npgsqlOptions.MigrationsAssembly(
                        typeof(AppDbContext).Assembly.FullName);
                });

            options.AddInterceptors(serviceProvider.GetRequiredService<AuditableEntitySaveChangesInterceptor>());
        });
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEventOutbox, PersistenceEventOutbox>();
        services.AddScoped<IOutboxMessageRepository, OutboxMessageRepository>();
        services.AddScoped<IInboxMessageRepository, InboxMessageRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
        services.AddScoped<IWorkspaceMemberRepository, WorkspaceMemberRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IProjectMemberRepository, ProjectMemberRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IDeadlineReminderPort, DeadlineReminderPort>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<ILabelRepository, LabelRepository>();
        services.AddScoped<ITaskLabelRepository, TaskLabelRepository>();
        return services;
    }
    
}
