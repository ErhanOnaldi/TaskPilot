namespace TaskPilot.Application.Features.Knowledge.Dtos;

public sealed record CreateKnowledgeFolderRequest(string Name, string Slug, int? ProjectId, int? ParentFolderId);
