using System.Net;
using AutoMapper;
using TaskPilot.Application.Authorization.Abstractions;
using TaskPilot.Application.Authorization.Enums;
using TaskPilot.Application.Common.Pagination;
using TaskPilot.Application.Events;
using TaskPilot.Application.Features.Tasks.Dtos;
using TaskPilot.Application.Interfaces.Infrastructure.Messaging;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Application.Interfaces.Persistence.Project;
using TaskPilot.Application.Interfaces.Persistence.Tasks;
using TaskPilot.Application.Interfaces.Persistence.Workspace;
using TaskPilot.Domain.Entities;
using TaskPilot.Domain.Policies;
using TaskPilot.Application.Features.Semantic.Contracts;
using TaskPilot.Domain.AI.Semantic;

namespace TaskPilot.Application.Features.Tasks.Services;

public class TaskService(
    ITaskRepository taskRepository,
    IProjectMemberRepository projectMemberRepository,
    IWorkspaceMemberRepository workspaceMemberRepository,
    IUnitOfWork unitOfWork,
    IAccessControlService accessControlService,
    IMapper mapper,
    IEventOutbox eventOutbox,
    IDateTimeProvider dateTimeProvider,
    ITaskDuplicateDetector? duplicateDetector = null) : ITaskService
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

        if (access.ProjectMember?.Role == ProjectRole.TeamMember &&
            request.AssignedUserId.HasValue &&
            request.AssignedUserId.Value != access.CurrentUserId)
        {
            return ServiceResult<TaskResponse>.Fail(
                "Team members cannot assign tasks to other users.",
                HttpStatusCode.Forbidden);
        }

        var assignmentValidation = await ValidateAssigneeAsync(projectId, access.Workspace.Id, request.AssignedUserId, cancellationToken);
        if (assignmentValidation is not null)
        {
            return ServiceResult<TaskResponse>.Fail(assignmentValidation.ErrorMessages!, assignmentValidation.Status);
        }

        var now = dateTimeProvider.UtcNow;
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
        await eventOutbox.EnqueueAsync(
            () => new TaskCreatedEvent(
                EventId: Guid.NewGuid(),
                TaskId: task.Id,
                ProjectId: task.ProjectId,
                CreatedByUserId: access.CurrentUserId,
                AssignedUserId: task.AssignedUserId,
                OccurredAt: dateTimeProvider.UtcNow),
            cancellationToken);
        await EnqueueDashboardInvalidationAsync(task.ProjectId, cancellationToken);
        await EnqueueSemanticChangeAsync(task, access.Workspace.Id, false, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var response = mapper.Map<TaskResponse>(task) with
        {
            DuplicateWarning = await FindDuplicateWarningAsync(task, cancellationToken)
        };
        return ServiceResult<TaskResponse>.Success(response, HttpStatusCode.Created);
    }

    public async Task<ServiceResult> UpdateTaskAsync(int taskId, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(taskId);
        if (task is null) return ServiceResult.Fail("Task not found.", HttpStatusCode.NotFound);
        var previousAssignedUserId = task.AssignedUserId;
        var access = await accessControlService.AuthorizeProjectAsync(
            task.ProjectId,
            ProjectAccessLevel.Manage,
            requireActiveProject: true,
            cancellationToken);
        if (access.Failure is not null)
        {
            return access.Failure.Status == HttpStatusCode.Forbidden
                ? ServiceResult.Fail("Only workspace owner or project manager can update tasks.", HttpStatusCode.Forbidden)
                : access.Failure;
        }

        var assignmentValidation = await ValidateAssigneeAsync(task.ProjectId, access.Workspace.Id, request.AssignedUserId, cancellationToken);
        if (assignmentValidation is not null)
        {
            return assignmentValidation;
        }

        task.Title = request.Title.Trim();
        task.Description = request.Description?.Trim();
        task.DueDate = request.DueDate;
        task.Priority = request.Priority;
        task.AssignedUserId = request.AssignedUserId;
        task.UpdatedAt = dateTimeProvider.UtcNow;

        var assignedUserChanged = previousAssignedUserId != request.AssignedUserId;
        if (assignedUserChanged && request.AssignedUserId.HasValue)
        {
            await eventOutbox.EnqueueAsync(
                () => new TaskAssignedEvent(
                    EventId: Guid.NewGuid(),
                    TaskId: task.Id,
                    ProjectId: task.ProjectId,
                    AssignedUserId: request.AssignedUserId.Value,
                    AssignedByUserId: access.CurrentUserId,
                    OccurredAt: dateTimeProvider.UtcNow),
                cancellationToken);
        }
        await EnqueueDashboardInvalidationAsync(task.ProjectId, cancellationToken);
        await EnqueueSemanticChangeAsync(task, access.Workspace.Id, false, cancellationToken);
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
        await EnqueueDashboardInvalidationAsync(task.ProjectId, cancellationToken);
        await EnqueueSemanticChangeAsync(task, access.Workspace.Id, true, cancellationToken);
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

        if (access.ProjectMember?.Role == ProjectRole.TeamMember && task.AssignedUserId != access.CurrentUserId)
        {
            return ServiceResult.Fail("Team members can update status only for tasks assigned to them.", HttpStatusCode.Forbidden);
        }

        if (!TaskStatusTransitionPolicy.IsAllowed(task.Status, request.Status))
        {
            return ServiceResult.Fail($"Transition from {task.Status} to {request.Status} is not allowed.", HttpStatusCode.BadRequest);
        }

        if (TaskStatusTransitionPolicy.IsCancelledReopen(task.Status, request.Status) &&
            access.WorkspaceMember.Role != Role.Owner &&
            access.ProjectMember?.Role != ProjectRole.ProjectManager)
        {
            return ServiceResult.Fail("Only workspace owner or project manager can reopen a cancelled task.", HttpStatusCode.Forbidden);
        }

        task.Status = request.Status;
        task.CompletedAt = request.Status == TaskItemStatus.Done ? dateTimeProvider.UtcNow : null;
        task.UpdatedAt = dateTimeProvider.UtcNow;
        await EnqueueDashboardInvalidationAsync(task.ProjectId, cancellationToken);
        await EnqueueSemanticChangeAsync(task, access.Workspace.Id, false, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    public async Task<ServiceResult> AssignTaskAsync(int taskId, AssignTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(taskId);
        if (task is null) return ServiceResult.Fail("Task not found.", HttpStatusCode.NotFound);
        var previousAssignedUserId = task.AssignedUserId;
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

        var assignmentValidation = await ValidateAssigneeAsync(task.ProjectId, access.Workspace.Id, request.AssignedUserId, cancellationToken);
        if (assignmentValidation is not null)
        {
            return assignmentValidation;
        }

        task.AssignedUserId = request.AssignedUserId;
        task.UpdatedAt = dateTimeProvider.UtcNow;
        var assignedUserChanged = previousAssignedUserId != request.AssignedUserId;
        if (assignedUserChanged && request.AssignedUserId.HasValue)
        {
            await eventOutbox.EnqueueAsync(
                () => new TaskAssignedEvent(
                    EventId: Guid.NewGuid(),
                    TaskId: task.Id,
                    ProjectId: task.ProjectId,
                    AssignedUserId: request.AssignedUserId.Value,
                    AssignedByUserId: access.CurrentUserId,
                    OccurredAt: dateTimeProvider.UtcNow),
                cancellationToken);
        }
        await EnqueueDashboardInvalidationAsync(task.ProjectId, cancellationToken);
        await EnqueueSemanticChangeAsync(task, access.Workspace.Id, false, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    private Task EnqueueDashboardInvalidationAsync(int projectId, CancellationToken cancellationToken)
    {
        return eventOutbox.EnqueueAsync(
            () => new ProjectDashboardInvalidationRequestedEvent(
                EventId: Guid.NewGuid(),
                ProjectId: projectId,
                OccurredAt: dateTimeProvider.UtcNow),
            cancellationToken);
    }

    private Task EnqueueSemanticChangeAsync(
        TaskItem task,
        int workspaceId,
        bool isDeleted,
        CancellationToken cancellationToken) =>
        eventOutbox.EnqueueAsync(
            () => new SemanticContentChangedEvent(
                Guid.NewGuid(),
                SemanticSourceType.Task,
                task.Id,
                workspaceId,
                task.ProjectId,
                BuildSemanticTaskContent(task),
                dateTimeProvider.UtcNow,
                isDeleted),
            cancellationToken);

    private async Task<DuplicateTaskWarning?> FindDuplicateWarningAsync(TaskItem task, CancellationToken cancellationToken)
    {
        if (duplicateDetector is null) return null;
        try
        {
            return await duplicateDetector.DetectAsync(task.ProjectId, task.Id, BuildSemanticTaskContent(task), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private static string BuildSemanticTaskContent(TaskItem task) =>
        $"{task.Title}\n{task.Description}\nStatus: {task.Status}\nPriority: {task.Priority}";

    private async Task<ServiceResult?> ValidateAssigneeAsync(
        int projectId,
        int workspaceId,
        int? assignedUserId,
        CancellationToken cancellationToken)
    {
        if (!assignedUserId.HasValue)
        {
            return null;
        }

        var projectMember = await projectMemberRepository.GetMemberAsync(projectId, assignedUserId.Value, cancellationToken);
        if (projectMember is null)
        {
            return ServiceResult.Fail("Assigned user must be a project member.", HttpStatusCode.BadRequest);
        }

        if (projectMember.Role == ProjectRole.Guest)
        {
            return ServiceResult.Fail("Guest users cannot be assigned tasks.", HttpStatusCode.BadRequest);
        }

        var workspaceMember = await workspaceMemberRepository.GetMemberAsync(workspaceId, assignedUserId.Value, cancellationToken);
        if (workspaceMember is null)
        {
            return ServiceResult.Fail("Assigned user must be a workspace member.", HttpStatusCode.BadRequest);
        }

        return workspaceMember.Role == Role.Guest
            ? ServiceResult.Fail("Guest users cannot be assigned tasks.", HttpStatusCode.BadRequest)
            : null;
    }
}
