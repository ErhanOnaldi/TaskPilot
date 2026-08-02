using Microsoft.Extensions.DependencyInjection;
using TaskPilot.Application.Features.Knowledge.Services;

namespace TaskPilot.Application.Features.Knowledge;

public static class KnowledgeApplicationExtensions
{
    public static IServiceCollection AddKnowledgeApplication(this IServiceCollection services) => services
        .AddScoped<IKnowledgeNoteService, KnowledgeNoteService>()
        .AddScoped<IKnowledgeAdministrationService, KnowledgeAdministrationService>()
        .AddScoped<IKnowledgeFolderService, KnowledgeFolderService>()
        .AddScoped<IKnowledgeTaskNoteService, KnowledgeTaskNoteService>();
}
