using System.Net;
using AutoMapper;
using TaskPilot.Application.Authorization.Abstractions;
using TaskPilot.Application.Authorization.Enums;
using TaskPilot.Application.Common.Pagination;
using TaskPilot.Application.Features.Tasks.Dtos;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Application.Interfaces.Persistence.Project;
using TaskPilot.Application.Interfaces.Persistence.Tasks;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.Tasks.Services;

public class TaskService(
    ITaskRepository taskRepository,
    IProjectMemberRepository projectMemberRepository,
    IUnitOfWork unitOfWork,
    IAccessControlService accessControlService,
    IMapper mapper) : ITaskService
{
    public async Task<ServiceResult<PagedResponse<TaskResponse>>> GetTasksAsync(
        int projectId,
        TaskQueryParameters query,
        CancellationToken cancellationToken)
    {
        var access = await accessControlService.AuthorizeProjectAsync(
            projectId,
            ProjectAccessLevel.Read,
            requireActiveProject: false,
            cancellationToken);
        if (access.Failure is not null)
        {
            return ServiceResult<PagedResponse<TaskResponse>>.Fail(access.Failure.ErrorMessages!, access.Failure.Status);
        }

        var tasks = await taskRepository.GetTasksByProjectIdAsync(projectId, query, cancellationToken);
        var response = PagedResponse<TaskResponse>.Create(
            mapper.Map<List<TaskResponse>>(tasks.Items),
            tasks.PageNumber,
            tasks.PageSize,
            tasks.TotalCount);

        return ServiceResult<PagedResponse<TaskResponse>>.Success(response);
    }

    public async Task<ServiceResult<TaskResponse>> GetTaskAsync(int taskId, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(taskId);
        if (task is null) return ServiceResult<TaskResponse>.Fail("Task not found.", HttpStatusCode.NotFound);

        var access = await accessControlService.AuthorizeProjectAsync(
            task.ProjectId,
            ProjectAccessLevel.Read,
            requireActiveProject: false,
            cancellationToken);
        if (access.Failure is not null)
        {
            return ServiceResult<TaskResponse>.Fail(access.Failure.ErrorMessages!, access.Failure.Status);
        }

        return ServiceResult<TaskResponse>.Success(mapper.Map<TaskResponse>(task));
    }

    public async Task<ServiceResult<TaskResponse>> CreateTaskAsync(int projectId, CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var access = await accessControlService.AuthorizeProjectAsync(
            projectId,
            ProjectAccessLevel.Participant,
            requireActiveProject: true,
            cancellationToken);
        if (access.Failure is not null)
        {
            return access.Failure.Status == HttpStatusCode.Forbidden
                ? ServiceResult<TaskResponse>.Fail("Only project members can create tasks.", HttpStatusCode.Forbidden)
                : ServiceResult<TaskResponse>.Fail(access.Failure.ErrorMessages!, access.Failure.Status);
        }

        if (request.AssignedUserId.HasValue && !await projectMemberRepository.IsProjectMemberAsync(projectId, request.AssignedUserId.Value, cancellationToken))
        {
            return ServiceResult<TaskResponse>.Fail("Assigned user must be a project member.", HttpStatusCode.BadRequest);
        }

        var now = DateTime.UtcNow;
        var task = new TaskItem
        {
            ProjectId = projectId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            DueDate = request.DueDate,
            Priority = request.Priority ?? TaskItemPriority.Medium,
            Status = TaskItemStatus.Todo,
            AssignedUserId = request.AssignedUserId,
            CreatedByUserId = access.CurrentUserId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await taskRepository.AddAsync(task);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult<TaskResponse>.Success(mapper.Map<TaskResponse>(task), HttpStatusCode.Created);
    }

    public async Task<ServiceResult> UpdateTaskAsync(int taskId, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(taskId);
        if (task is null) return ServiceResult.Fail("Task not found.", HttpStatusCode.NotFound);

        var access = await accessControlService.AuthorizeProjectAsync(
            task.ProjectId,
            ProjectAccessLevel.Participant,
            requireActiveProject: true,
            cancellationToken);
        if (access.Failure is not null)
        {
            return access.Failure.Status == HttpStatusCode.Forbidden
                ? ServiceResult.Fail("Only project members can update tasks.", HttpStatusCode.Forbidden)
                : access.Failure;
        }

        if (request.AssignedUserId.HasValue && !await projectMemberRepository.IsProjectMemberAsync(task.ProjectId, request.AssignedUserId.Value, cancellationToken))
        {
            return ServiceResult.Fail("Assigned user must be a project member.", HttpStatusCode.BadRequest);
        }

        task.Title = request.Title.Trim();
        task.Description = request.Description?.Trim();
        task.DueDate = request.DueDate;
        task.Priority = request.Priority;
        task.AssignedUserId = request.AssignedUserId;
        task.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    public async Task<ServiceResult> DeleteTaskAsync(int taskId, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(taskId);
        if (task is null) return ServiceResult.Fail("Task not found.", HttpStatusCode.NotFound);

        var access = await accessControlService.AuthorizeProjectAsync(
            task.ProjectId,
            ProjectAccessLevel.Manage,
            requireActiveProject: true,
            cancellationToken);
        if (access.Failure is not null)
        {
            return access.Failure.Status == HttpStatusCode.Forbidden
                ? ServiceResult.Fail("Only workspace owner or project manager can delete tasks.", HttpStatusCode.Forbidden)
                : access.Failure;
        }

        taskRepository.Delete(task);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    public async Task<ServiceResult> UpdateStatusAsync(int taskId, UpdateTaskStatusRequest request, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(taskId);
        if (task is null) return ServiceResult.Fail("Task not found.", HttpStatusCode.NotFound);

        var access = await accessControlService.AuthorizeProjectAsync(
            task.ProjectId,
            ProjectAccessLevel.Participant,
            requireActiveProject: true,
            cancellationToken);
        if (access.Failure is not null)
        {
            return access.Failure.Status == HttpStatusCode.Forbidden
                ? ServiceResult.Fail("Only project members can update task status.", HttpStatusCode.Forbidden)
                : access.Failure;
        }

        task.Status = request.Status;
        task.CompletedAt = request.Status == TaskItemStatus.Done ? DateTime.UtcNow : null;
        task.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    public async Task<ServiceResult> AssignTaskAsync(int taskId, AssignTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(taskId);
        if (task is null) return ServiceResult.Fail("Task not found.", HttpStatusCode.NotFound);

        var access = await accessControlService.AuthorizeProjectAsync(
            task.ProjectId,
            ProjectAccessLevel.Manage,
            requireActiveProject: true,
            cancellationToken);
        if (access.Failure is not null)
        {
            return access.Failure.Status == HttpStatusCode.Forbidden
                ? ServiceResult.Fail("Only workspace owner or project manager can assign tasks.", HttpStatusCode.Forbidden)
                : access.Failure;
        }

        if (request.AssignedUserId.HasValue && !await projectMemberRepository.IsProjectMemberAsync(task.ProjectId, request.AssignedUserId.Value, cancellationToken))
        {
            return ServiceResult.Fail("Assigned user must be a project member.", HttpStatusCode.BadRequest);
        }

        task.AssignedUserId = request.AssignedUserId;
        task.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success(HttpStatusCode.NoContent);
    }
}
