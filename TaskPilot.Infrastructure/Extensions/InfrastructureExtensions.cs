using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using MassTransit;
using TaskPilot.Application.Events;
using TaskPilot.Application.Authorization.Abstractions;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Infrastructure.Caching;
using TaskPilot.Application.Interfaces.Infrastructure.Messaging;
using TaskPilot.Application.Interfaces.Security;
using TaskPilot.Infrastructure.Authorization.Handlers;
using TaskPilot.Infrastructure.Authorization.Services;
using TaskPilot.Infrastructure.Caching;
using TaskPilot.Infrastructure.Messaging;
using TaskPilot.Infrastructure.Messaging.Consumers;
using TaskPilot.Infrastructure.Security;

namespace TaskPilot.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
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
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
        });
        services.AddSingleton<ICacheService, RedisCacheService>();
        services.AddScoped<IDashboardCacheInvalidator, DashboardCacheInvalidator>();
        services.Configure<RabbitMqOptions>(
            configuration.GetSection("RabbitMq"));
        services.AddScoped<IEventPublisher, MassTransitEventPublisher>();
        services.AddMassTransit(busRegistrationConfigurator =>
        {
            busRegistrationConfigurator.AddConsumer<NotificationConsumer>();
            busRegistrationConfigurator.SetKebabCaseEndpointNameFormatter();
            busRegistrationConfigurator.UsingRabbitMq((context, rabbitMqBusFactoryConfigurator) =>
            {
                var rabbitMqOptions = configuration.GetSection("RabbitMq").Get<RabbitMqOptions>() ?? new RabbitMqOptions();
                var virtualHost = Uri.EscapeDataString(rabbitMqOptions.VirtualHost);
                rabbitMqBusFactoryConfigurator.Host(
                    new Uri($"rabbitmq://{rabbitMqOptions.HostName}:{rabbitMqOptions.Port}/{virtualHost}"),
                    hostConfigurator =>
                    {
                        hostConfigurator.Username(rabbitMqOptions.UserName);
                        hostConfigurator.Password(rabbitMqOptions.Password);
                    });

                rabbitMqBusFactoryConfigurator.Message<TaskCreatedEvent>(messageConfigurator => messageConfigurator.SetEntityName("task.created"));
                rabbitMqBusFactoryConfigurator.Message<TaskAssignedEvent>(messageConfigurator => messageConfigurator.SetEntityName("task.assigned"));
                rabbitMqBusFactoryConfigurator.Message<CommentAddedEvent>(messageConfigurator => messageConfigurator.SetEntityName("comment.added"));

                rabbitMqBusFactoryConfigurator.ReceiveEndpoint("taskpilot.notifications", endpointConfigurator =>
                {
                    endpointConfigurator.PrefetchCount = 10;
                    endpointConfigurator.ConcurrentMessageLimit = 5;
                    endpointConfigurator.UseMessageRetry(retryConfigurator =>
                        retryConfigurator.Interval(3, TimeSpan.FromSeconds(5)));
                    endpointConfigurator.ConfigureConsumer<NotificationConsumer>(context);
                });
            });
        });
        return services;
    }
}
