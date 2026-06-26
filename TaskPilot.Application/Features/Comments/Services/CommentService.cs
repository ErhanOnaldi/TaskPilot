using System.Net;
using FluentValidation;
using TaskPilot.Application.Features.Comments.Dtos;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Application.Interfaces.Persistence.Project;
using TaskPilot.Application.Interfaces.Persistence.Workspace;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.Comments.Services;

public class CommentService(
    IGenericRepository<Comment> commentRepository,
    IGenericRepository<TaskItem> taskRepository,
    IProjectRepository projectRepository,
    IProjectMemberRepository projectMemberRepository,
    IWorkspaceMemberRepository workspaceMemberRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IValidator<CreateCommentRequest> createValidator,
    IValidator<UpdateCommentRequest> updateValidator) : ICommentService
{
    public async Task<ServiceResult<List<CommentResponse>>> GetCommentsAsync(int taskId, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(taskId);
        if (task is null) return ServiceResult<List<CommentResponse>>.Fail("Task not found.", HttpStatusCode.NotFound);
        var access = await EnsureProjectReadableAsync(task.ProjectId, cancellationToken);
        if (access is not null) return ServiceResult<List<CommentResponse>>.Fail(access.ErrorMessages!, access.Status);

        var comments = commentRepository.Where(x => x.TaskId == taskId).OrderBy(x => x.CreatedAt).ToList();
        return ServiceResult<List<CommentResponse>>.Success(comments.Select(Map).ToList());
    }

    public async Task<ServiceResult<CommentResponse>> CreateCommentAsync(int taskId, CreateCommentRequest request, CancellationToken cancellationToken)
    {
        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ServiceResult<CommentResponse>.Fail(validation.Errors.Select(x => x.ErrorMessage).ToList(), HttpStatusCode.BadRequest);

        var task = await taskRepository.GetByIdAsync(taskId);
        if (task is null) return ServiceResult<CommentResponse>.Fail("Task not found.", HttpStatusCode.NotFound);
        var access = await EnsureProjectParticipantAsync(task.ProjectId, cancellationToken);
        if (access is not null) return ServiceResult<CommentResponse>.Fail(access.ErrorMessages!, access.Status);

        var now = DateTime.UtcNow;
        var comment = new Comment
        {
            TaskId = taskId,
            UserId = currentUserService.UserId,
            Content = request.Content.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
        await commentRepository.AddAsync(comment);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult<CommentResponse>.Success(Map(comment), HttpStatusCode.Created);
    }

    public async Task<ServiceResult> UpdateCommentAsync(int commentId, UpdateCommentRequest request, CancellationToken cancellationToken)
    {
        var validation = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ServiceResult.Fail(validation.Errors.Select(x => x.ErrorMessage).ToList(), HttpStatusCode.BadRequest);

        var comment = await commentRepository.GetByIdAsync(commentId);
        if (comment is null) return ServiceResult.Fail("Comment not found.", HttpStatusCode.NotFound);
        var task = await taskRepository.GetByIdAsync(comment.TaskId);
        if (task is null) return ServiceResult.Fail("Task not found.", HttpStatusCode.NotFound);

        var canManage = await CanManageProjectAsync(task.ProjectId, cancellationToken);
        if (comment.UserId != currentUserService.UserId && !canManage)
        {
            return ServiceResult.Fail("Only comment author, workspace owner, or project manager can update comment.", HttpStatusCode.Forbidden);
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

        var canManage = await CanManageProjectAsync(task.ProjectId, cancellationToken);
        if (comment.UserId != currentUserService.UserId && !canManage)
        {
            return ServiceResult.Fail("Only comment author, workspace owner, or project manager can delete comment.", HttpStatusCode.Forbidden);
        }

        commentRepository.Delete(comment);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    private async Task<ServiceResult?> EnsureProjectReadableAsync(int projectId, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetProjectByIdAsync(projectId, cancellationToken);
        if (project is null) return ServiceResult.Fail("Project not found.", HttpStatusCode.NotFound);
        if (!await workspaceMemberRepository.IsWorkspaceMemberAsync(project.WorkspaceId, currentUserService.UserId, cancellationToken))
        {
            return ServiceResult.Fail("Project not found.", HttpStatusCode.NotFound);
        }
        return null;
    }

    private async Task<ServiceResult?> EnsureProjectParticipantAsync(int projectId, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetProjectByIdAsync(projectId, cancellationToken);
        if (project is null) return ServiceResult.Fail("Project not found.", HttpStatusCode.NotFound);
        if (project.Status == ProjectStatus.Archived) return ServiceResult.Fail("Project is archived.", HttpStatusCode.BadRequest);
        if (!await workspaceMemberRepository.IsWorkspaceMemberAsync(project.WorkspaceId, currentUserService.UserId, cancellationToken))
        {
            return ServiceResult.Fail("Project not found.", HttpStatusCode.NotFound);
        }
        var workspaceMember = await workspaceMemberRepository.GetMemberAsync(project.WorkspaceId, currentUserService.UserId, cancellationToken);
        if (workspaceMember?.Role == Role.Owner) return null;
        if (!await projectMemberRepository.IsProjectMemberAsync(projectId, currentUserService.UserId, cancellationToken))
        {
            return ServiceResult.Fail("Only project members can comment.", HttpStatusCode.Forbidden);
        }
        return null;
    }

    private async Task<bool> CanManageProjectAsync(int projectId, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetProjectByIdAsync(projectId, cancellationToken);
        if (project is null) return false;
        var workspaceMember = await workspaceMemberRepository.GetMemberAsync(project.WorkspaceId, currentUserService.UserId, cancellationToken);
        return workspaceMember?.Role == Role.Owner ||
               await projectMemberRepository.IsProjectManagerAsync(projectId, currentUserService.UserId, cancellationToken);
    }

    private static CommentResponse Map(Comment comment)
    {
        return new CommentResponse(comment.Id, comment.TaskId, comment.UserId, comment.Content, comment.CreatedAt, comment.UpdatedAt);
    }
}
