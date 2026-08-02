namespace TaskPilot.Application.Features.Knowledge.Dtos;

public sealed record UpdateKnowledgeFolderRequest(string Name, string Slug, int? ProjectId, int? ParentFolderId);
