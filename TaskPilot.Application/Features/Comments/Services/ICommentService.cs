using TaskPilot.Application.Features.Comments.Dtos;

namespace TaskPilot.Application.Features.Comments.Services;

public interface ICommentService
{
    Task<ServiceResult<List<CommentResponse>>> GetCommentsAsync(int taskId, CancellationToken cancellationToken);
    Task<ServiceResult<CommentResponse>> CreateCommentAsync(int taskId, CreateCommentRequest request, CancellationToken cancellationToken);
    Task<ServiceResult> UpdateCommentAsync(int commentId, UpdateCommentRequest request, CancellationToken cancellationToken);
    Task<ServiceResult> DeleteCommentAsync(int commentId, CancellationToken cancellationToken);
}
