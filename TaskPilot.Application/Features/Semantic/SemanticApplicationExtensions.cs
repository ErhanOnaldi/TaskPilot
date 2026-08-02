using Microsoft.Extensions.DependencyInjection;
using TaskPilot.Application.Features.Semantic.Contracts;
using TaskPilot.Application.Features.Semantic.Services;

namespace TaskPilot.Application.Features.Semantic;

public static class SemanticApplicationExtensions
{
    public static IServiceCollection AddSemanticApplication(this IServiceCollection services) => services
        .AddScoped<ISemanticIndexingService, SemanticIndexingService>()
        .AddScoped<ISemanticSearchService, SemanticSearchService>()
        .AddScoped<ISemanticBackfillService, SemanticBackfillService>()
        .AddScoped<ITaskDuplicateDetector, TaskDuplicateDetector>();
}
