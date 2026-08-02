namespace TaskPilot.Application.Features.Knowledge.Dtos;

public sealed record UpdateNoteRequest(string Title, string Slug, string? Content, int? ProjectId, int? FolderId, int ExpectedVersion);
