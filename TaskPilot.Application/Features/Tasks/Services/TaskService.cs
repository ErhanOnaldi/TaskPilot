using System.Net;
using AutoMapper;
using FluentValidation;
using TaskPilot.Application.Features.Tasks.Dtos;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Application.Interfaces.Persistence.Project;
using TaskPilot.Application.Interfaces.Persistence.Workspace;
using TaskPilot.Domain.Entities;
using ProjectEntity = TaskPilot.Domain.Entities.Project;

namespace TaskPilot.Application.Features.Tasks.Services;

public class TaskService(
    IGenericRepository<TaskItem> taskRepository,
    IProjectRepository projectRepository,
    IProjectMemberRepository projectMemberRepository,
    IWorkspaceRepository workspaceRepository,
    IWorkspaceMemberRepository workspaceMemberRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IValidator<CreateTaskRequest> createValidator,
    IValidator<UpdateTaskRequest> updateValidator,
    IValidator<UpdateTaskStatusRequest> statusValidator,
    IValidator<AssignTaskRequest> assignValidator,
    IMapper mapper) : ITaskService
{
    public async Task<ServiceResult<List<TaskResponse>>> GetTasksAsync(int projectId, CancellationToken cancellationToken)
    {
        var access = await LoadProjectAccessAsync(projectId, requireActiveProject: false, cancellationToken);
        if (access.Result is not null) return ServiceResult<List<TaskResponse>>.Fail(access.Result.ErrorMessages!, access.Result.Status);

        var tasks = taskRepository.Where(x => x.ProjectId == projectId)
            .OrderByDescending(x => x.CreatedAt)
            .ToList();
        return ServiceResult<List<TaskResponse>>.Success(mapper.Map<List<TaskResponse>>(tasks));
    }

    public async Task<ServiceResult<TaskResponse>> GetTaskAsync(int taskId, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(taskId);
        if (task is null) return ServiceResult<TaskResponse>.Fail("Task not found.", HttpStatusCode.NotFound);

        var access = await LoadProjectAccessAsync(task.ProjectId, requireActiveProject: false, cancellationToken);
        if (access.Result is not null) return ServiceResult<TaskResponse>.Fail(access.Result.ErrorMessages!, access.Result.Status);

        return ServiceResult<TaskResponse>.Success(mapper.Map<TaskResponse>(task));
    }

    public async Task<ServiceResult<TaskResponse>> CreateTaskAsync(int projectId, CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ServiceResult<TaskResponse>.Fail(validation.Errors.Select(x => x.ErrorMessage).ToList(), HttpStatusCode.BadRequest);

        var access = await LoadProjectAccessAsync(projectId, requireActiveProject: true, cancellationToken);
        if (access.Result is not null) return ServiceResult<TaskResponse>.Fail(access.Result.ErrorMessages!, access.Result.Status);
        if (!await IsProjectParticipantOrWorkspaceOwnerAsync(projectId, access.WorkspaceMember!, cancellationToken))
        {
            return ServiceResult<TaskResponse>.Fail("Only project members can create tasks.", HttpStatusCode.Forbidden);
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
            CreatedByUserId = currentUserService.GetRequiredUserId(),
            CreatedAt = now,
            UpdatedAt = now
        };

        await taskRepository.AddAsync(task);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult<TaskResponse>.Success(mapper.Map<TaskResponse>(task), HttpStatusCode.Created);
    }

    public async Task<ServiceResult> UpdateTaskAsync(int taskId, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var validation = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ServiceResult.Fail(validation.Errors.Select(x => x.ErrorMessage).ToList(), HttpStatusCode.BadRequest);

        var task = await taskRepository.GetByIdAsync(taskId);
        if (task is null) return ServiceResult.Fail("Task not found.", HttpStatusCode.NotFound);

        var access = await LoadProjectAccessAsync(task.ProjectId, requireActiveProject: true, cancellationToken);
        if (access.Result is not null) return access.Result;
        if (!await IsProjectParticipantOrWorkspaceOwnerAsync(task.ProjectId, access.WorkspaceMember!, cancellationToken))
        {
            return ServiceResult.Fail("Only project members can update tasks.", HttpStatusCode.Forbidden);
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

        var access = await LoadProjectAccessAsync(task.ProjectId, requireActiveProject: true, cancellationToken);
        if (access.Result is not null) return access.Result;
        if (!await IsProjectManagerOrWorkspaceOwnerAsync(task.ProjectId, access.WorkspaceMember!, cancellationToken))
        {
            return ServiceResult.Fail("Only workspace owner or project manager can delete tasks.", HttpStatusCode.Forbidden);
        }

        taskRepository.Delete(task);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    public async Task<ServiceResult> UpdateStatusAsync(int taskId, UpdateTaskStatusRequest request, CancellationToken cancellationToken)
    {
        var validation = await statusValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ServiceResult.Fail(validation.Errors.Select(x => x.ErrorMessage).ToList(), HttpStatusCode.BadRequest);

        var task = await taskRepository.GetByIdAsync(taskId);
        if (task is null) return ServiceResult.Fail("Task not found.", HttpStatusCode.NotFound);

        var access = await LoadProjectAccessAsync(task.ProjectId, requireActiveProject: true, cancellationToken);
        if (access.Result is not null) return access.Result;
        if (!await IsProjectParticipantOrWorkspaceOwnerAsync(task.ProjectId, access.WorkspaceMember!, cancellationToken))
        {
            return ServiceResult.Fail("Only project members can update task status.", HttpStatusCode.Forbidden);
        }

        task.Status = request.Status;
        task.CompletedAt = request.Status == TaskItemStatus.Done ? DateTime.UtcNow : null;
        task.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    public async Task<ServiceResult> AssignTaskAsync(int taskId, AssignTaskRequest request, CancellationToken cancellationToken)
    {
        var validation = await assignValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ServiceResult.Fail(validation.Errors.Select(x => x.ErrorMessage).ToList(), HttpStatusCode.BadRequest);

        var task = await taskRepository.GetByIdAsync(taskId);
        if (task is null) return ServiceResult.Fail("Task not found.", HttpStatusCode.NotFound);

        var access = await LoadProjectAccessAsync(task.ProjectId, requireActiveProject: true, cancellationToken);
        if (access.Result is not null) return access.Result;
        if (!await IsProjectManagerOrWorkspaceOwnerAsync(task.ProjectId, access.WorkspaceMember!, cancellationToken))
        {
            return ServiceResult.Fail("Only workspace owner or project manager can assign tasks.", HttpStatusCode.Forbidden);
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

    private async Task<ProjectAccess> LoadProjectAccessAsync(int projectId, bool requireActiveProject, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetProjectByIdAsync(projectId, cancellationToken);
        if (project is null) return ProjectAccess.Fail(ServiceResult.Fail("Project not found.", HttpStatusCode.NotFound));
        if (requireActiveProject && project.Status == ProjectStatus.Archived) return ProjectAccess.Fail(ServiceResult.Fail("Project is archived.", HttpStatusCode.BadRequest));

        var workspace = await workspaceRepository.GetByIdAsync(project.WorkspaceId);
        if (workspace is null) return ProjectAccess.Fail(ServiceResult.Fail("Workspace not found.", HttpStatusCode.NotFound));
        if (workspace.IsArchived) return ProjectAccess.Fail(ServiceResult.Fail("Workspace is archived.", HttpStatusCode.BadRequest));

        var workspaceMember = await workspaceMemberRepository.GetMemberAsync(project.WorkspaceId, currentUserService.GetRequiredUserId(), cancellationToken);
        if (workspaceMember is null) return ProjectAccess.Fail(ServiceResult.Fail("Project not found.", HttpStatusCode.NotFound));

        return new ProjectAccess(project, workspaceMember, null);
    }

    private async Task<bool> IsProjectParticipantOrWorkspaceOwnerAsync(int projectId, WorkspaceMember workspaceMember, CancellationToken cancellationToken)
    {
        return workspaceMember.Role == Role.Owner ||
               await projectMemberRepository.IsProjectMemberAsync(projectId, currentUserService.GetRequiredUserId(), cancellationToken);
    }

    private async Task<bool> IsProjectManagerOrWorkspaceOwnerAsync(int projectId, WorkspaceMember workspaceMember, CancellationToken cancellationToken)
    {
        return workspaceMember.Role == Role.Owner ||
               await projectMemberRepository.IsProjectManagerAsync(projectId, currentUserService.GetRequiredUserId(), cancellationToken);
    }

    private sealed record ProjectAccess(ProjectEntity Project, WorkspaceMember WorkspaceMember, ServiceResult? Result)
    {
        public static ProjectAccess Fail(ServiceResult result) => new(null!, null!, result);
    }
}
