namespace TaskPilot.Application.Features.Knowledge.Dtos;

public sealed record CreateNoteRequest(string Title, string Slug, string? Content, int? ProjectId, int? FolderId);
