namespace TaskPilot.Application.Features.Knowledge.Dtos;

public sealed record NoteResponse(int Id, int WorkspaceId, int? ProjectId, int? FolderId, string Title, string Slug, string Content, int Version, DateTime UpdatedAt);
