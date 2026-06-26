using System.Net;
using FluentValidation;
using TaskPilot.Application.Features.Labels.Dtos;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Application.Interfaces.Persistence.Project;
using TaskPilot.Application.Interfaces.Persistence.Workspace;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.Labels.Services;

public class LabelService(
    IGenericRepository<Label> labelRepository,
    IGenericRepository<TaskItem> taskRepository,
    IGenericRepository<TaskLabel> taskLabelRepository,
    IProjectRepository projectRepository,
    IProjectMemberRepository projectMemberRepository,
    IWorkspaceMemberRepository workspaceMemberRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IValidator<CreateLabelRequest> createValidator) : ILabelService
{
    public async Task<ServiceResult<List<LabelResponse>>> GetLabelsAsync(int projectId, CancellationToken cancellationToken)
    {
        var access = await EnsureProjectReadableAsync(projectId, cancellationToken);
        if (access is not null) return ServiceResult<List<LabelResponse>>.Fail(access.ErrorMessages!, access.Status);

        var labels = labelRepository.Where(x => x.ProjectId == projectId).OrderBy(x => x.Name).ToList();
        return ServiceResult<List<LabelResponse>>.Success(labels.Select(Map).ToList());
    }

    public async Task<ServiceResult<LabelResponse>> CreateLabelAsync(int projectId, CreateLabelRequest request, CancellationToken cancellationToken)
    {
        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ServiceResult<LabelResponse>.Fail(validation.Errors.Select(x => x.ErrorMessage).ToList(), HttpStatusCode.BadRequest);

        var access = await EnsureProjectManageableAsync(projectId, cancellationToken);
        if (access is not null) return ServiceResult<LabelResponse>.Fail(access.ErrorMessages!, access.Status);

        var name = request.Name.Trim();
        if (labelRepository.Where(x => x.ProjectId == projectId && x.Name == name).Any())
        {
            return ServiceResult<LabelResponse>.Fail("Label already exists.", HttpStatusCode.Conflict);
        }

        var label = new Label
        {
            ProjectId = projectId,
            Name = name,
            Color = string.IsNullOrWhiteSpace(request.Color) ? "#3B82F6" : request.Color.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await labelRepository.AddAsync(label);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult<LabelResponse>.Success(Map(label), HttpStatusCode.Created);
    }

    public async Task<ServiceResult> AddLabelToTaskAsync(int taskId, int labelId, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(taskId);
        if (task is null) return ServiceResult.Fail("Task not found.", HttpStatusCode.NotFound);
        var label = await labelRepository.GetByIdAsync(labelId);
        if (label is null || label.ProjectId != task.ProjectId) return ServiceResult.Fail("Label not found.", HttpStatusCode.NotFound);

        var access = await EnsureProjectParticipantAsync(task.ProjectId, cancellationToken);
        if (access is not null) return access;

        if (taskLabelRepository.Where(x => x.TaskId == taskId && x.LabelId == labelId).Any())
        {
            return ServiceResult.Fail("Task already has this label.", HttpStatusCode.Conflict);
        }

        await taskLabelRepository.AddAsync(new TaskLabel { TaskId = taskId, LabelId = labelId });
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    public async Task<ServiceResult> RemoveLabelFromTaskAsync(int taskId, int labelId, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(taskId);
        if (task is null) return ServiceResult.Fail("Task not found.", HttpStatusCode.NotFound);
        var access = await EnsureProjectParticipantAsync(task.ProjectId, cancellationToken);
        if (access is not null) return access;

        var taskLabel = taskLabelRepository.Where(x => x.TaskId == taskId && x.LabelId == labelId).FirstOrDefault();
        if (taskLabel is null) return ServiceResult.Fail("Task label not found.", HttpStatusCode.NotFound);

        taskLabelRepository.Delete(taskLabel);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    private async Task<ServiceResult?> EnsureProjectReadableAsync(int projectId, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetProjectByIdAsync(projectId, cancellationToken);
        if (project is null) return ServiceResult.Fail("Project not found.", HttpStatusCode.NotFound);
        if (!await workspaceMemberRepository.IsWorkspaceMemberAsync(project.WorkspaceId, currentUserService.GetRequiredUserId(), cancellationToken))
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
        var workspaceMember = await workspaceMemberRepository.GetMemberAsync(project.WorkspaceId, currentUserService.GetRequiredUserId(), cancellationToken);
        if (workspaceMember is null) return ServiceResult.Fail("Project not found.", HttpStatusCode.NotFound);
        if (workspaceMember.Role == Role.Owner) return null;
        if (!await projectMemberRepository.IsProjectMemberAsync(projectId, currentUserService.GetRequiredUserId(), cancellationToken))
        {
            return ServiceResult.Fail("Only project members can manage task labels.", HttpStatusCode.Forbidden);
        }
        return null;
    }

    private async Task<ServiceResult?> EnsureProjectManageableAsync(int projectId, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetProjectByIdAsync(projectId, cancellationToken);
        if (project is null) return ServiceResult.Fail("Project not found.", HttpStatusCode.NotFound);
        if (project.Status == ProjectStatus.Archived) return ServiceResult.Fail("Project is archived.", HttpStatusCode.BadRequest);
        var workspaceMember = await workspaceMemberRepository.GetMemberAsync(project.WorkspaceId, currentUserService.GetRequiredUserId(), cancellationToken);
        if (workspaceMember is null) return ServiceResult.Fail("Project not found.", HttpStatusCode.NotFound);
        if (workspaceMember.Role == Role.Owner) return null;
        if (!await projectMemberRepository.IsProjectManagerAsync(projectId, currentUserService.GetRequiredUserId(), cancellationToken))
        {
            return ServiceResult.Fail("Only workspace owner or project manager can manage labels.", HttpStatusCode.Forbidden);
        }
        return null;
    }

    private static LabelResponse Map(Label label)
    {
        return new LabelResponse(label.Id, label.ProjectId, label.Name, label.Color, label.CreatedAt);
    }
}
