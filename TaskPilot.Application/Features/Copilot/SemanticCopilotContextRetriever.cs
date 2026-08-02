using TaskPilot.Application.Features.Semantic.Contracts;
using TaskPilot.Domain.AI.Copilot;
using TaskPilot.Domain.AI.Semantic;

namespace TaskPilot.Application.Features.Copilot;

/// <summary>
/// Performs authorization-first retrieval: the database query receives the already constrained
/// tenant/project scope before vector distance is evaluated.
/// </summary>
public sealed class SemanticCopilotContextRetriever(
    ISemanticDocumentRepository documents,
    IEmbeddingGenerator embeddings) : ICopilotContextRetriever
{
    public async Task<IReadOnlyList<CopilotContextItem>> RetrieveAsync(
        CopilotAuthorizedScope scope,
        string query,
        CancellationToken cancellationToken)
    {
        var embedding = await embeddings.GenerateAsync(query, cancellationToken);
        var semanticScope = new SemanticSearchScope(
            scope.ExecutionScope.WorkspaceId,
            scope.AuthorizedProjectIds,
            scope.IncludeWorkspaceWideDocuments);
        var candidates = await documents.SearchScopedAsync(
            semanticScope,
            query,
            embedding.Vector,
            8,
            cancellationToken);

        return candidates
            .Where(candidate => candidate.SourceType is SemanticSourceType.Task or SemanticSourceType.Note)
            .Select(candidate => new CopilotContextItem(
                new CopilotCitation(
                    candidate.SourceType == SemanticSourceType.Task
                        ? CopilotCitationType.Task
                        : CopilotCitationType.Note,
                    candidate.SourceId),
                candidate.Content))
            .ToList();
    }
}
