using System.Net;
using AutoMapper;
using TaskPilot.Application.Authorization;
using TaskPilot.Application.Features.Comments.Dtos;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.Comments.Services;

public class CommentService(
    IGenericRepository<Comment> commentRepository,
    IGenericRepository<TaskItem> taskRepository,
    IUnitOfWork unitOfWork,
    IAccessControlService accessControlService,
    IMapper mapper) : ICommentService
{
    public async Task<ServiceResult<List<CommentResponse>>> GetCommentsAsync(int taskId, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(taskId);
        if (task is null) return ServiceResult<List<CommentResponse>>.Fail("Task not found.", HttpStatusCode.NotFound);
        var access = await accessControlService.AuthorizeProjectAsync(
            task.ProjectId,
            ProjectAccessLevel.Read,
            requireActiveProject: false,
            cancellationToken);
        if (access.Failure is not null)
        {
            return ServiceResult<List<CommentResponse>>.Fail(access.Failure.ErrorMessages!, access.Failure.Status);
        }

        var comments = commentRepository.Where(x => x.TaskId == taskId).OrderBy(x => x.CreatedAt).ToList();
        return ServiceResult<List<CommentResponse>>.Success(mapper.Map<List<CommentResponse>>(comments));
    }

    public async Task<ServiceResult<CommentResponse>> CreateCommentAsync(int taskId, CreateCommentRequest request, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(taskId);
        if (task is null) return ServiceResult<CommentResponse>.Fail("Task not found.", HttpStatusCode.NotFound);
        var access = await accessControlService.AuthorizeProjectAsync(
            task.ProjectId,
            ProjectAccessLevel.Participant,
            requireActiveProject: true,
            cancellationToken);
        if (access.Failure is not null)
        {
            return access.Failure.Status == HttpStatusCode.Forbidden
                ? ServiceResult<CommentResponse>.Fail("Only project members can comment.", HttpStatusCode.Forbidden)
                : ServiceResult<CommentResponse>.Fail(access.Failure.ErrorMessages!, access.Failure.Status);
        }

        var now = DateTime.UtcNow;
        var comment = new Comment
        {
            TaskId = taskId,
            UserId = access.CurrentUserId,
            Content = request.Content.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
        await commentRepository.AddAsync(comment);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult<CommentResponse>.Success(mapper.Map<CommentResponse>(comment), HttpStatusCode.Created);
    }

    public async Task<ServiceResult> UpdateCommentAsync(int commentId, UpdateCommentRequest request, CancellationToken cancellationToken)
    {
        var comment = await commentRepository.GetByIdAsync(commentId);
        if (comment is null) return ServiceResult.Fail("Comment not found.", HttpStatusCode.NotFound);
        var task = await taskRepository.GetByIdAsync(comment.TaskId);
        if (task is null) return ServiceResult.Fail("Task not found.", HttpStatusCode.NotFound);

        var access = await accessControlService.AuthorizeProjectAsync(
            task.ProjectId,
            ProjectAccessLevel.Read,
            requireActiveProject: true,
            cancellationToken);
        if (access.Failure is not null) return access.Failure;

        if (comment.UserId != access.CurrentUserId)
        {
            var manageAccess = await accessControlService.AuthorizeProjectAsync(
                task.ProjectId,
                ProjectAccessLevel.Manage,
                requireActiveProject: true,
                cancellationToken);
            if (manageAccess.Failure is not null)
            {
                return manageAccess.Failure.Status == HttpStatusCode.Forbidden
                    ? ServiceResult.Fail("Only comment author, workspace owner, or project manager can update comment.", HttpStatusCode.Forbidden)
                    : manageAccess.Failure;
            }
        }

        comment.Content = request.Content.Trim();
        comment.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    public async Task<ServiceResult> DeleteCommentAsync(int commentId, CancellationToken cancellationToken)
    {
        var comment = await commentRepository.GetByIdAsync(commentId);
        if (comment is null) return ServiceResult.Fail("Comment not found.", HttpStatusCode.NotFound);
        var task = await taskRepository.GetByIdAsync(comment.TaskId);
        if (task is null) return ServiceResult.Fail("Task not found.", HttpStatusCode.NotFound);

        var access = await accessControlService.AuthorizeProjectAsync(
            task.ProjectId,
            ProjectAccessLevel.Read,
            requireActiveProject: true,
            cancellationToken);
        if (access.Failure is not null) return access.Failure;

        if (comment.UserId != access.CurrentUserId)
        {
            var manageAccess = await accessControlService.AuthorizeProjectAsync(
                task.ProjectId,
                ProjectAccessLevel.Manage,
                requireActiveProject: true,
                cancellationToken);
            if (manageAccess.Failure is not null)
            {
                return manageAccess.Failure.Status == HttpStatusCode.Forbidden
                    ? ServiceResult.Fail("Only comment author, workspace owner, or project manager can delete comment.", HttpStatusCode.Forbidden)
                    : manageAccess.Failure;
            }
        }

        commentRepository.Delete(comment);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success(HttpStatusCode.NoContent);
    }
}
