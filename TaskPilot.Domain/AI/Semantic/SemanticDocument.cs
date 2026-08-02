namespace TaskPilot.Domain.AI.Semantic;

public enum SemanticSourceType { Task, Comment, Note }

public sealed class SemanticDocument
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public int? ProjectId { get; set; }
    public SemanticSourceType SourceType { get; set; }
    public int SourceId { get; set; }
    public int ChunkIndex { get; set; }
    public string SourceText { get; set; } = null!;
    public string ContentHash { get; set; } = null!;
    public float[]? Embedding { get; set; }
    public string EmbeddingModel { get; set; } = null!;
    public int Dimensions { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; }
}
