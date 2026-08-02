using Microsoft.Extensions.DependencyInjection;
using TaskPilot.Application.Features.Knowledge.ReadModels;
using TaskPilot.Application.Features.Knowledge.Repositories;

namespace TaskPilot.Persistence.Features.Knowledge;

public static class KnowledgePersistenceExtensions
{
    public static IServiceCollection AddKnowledgePersistence(this IServiceCollection services) => services
        .AddScoped<IKnowledgeFolderRepository, KnowledgeFolderRepository>()
        .AddScoped<INoteRepository, KnowledgeNoteRepository>()
        .AddScoped<INoteRevisionRepository, KnowledgeNoteRevisionRepository>()
        .AddScoped<INoteLinkRepository, KnowledgeNoteLinkRepository>()
        .AddScoped<IKnowledgeTagRepository, KnowledgeTagRepository>()
        .AddScoped<ITaskNoteLinkRepository, KnowledgeTaskNoteLinkRepository>()
        .AddScoped<IKnowledgeTaskNoteScopePort, KnowledgeTaskNoteScopeRepository>()
        .AddScoped<IKnowledgeScopeValidationPort, KnowledgeScopeValidationRepository>()
        .AddScoped<IKnowledgeReadPort, KnowledgeReadRepository>();
}
