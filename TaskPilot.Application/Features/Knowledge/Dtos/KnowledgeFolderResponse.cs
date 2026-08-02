namespace TaskPilot.Application.Features.Knowledge.Dtos;

public sealed record KnowledgeFolderResponse(int Id, int WorkspaceId, int? ProjectId, int? ParentFolderId, string Name, string Slug, DateTime UpdatedAt);
