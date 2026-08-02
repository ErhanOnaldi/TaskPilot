namespace TaskPilot.Application.Features.Knowledge.ReadModels;

public sealed record NoteLinkReadModel(int SourceNoteId, int? TargetNoteId, string TargetSlug, string? Alias, bool IsResolved);
public sealed record NoteRevisionReadModel(int Id, int RevisionNumber, string Title, string Slug, string Content, DateTime CreatedAt, int? RestoredFromRevisionId);
public sealed record KnowledgeSearchItem(int NoteId, string Title, string Slug, string Excerpt, DateTime UpdatedAt);
public sealed record KnowledgeGraphNode(int NoteId, string Title, string Slug);
public sealed record KnowledgeGraphEdge(int SourceNoteId, int? TargetNoteId, string TargetSlug);
public sealed record KnowledgeGraphReadModel(IReadOnlyList<KnowledgeGraphNode> Nodes, IReadOnlyList<KnowledgeGraphEdge> Edges);
