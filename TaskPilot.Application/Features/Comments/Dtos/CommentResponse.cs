namespace TaskPilot.Application.Features.Comments.Dtos;

public sealed record CommentResponse(
    int Id,
    int TaskId,
    int UserId,
    string Content,
    DateTime CreatedAt,
    DateTime UpdatedAt);
