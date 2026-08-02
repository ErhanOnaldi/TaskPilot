namespace TaskPilot.Application.Features.Knowledge.Dtos;

public sealed record CreateKnowledgeTagRequest(string Name, string Slug, int? ProjectId);
