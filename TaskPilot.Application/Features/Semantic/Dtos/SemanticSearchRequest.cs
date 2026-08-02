namespace TaskPilot.Application.Features.Semantic.Dtos;

public enum SemanticSearchMode { Text, Semantic, Hybrid }
public sealed record SemanticSearchRequest(string Query, SemanticSearchMode Mode = SemanticSearchMode.Hybrid, int Limit = 20);
public sealed record SemanticSearchResponse(int DocumentId, TaskPilot.Domain.AI.Semantic.SemanticSourceType SourceType, int SourceId, int? ProjectId, string Content, float Score);
