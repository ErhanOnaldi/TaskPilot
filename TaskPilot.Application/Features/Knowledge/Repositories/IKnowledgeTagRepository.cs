using TaskPilot.Domain.Knowledge;

namespace TaskPilot.Application.Features.Knowledge.Repositories;

public interface IKnowledgeTagRepository
{
    Task<bool> ExistsBySlugAsync(int workspaceId, int? projectId, string normalizedSlug, CancellationToken cancellationToken);
    ValueTask AddAsync(KnowledgeTag tag, CancellationToken cancellationToken);
}
