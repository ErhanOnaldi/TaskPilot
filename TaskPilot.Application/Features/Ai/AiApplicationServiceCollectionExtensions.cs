using Microsoft.Extensions.DependencyInjection;

namespace TaskPilot.Application.Features.Ai;

public static class AiApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddAiApplication(this IServiceCollection services)
    {
        services.AddScoped<IAiSuggestionService, AiSuggestionService>();
        services.AddScoped<IAiSuggestionProcessor, AiSuggestionProcessor>();
        services.AddSingleton<IAiInputGuard, AiInputGuard>();
        services.AddSingleton<IAiOutputGuard, AiOutputGuard>();
        return services;
    }
}
